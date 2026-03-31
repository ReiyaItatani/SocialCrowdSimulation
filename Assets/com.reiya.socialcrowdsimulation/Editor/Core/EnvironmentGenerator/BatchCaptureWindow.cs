#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using CollisionAvoidance;
using CollisionAvoidance.EnvironmentGeneration;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Batch dataset generation window.
    /// Generates multiple environment + agent configurations and captures data automatically.
    /// Menu: SocialCrowdSimulation > Batch Capture
    /// </summary>
    public class BatchCaptureWindow : EditorWindow
    {
        // Batch configuration
        private int[] seeds = { 42, 123, 456 };
        private int[] agentCounts = { 5, 10 };
        private float captureDuration = 10f;
        private bool captureDepth = true;
        private bool visualizeFOV;
        private string seedsText = "42, 123, 456";
        private string agentCountsText = "5, 10";

        // State
        private List<BatchJob> pendingJobs = new List<BatchJob>();
        private List<BatchJob> completedJobs = new List<BatchJob>();
        private bool isRunning;
        private string statusMessage = "";
        private Vector2 scrollPosition;

        private struct BatchJob
        {
            public LayoutType layout;
            public int seed;
            public int agentCount;
            public string scenarioName;
        }

        [MenuItem("SocialCrowdSimulation/Batch Capture")]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchCaptureWindow>("Batch Capture");
            window.minSize = new Vector2(350, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Batch Dataset Generation", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Configuration
            seedsText = EditorGUILayout.TextField("Seeds (comma-sep)", seedsText);
            agentCountsText = EditorGUILayout.TextField("Agent Counts (comma-sep)", agentCountsText);
            captureDuration = EditorGUILayout.FloatField("Capture Duration (sec)", captureDuration);
            captureDepth = EditorGUILayout.Toggle("Capture Depth", captureDepth);
            visualizeFOV = EditorGUILayout.Toggle("Visualize FOV", visualizeFOV);

            EditorGUILayout.Space(8);

            // Generate job list
            if (GUILayout.Button("Preview Jobs", GUILayout.Height(30)))
            {
                GenerateJobList();
            }

            if (pendingJobs.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Jobs: {pendingJobs.Count} pending, {completedJobs.Count} completed");

                if (GUILayout.Button("Generate All Environments (Editor Only)", GUILayout.Height(36)))
                {
                    RunBatchGeneration();
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "This generates environments one by one in Editor mode.\n" +
                    "For data capture, each environment needs Play mode.\n" +
                    "Use 'Generate Next + Capture' to process one at a time.",
                    MessageType.Info);

                if (GUILayout.Button("Generate Next Environment"))
                {
                    GenerateNextEnvironment();
                }
            }

            // Job list
            if (pendingJobs.Count > 0 || completedJobs.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Job Queue", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));

                foreach (var job in completedJobs)
                {
                    EditorGUILayout.LabelField($"  ✓ {job.scenarioName}", EditorStyles.miniLabel);
                }
                foreach (var job in pendingJobs)
                {
                    EditorGUILayout.LabelField($"  ○ {job.scenarioName}", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndScrollView();
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        private void GenerateJobList()
        {
            pendingJobs.Clear();
            completedJobs.Clear();

            seeds = ParseIntArray(seedsText);
            agentCounts = ParseIntArray(agentCountsText);

            var layouts = (LayoutType[])System.Enum.GetValues(typeof(LayoutType));

            foreach (var layout in layouts)
            {
                foreach (int seed in seeds)
                {
                    foreach (int count in agentCounts)
                    {
                        pendingJobs.Add(new BatchJob
                        {
                            layout = layout,
                            seed = seed,
                            agentCount = count,
                            scenarioName = $"{layout.ToString().ToLower()}_seed{seed}_{count}agents"
                        });
                    }
                }
            }

            statusMessage = $"Generated {pendingJobs.Count} jobs: {layouts.Length} layouts × {seeds.Length} seeds × {agentCounts.Length} densities";
        }

        private void RunBatchGeneration()
        {
            int total = pendingJobs.Count;
            for (int i = 0; i < total; i++)
            {
                EditorUtility.DisplayProgressBar("Batch Generation",
                    $"Generating {pendingJobs[0].scenarioName}... ({i + 1}/{total})",
                    (float)i / total);

                GenerateNextEnvironment();
            }
            EditorUtility.ClearProgressBar();
            statusMessage = $"All {total} environments generated. Play each scene to capture data.";
        }

        private void GenerateNextEnvironment()
        {
            if (pendingJobs.Count == 0)
            {
                statusMessage = "No pending jobs.";
                return;
            }

            var job = pendingJobs[0];
            pendingJobs.RemoveAt(0);

            // Clear existing
            ClearEnvironment();

            // Ensure tags
            EnsureTagExists("Wall");
            EnsureTagExists("Obstacle");
            EnsureTagExists("Agent");
            EnsureTagExists("Group");

            // Create config
            var config = EnvironmentGeneratorWindow.CreatePresetConfigPublic(job.layout, job.seed);

            // Build layout
            var layout = LayoutBuilder.Build(config.layout, job.seed);
            Physics.SyncTransforms();

            // QuickGraph
            var graph = QuickGraphBuilder.Build(layout, config.graph, job.seed);

            // NavMesh
            NavMeshBuilder.BuildNavMesh();

            // Agents (auto-generate AgentsList)
            if (graph != null)
            {
                var agentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/com.reiya.socialcrowdsimulation/Sample/QuickStart/ForAvatarCreator/Agent.prefab");

                if (agentPrefab != null)
                {
                    var agentsList = CreateAgentsList(agentPrefab, job.agentCount);

                    var creatorGO = new GameObject("AvatarCreator");
                    creatorGO.transform.parent = layout.rootGameObject.transform;
                    creatorGO.AddComponent<AgentManager>();

                    var avatarCreator = creatorGO.AddComponent<AvatarCreatorQuickGraph>();
                    avatarCreator.quickGraph = graph;
                    avatarCreator.agentsList = agentsList;
                    avatarCreator.InstantiateAvatars();

                    // Add DataCaptureManager
                    var dcGO = new GameObject("DataCaptureManager");
                    dcGO.transform.parent = layout.rootGameObject.transform;
                    var dcm = dcGO.AddComponent<DataCaptureManager>();

                    var so = new SerializedObject(dcm);
                    so.FindProperty("scenarioName").stringValue = job.scenarioName;
                    so.FindProperty("captureDepth").boolValue = captureDepth;
                    so.FindProperty("visualizeFOV").boolValue = visualizeFOV;
                    so.FindProperty("observerAgentIndex").intValue = 0;
                    so.ApplyModifiedProperties();

                    int total = avatarCreator.instantiatedAvatars.Count;
                    statusMessage = $"Generated: {job.scenarioName} ({total} agents). Hit Play → Start Capture.";
                }
            }

            completedJobs.Add(job);
            Selection.activeGameObject = layout.rootGameObject;
        }

        private AgentsList CreateAgentsList(GameObject prefab, int totalCount)
        {
            var al = ScriptableObject.CreateInstance<AgentsList>();

            // Split: ~70% individual, ~30% groups
            int groupCount = Mathf.Max(1, totalCount / 4);
            int groupSize = 2;
            int individualCount = totalCount - groupCount * groupSize;
            if (individualCount < 0) individualCount = 0;

            al.individuals = new IndividualEntry
            {
                agents = new System.Collections.Generic.List<GameObject>(),
                speedRange = new SpeedRange(0.5f, 1.2f)
            };
            for (int i = 0; i < individualCount; i++)
                al.individuals.agents.Add(prefab);

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
                    group.agents.Add(prefab);
                al.groups.Add(group);
            }

            return al;
        }

        private void ClearEnvironment()
        {
            foreach (var name in new[] { "Environment_Corridor", "Environment_Plaza", "Environment_Intersection",
                "Environment_LShape", "Environment_TJunction", "Environment_Bottleneck" })
            {
                var go = GameObject.Find(name);
                if (go != null) DestroyImmediate(go);
            }
        }

        private static int[] ParseIntArray(string text)
        {
            var parts = text.Split(',');
            var result = new System.Collections.Generic.List<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int val))
                    result.Add(val);
            }
            return result.ToArray();
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
