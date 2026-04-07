#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using CollisionAvoidance;
using CollisionAvoidance.EnvironmentGeneration;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Batch dataset generation window.
    /// State persists across Play/Stop via EditorPrefs.
    /// Menu: SocialCrowdSimulation > Batch Capture
    /// </summary>
    public class BatchCaptureWindow : EditorWindow
    {
        // EditorPrefs keys
        private const string PREF_SEEDS = "BatchCapture_Seeds";
        private const string PREF_AGENTS = "BatchCapture_Agents";
        private const string PREF_LAYOUTS = "BatchCapture_Layouts";
        private const string PREF_DURATION = "BatchCapture_Duration";
        private const string PREF_DEPTH = "BatchCapture_Depth";
        private const string PREF_FOV = "BatchCapture_FOV";
        private const string PREF_CAM_DIR = "BatchCapture_CamDir";
        private const string PREF_PENDING = "BatchCapture_Pending";
        private const string PREF_COMPLETED = "BatchCapture_Completed";

        // Configuration
        private string seedsText = "42, 123";
        private string agentCountsText = "5, 10";
        private string layoutsText = "Corridor, Plaza";
        private float captureDuration = 10f;
        private bool captureDepth = true;
        private bool visualizeFOV;
        private CameraDirectionMode cameraDirectionMode = CameraDirectionMode.MovementDirection;

        // State
        private List<string> pendingJobs = new List<string>();
        private List<string> completedJobs = new List<string>();
        private string statusMessage = "";
        private Vector2 scrollPosition;

        [MenuItem("SocialCrowdSimulation/Batch Capture")]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchCaptureWindow>("Batch Capture");
            window.minSize = new Vector2(350, 400);
        }

        private void OnEnable()
        {
            LoadState();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Batch Dataset Generation", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Configuration
            seedsText = EditorGUILayout.TextField("Seeds", seedsText);
            agentCountsText = EditorGUILayout.TextField("Agent Counts", agentCountsText);
            layoutsText = EditorGUILayout.TextField("Layouts", layoutsText);
            captureDuration = EditorGUILayout.FloatField("Capture Duration (sec)", captureDuration);
            captureDepth = EditorGUILayout.Toggle("Capture Depth", captureDepth);
            visualizeFOV = EditorGUILayout.Toggle("Visualize FOV", visualizeFOV);
            cameraDirectionMode = (CameraDirectionMode)EditorGUILayout.EnumPopup("Camera Direction", cameraDirectionMode);

            EditorGUILayout.Space(8);

            // Preview / Reset
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Jobs", GUILayout.Height(30)))
            {
                GenerateJobList();
                SaveState();
            }
            if (GUILayout.Button("Reset", GUILayout.Height(30), GUILayout.Width(60)))
            {
                pendingJobs.Clear();
                completedJobs.Clear();
                statusMessage = "Reset.";
                SaveState();
            }
            EditorGUILayout.EndHorizontal();

            // Progress
            int total = pendingJobs.Count + completedJobs.Count;
            if (total > 0)
            {
                EditorGUILayout.Space(4);
                float progress = (float)completedJobs.Count / total;
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                    progress,
                    $"{completedJobs.Count} / {total} completed");
            }

            // Generate Next button
            if (pendingJobs.Count > 0)
            {
                EditorGUILayout.Space(8);
                string nextJob = pendingJobs[0];
                if (GUILayout.Button($"Generate Next: {nextJob}", GUILayout.Height(36)))
                {
                    GenerateEnvironment(nextJob);
                    pendingJobs.RemoveAt(0);
                    completedJobs.Add(nextJob);
                    SaveState();
                }

                EditorGUILayout.HelpBox(
                    "1. Click 'Generate Next' above\n" +
                    "2. Play → Start Capture in Inspector\n" +
                    "3. Wait for 300 frames → Stop Capture\n" +
                    "4. Stop Play → Click 'Generate Next' again",
                    MessageType.Info);
            }
            else if (completedJobs.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("All jobs completed!", MessageType.Info);
            }

            // Job list
            if (total > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Job Queue", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));

                foreach (var job in completedJobs)
                {
                    EditorGUILayout.LabelField($"  \u2713 {job}", EditorStyles.miniLabel);
                }
                foreach (var job in pendingJobs)
                {
                    EditorGUILayout.LabelField($"  \u25CB {job}", EditorStyles.miniLabel);
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

            var seeds = ParseIntArray(seedsText);
            var agentCounts = ParseIntArray(agentCountsText);
            var layouts = ParseLayouts(layoutsText);

            foreach (var layout in layouts)
            {
                foreach (int seed in seeds)
                {
                    foreach (int count in agentCounts)
                    {
                        string name = $"{layout.ToString().ToLower()}_seed{seed}_{count}agents";
                        pendingJobs.Add(name);
                    }
                }
            }

            statusMessage = $"{pendingJobs.Count} jobs: {layouts.Length} layouts x {seeds.Length} seeds x {agentCounts.Length} densities";
        }

        private void GenerateEnvironment(string jobName)
        {
            // Parse job name: layout_seedN_Magents
            var parts = jobName.Split('_');
            string layoutStr = parts[0];
            int seed = int.Parse(parts[1].Replace("seed", ""));
            int agentCount = int.Parse(parts[2].Replace("agents", ""));

            if (!System.Enum.TryParse(layoutStr, true, out LayoutType layout))
            {
                statusMessage = $"Unknown layout: {layoutStr}";
                return;
            }

            // Clear existing
            ClearEnvironment();

            EnsureTagExists("Wall");
            EnsureTagExists("Obstacle");
            EnsureTagExists("Agent");
            EnsureTagExists("Group");

            // Build
            var config = EnvironmentGeneratorWindow.CreatePresetConfigPublic(layout, seed);
            var layoutResult = LayoutBuilder.Build(config.layout, seed);
            Physics.SyncTransforms();
            var graph = QuickGraphBuilder.Build(layoutResult, config.graph, seed);
            NavMeshBuilder.BuildNavMesh();

            if (graph == null) { statusMessage = "Graph build failed."; return; }

            var agentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/com.reiya.socialcrowdsimulation/Sample/QuickStart/ForAvatarCreator/Agent.prefab");
            if (agentPrefab == null) { statusMessage = "Agent prefab not found."; return; }

            var agentsList = CreateAgentsList(agentPrefab, agentCount);

            var creatorGO = new GameObject("AvatarCreator");
            creatorGO.transform.parent = layoutResult.rootGameObject.transform;
            creatorGO.AddComponent<AgentManager>();

            var avatarCreator = creatorGO.AddComponent<AvatarCreatorQuickGraph>();
            avatarCreator.quickGraph = graph;
            avatarCreator.agentsList = agentsList;
            avatarCreator.observerAgentIndex = 0;
            avatarCreator.InstantiateAvatars();

            // DataCaptureManager
            var dcGO = new GameObject("DataCaptureManager");
            dcGO.transform.parent = layoutResult.rootGameObject.transform;
            var dcm = dcGO.AddComponent<DataCaptureManager>();

            var so = new SerializedObject(dcm);
            so.FindProperty("scenarioName").stringValue = jobName;
            so.FindProperty("captureDepth").boolValue = captureDepth;
            so.FindProperty("visualizeFOV").boolValue = visualizeFOV;
            so.FindProperty("observerAgentIndex").intValue = 0;
            so.FindProperty("cameraDirectionMode").enumValueIndex = (int)cameraDirectionMode;
            so.FindProperty("autoCapture").boolValue = true;
            so.FindProperty("maxFrames").intValue = (int)(captureDuration * 30);
            so.FindProperty("autoExitPlay").boolValue = true;
            so.ApplyModifiedProperties();

            int total = avatarCreator.instantiatedAvatars.Count;
            statusMessage = $"Generated: {jobName} ({total} agents). Hit Play — auto captures {(int)(captureDuration * 30)} frames and stops.";

            Selection.activeGameObject = dcGO;
        }

        private AgentsList CreateAgentsList(GameObject prefab, int totalCount)
        {
            var al = ScriptableObject.CreateInstance<AgentsList>();

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

        // --- State Persistence ---

        private void SaveState()
        {
            EditorPrefs.SetString(PREF_SEEDS, seedsText);
            EditorPrefs.SetString(PREF_AGENTS, agentCountsText);
            EditorPrefs.SetString(PREF_LAYOUTS, layoutsText);
            EditorPrefs.SetFloat(PREF_DURATION, captureDuration);
            EditorPrefs.SetBool(PREF_DEPTH, captureDepth);
            EditorPrefs.SetBool(PREF_FOV, visualizeFOV);
            EditorPrefs.SetInt(PREF_CAM_DIR, (int)cameraDirectionMode);
            EditorPrefs.SetString(PREF_PENDING, string.Join("|", pendingJobs));
            EditorPrefs.SetString(PREF_COMPLETED, string.Join("|", completedJobs));
        }

        private void LoadState()
        {
            seedsText = EditorPrefs.GetString(PREF_SEEDS, seedsText);
            agentCountsText = EditorPrefs.GetString(PREF_AGENTS, agentCountsText);
            layoutsText = EditorPrefs.GetString(PREF_LAYOUTS, layoutsText);
            captureDuration = EditorPrefs.GetFloat(PREF_DURATION, captureDuration);
            captureDepth = EditorPrefs.GetBool(PREF_DEPTH, captureDepth);
            visualizeFOV = EditorPrefs.GetBool(PREF_FOV, visualizeFOV);
            cameraDirectionMode = (CameraDirectionMode)EditorPrefs.GetInt(PREF_CAM_DIR, (int)cameraDirectionMode);

            string pending = EditorPrefs.GetString(PREF_PENDING, "");
            pendingJobs = string.IsNullOrEmpty(pending)
                ? new List<string>()
                : pending.Split('|').Where(s => !string.IsNullOrEmpty(s)).ToList();

            string completed = EditorPrefs.GetString(PREF_COMPLETED, "");
            completedJobs = string.IsNullOrEmpty(completed)
                ? new List<string>()
                : completed.Split('|').Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        // --- Utilities ---

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
            return text.Split(',')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToArray();
        }

        private static LayoutType[] ParseLayouts(string text)
        {
            return text.Split(',')
                .Select(s => s.Trim())
                .Where(s => System.Enum.TryParse(s, true, out LayoutType _))
                .Select(s => (LayoutType)System.Enum.Parse(typeof(LayoutType), s, true))
                .ToArray();
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
