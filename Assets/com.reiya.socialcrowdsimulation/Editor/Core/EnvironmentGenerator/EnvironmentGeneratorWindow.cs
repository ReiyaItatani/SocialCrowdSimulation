#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Simple environment generation window.
    /// Layout + AgentsList + Seed + Generate.
    /// Menu: SocialCrowdSimulation > Environment Generator
    /// </summary>
    public class EnvironmentGeneratorWindow : EditorWindow
    {
        private LayoutType selectedLayout = LayoutType.Corridor;
        private int seed = 42;

        // Agent config
        private enum AgentMode { Manual, Auto }
        private AgentMode agentMode = AgentMode.Auto;
        private AgentsList agentsList; // Manual mode
        private int individualCount = 3; // Auto mode
        private int groupCount = 1;
        private int groupSize = 2;

        private GameObject generatedEnvironment;
        private string statusMessage = "";
        private MessageType statusType = MessageType.None;

        // Data Capture
        private bool showDataCapture;
        private string scenarioName = "default";
        private bool visualizeFOV;
        private int observerAgentIndex;
        private DataCaptureManager dataCaptureInstance;

        [MenuItem("SocialCrowdSimulation/Environment Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnvironmentGeneratorWindow>("Environment Generator");
            window.minSize = new Vector2(300, 200);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Environment Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            selectedLayout = (LayoutType)EditorGUILayout.EnumPopup("Layout", selectedLayout);
            seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space(4);
            agentMode = (AgentMode)EditorGUILayout.EnumPopup("Agent Mode", agentMode);

            if (agentMode == AgentMode.Manual)
            {
                agentsList = (AgentsList)EditorGUILayout.ObjectField(
                    "Agents List", agentsList, typeof(AgentsList), false);
            }
            else
            {
                individualCount = EditorGUILayout.IntSlider("Individuals", individualCount, 0, 20);
                groupCount = EditorGUILayout.IntSlider("Groups", groupCount, 0, 10);
                if (groupCount > 0)
                {
                    groupSize = EditorGUILayout.IntSlider("Group Size", groupSize, 2, 5);
                }
                int total = individualCount + groupCount * groupSize;
                EditorGUILayout.LabelField("Total Agents", total.ToString(), EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(8);

            bool canGenerate = (agentMode == AgentMode.Auto && (individualCount + groupCount) > 0)
                            || (agentMode == AgentMode.Manual && agentsList != null);
            GUI.enabled = canGenerate;
            if (GUILayout.Button("Generate", GUILayout.Height(36)))
            {
                Generate();
            }
            GUI.enabled = true;

            if (generatedEnvironment != null)
            {
                if (GUILayout.Button("Clear"))
                {
                    ClearEnvironment();
                }
            }

            // --- Data Capture Section ---
            if (generatedEnvironment != null)
            {
                EditorGUILayout.Space(12);
                showDataCapture = EditorGUILayout.Foldout(showDataCapture, "Data Capture", true, EditorStyles.foldoutHeader);

                if (showDataCapture)
                {
                    EditorGUI.indentLevel++;
                    scenarioName = EditorGUILayout.TextField("Scenario Name", scenarioName);
                    observerAgentIndex = EditorGUILayout.IntField("Observer Agent Index", observerAgentIndex);
                    visualizeFOV = EditorGUILayout.Toggle("Visualize FOV", visualizeFOV);

                    dataCaptureInstance = generatedEnvironment.GetComponentInChildren<DataCaptureManager>();

                    EditorGUILayout.Space(4);
                    if (dataCaptureInstance == null)
                    {
                        if (GUILayout.Button("Add Data Capture"))
                        {
                            AddDataCapture();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("DataCaptureManager is ready.\nHit Play, then click Start Capture in the Inspector.", MessageType.Info);
                        if (GUILayout.Button("Remove Data Capture"))
                        {
                            RemoveDataCapture();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            if (agentMode == AgentMode.Manual && agentsList == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Drag an AgentsList asset to generate agents.\n" +
                    "Find examples in Sample/QuickStart/ForAvatarCreator/.\n" +
                    "Or switch to Auto mode to use sliders.",
                    MessageType.Info);
            }
        }

        private void Generate()
        {
            ClearEnvironment();

            var config = CreatePresetConfig(selectedLayout, seed);

            EnsureTagExists("Wall");
            EnsureTagExists("Obstacle");
            EnsureTagExists("Agent");
            EnsureTagExists("Group");

            // L1: Layout
            var layout = LayoutBuilder.Build(config.layout, seed);
            Physics.SyncTransforms();

            // L2: QuickGraph
            var graph = QuickGraphBuilder.Build(layout, config.graph, seed);

            // NavMesh bake
            NavMeshBuilder.BuildNavMesh();

            // Agent setup
            if (graph != null)
            {
                // Auto-generate AgentsList if in Auto mode
                AgentsList effectiveAgentsList = agentsList;
                if (agentMode == AgentMode.Auto)
                {
                    effectiveAgentsList = CreateAgentsListFromSliders();
                }

                var creatorGO = new GameObject("AvatarCreator");
                creatorGO.transform.parent = layout.rootGameObject.transform;
                creatorGO.AddComponent<AgentManager>();

                var avatarCreator = creatorGO.AddComponent<AvatarCreatorQuickGraph>();
                avatarCreator.quickGraph = graph;
                avatarCreator.agentsList = effectiveAgentsList;

                // Instantiate agents directly using the existing system
                avatarCreator.InstantiateAvatars();

                // Add bootstrapper for Play mode re-entry (if scene is saved)
                layout.rootGameObject.AddComponent<EnvironmentBootstrapper>();

                int total = avatarCreator.instantiatedAvatars.Count;
                int groups = avatarCreator.instantiatedGroups.Count;

                statusMessage = $"Generated: {selectedLayout} (seed={seed})\n" +
                                $"Agents: {total} spawned ({groups} groups)\n" +
                                $"Hit Play to start simulation.";
                statusType = MessageType.Info;
            }

            generatedEnvironment = layout.rootGameObject;

            // Validate
            var report = EnvironmentValidator.Validate(generatedEnvironment);
            if (!report.IsValid)
            {
                statusMessage += $"\n\nWarnings:\n{string.Join("\n", report.messages)}";
                statusType = MessageType.Warning;
            }
        }

        private void ClearEnvironment()
        {
            if (generatedEnvironment != null)
            {
                DestroyImmediate(generatedEnvironment);
                generatedEnvironment = null;
            }
            statusMessage = "";
        }

        public static EnvironmentConfig CreatePresetConfigPublic(LayoutType layout, int seed)
        {
            return CreatePresetConfig(layout, seed);
        }

        private static EnvironmentConfig CreatePresetConfig(LayoutType layout, int seed)
        {
            var config = new EnvironmentConfig { seed = seed };
            config.layout.type = layout;
            config.layout.wallHeight = 3f;
            config.layout.wallThickness = 0.2f;

            switch (layout)
            {
                case LayoutType.Corridor:
                    config.layout.width = 4f;
                    config.layout.depth = 20f;
                    config.layout.openEnds = true;
                    break;
                case LayoutType.Plaza:
                    config.layout.width = 15f;
                    config.layout.depth = 15f;
                    break;
                case LayoutType.Intersection:
                    config.layout.armWidth = 4f;
                    config.layout.armDepth = 12f;
                    break;
                case LayoutType.LShape:
                    config.layout.arm1Width = 4f;
                    config.layout.arm1Depth = 12f;
                    config.layout.arm2Width = 4f;
                    config.layout.arm2Depth = 12f;
                    break;
                case LayoutType.TJunction:
                    config.layout.mainWidth = 4f;
                    config.layout.mainDepth = 20f;
                    config.layout.branchWidth = 4f;
                    config.layout.branchDepth = 10f;
                    break;
                case LayoutType.Bottleneck:
                    config.layout.wideWidth = 10f;
                    config.layout.wideDepth = 8f;
                    config.layout.narrowWidth = 2f;
                    config.layout.narrowDepth = 4f;
                    break;
            }

            config.graph.nodeSpacing = 3f;
            config.graph.wallMargin = 0.8f;
            config.graph.maxConnectionDistance = 8f;
            config.graph.requireLineOfSight = true;

            return config;
        }

        private AgentsList CreateAgentsListFromSliders()
        {
            var agentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/com.reiya.socialcrowdsimulation/Sample/QuickStart/ForAvatarCreator/Agent.prefab");

            if (agentPrefab == null)
            {
                Debug.LogError("[EnvironmentGenerator] Agent.prefab not found.");
                return null;
            }

            var al = ScriptableObject.CreateInstance<AgentsList>();

            // Individuals
            al.individuals = new IndividualEntry
            {
                agents = new System.Collections.Generic.List<GameObject>(),
                speedRange = new SpeedRange(0.5f, 1.2f)
            };
            for (int i = 0; i < individualCount; i++)
            {
                al.individuals.agents.Add(agentPrefab);
            }

            // Groups
            al.groups = new System.Collections.Generic.List<GroupEntry>();
            for (int g = 0; g < groupCount; g++)
            {
                var group = new GroupEntry
                {
                    groupName = "Group" + (g + 1),
                    count = groupSize,
                    agents = new System.Collections.Generic.List<GameObject>(),
                    speedRange = new SpeedRange(0.4f, 1.0f)
                };
                for (int m = 0; m < groupSize; m++)
                {
                    group.agents.Add(agentPrefab);
                }
                al.groups.Add(group);
            }

            return al;
        }

        private void AddDataCapture()
        {
            if (generatedEnvironment == null) return;

            var dcGO = new GameObject("DataCaptureManager");
            dcGO.transform.parent = generatedEnvironment.transform;
            var dcm = dcGO.AddComponent<DataCaptureManager>();

            var so = new SerializedObject(dcm);
            so.FindProperty("scenarioName").stringValue = scenarioName;
            so.FindProperty("visualizeFOV").boolValue = visualizeFOV;
            so.FindProperty("observerAgentIndex").intValue = observerAgentIndex;
            so.ApplyModifiedProperties();

            dataCaptureInstance = dcm;
            Selection.activeGameObject = dcGO;

            statusMessage = "DataCaptureManager added. Hit Play → Start Capture.";
            statusType = MessageType.Info;
        }

        private void RemoveDataCapture()
        {
            if (dataCaptureInstance != null)
            {
                DestroyImmediate(dataCaptureInstance.gameObject);
                dataCaptureInstance = null;
                statusMessage = "DataCaptureManager removed.";
                statusType = MessageType.Info;
            }
        }

        private static void EnsureTagExists(string tag)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0) return;
            var tagManager = new SerializedObject(asset[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif
