using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CollisionAvoidance
{
    [CustomEditor(typeof(DataCaptureManager))]
    public class DataCaptureManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var manager = (DataCaptureManager)target;

            List<GameObject> runtimeAgents = manager.GetSceneAgents();
            bool hasRuntimeAgents = runtimeAgents.Count > 0;

            List<AgentEntry> agentEntries = hasRuntimeAgents
                ? BuildFromRuntime(runtimeAgents)
                : BuildFromAgentsList();

            if (agentEntries.Count > 0)
            {
                string[] names = BuildDisplayNames(agentEntries);
                int clampedIndex = Mathf.Clamp(manager.observerAgentIndex, 0, agentEntries.Count - 1);

                EditorGUILayout.LabelField("Observer Agent", EditorStyles.boldLabel);

                if (!hasRuntimeAgents)
                {
                    EditorGUILayout.HelpBox(
                        "Showing prefab list (agents not yet spawned). Index will map to spawn order at runtime.",
                        MessageType.None);
                }

                int newIndex = EditorGUILayout.Popup("Select Observer", clampedIndex, names);
                if (newIndex != manager.observerAgentIndex)
                {
                    Undo.RecordObject(manager, "Change Observer Agent");
                    manager.observerAgentIndex = newIndex;
                    EditorUtility.SetDirty(manager);
                }

                EditorGUILayout.HelpBox(
                    $"Recording {agentEntries.Count - 1} other agents' trajectories.\n" +
                    $"Observer ({names[clampedIndex]}) will be excluded from CSV.",
                    MessageType.Info);

                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No agents found. Make sure AvatarCreatorQuickGraph is in the scene with an AgentsList assigned.",
                    MessageType.Warning);

                EditorGUILayout.LabelField("Observer Agent Index (manual)", EditorStyles.boldLabel);
                int newIndex = EditorGUILayout.IntField("Index", manager.observerAgentIndex);
                if (newIndex != manager.observerAgentIndex)
                {
                    Undo.RecordObject(manager, "Change Observer Agent");
                    manager.observerAgentIndex = Mathf.Max(0, newIndex);
                    EditorUtility.SetDirty(manager);
                }
            }

            // Perspective blend slider with 1PP/3PP labels
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Perspective", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("1PP", GUILayout.Width(30));
            var blendProp = serializedObject.FindProperty("perspectiveBlend");
            float newBlend = EditorGUILayout.Slider(blendProp.floatValue, 0f, 1f);
            if (!Mathf.Approximately(newBlend, blendProp.floatValue))
            {
                blendProp.floatValue = newBlend;
            }
            EditorGUILayout.LabelField("Bird", GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            DrawPropertiesExcluding(serializedObject, "m_Script", "observerAgentIndex", "perspectiveBlend");
            serializedObject.ApplyModifiedProperties();

            DrawCaptureControls(manager);
        }

        private void DrawCaptureControls(DataCaptureManager manager)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Capture Control", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to start capture.", MessageType.None);
                return;
            }

            bool isCapturing = manager.IsCapturing;

            if (isCapturing)
            {
                EditorGUILayout.HelpBox($"Recording... Frame: {manager.FrameCount}", MessageType.None);

                if (GUILayout.Button("Stop Capture", GUILayout.Height(30)))
                {
                    manager.StopCapture();
                }
            }
            else
            {
                if (GUILayout.Button("Start Capture", GUILayout.Height(30)))
                {
                    manager.StartCapture();
                }
            }

            Repaint();
        }

        private List<AgentEntry> BuildFromRuntime(List<GameObject> agents)
        {
            var entries = new List<AgentEntry>(agents.Count);
            for (int i = 0; i < agents.Count; i++)
            {
                string name = agents[i] != null ? agents[i].name : "(null)";
                entries.Add(new AgentEntry { name = name, group = "" });
            }
            return entries;
        }

        private List<AgentEntry> BuildFromAgentsList()
        {
#if UNITY_2023_1_OR_NEWER
            var creator = Object.FindAnyObjectByType<AvatarCreatorQuickGraph>();
#else
            var creator = Object.FindObjectOfType<AvatarCreatorQuickGraph>();
#endif
            if (creator == null || creator.agentsList == null)
                return new List<AgentEntry>();

            var entries = new List<AgentEntry>();

            if (creator.agentsList.individuals != null)
            {
                foreach (GameObject agent in creator.agentsList.individuals.agents)
                {
                    string name = agent != null ? agent.name : "(unassigned)";
                    entries.Add(new AgentEntry { name = name, group = "Individual" });
                }
            }

            if (creator.agentsList.groups != null)
            {
                foreach (GroupEntry group in creator.agentsList.groups)
                {
                    foreach (GameObject agent in group.agents)
                    {
                        string name = agent != null ? agent.name : "(unassigned)";
                        entries.Add(new AgentEntry { name = name, group = group.groupName });
                    }
                }
            }

            return entries;
        }

        private string[] BuildDisplayNames(List<AgentEntry> entries)
        {
            var names = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                string groupLabel = string.IsNullOrEmpty(entries[i].group) ? "" : $" ({entries[i].group})";
                names[i] = $"[{i}] {entries[i].name}{groupLabel}";
            }
            return names;
        }

        private struct AgentEntry
        {
            public string name;
            public string group;
        }
    }
}
