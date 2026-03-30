using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Captures first-person perspective video and records surrounding agent trajectories.
    /// Attach to a single GameObject in the scene. Observer agent is excluded from trajectory CSV.
    /// AvatarCreatorQuickGraph is auto-detected from the scene.
    /// </summary>
    public class DataCaptureManager : MonoBehaviour
    {
        private const float AgentBodyCenterHeight = 1.0f;

        [Header("Observer Selection")]
        [Tooltip("Select which agent to use as the 1PP camera observer")]
        public int observerAgentIndex;

        [Header("Perspective")]
        [Tooltip("0 = First Person, 1 = Bird View. Blend in real-time.")]
        [Range(0f, 1f)]
        public float perspectiveBlend;
        [Tooltip("Bird view camera height above the agent")]
        [SerializeField] private float birdViewHeight = 10f;
        [Tooltip("Bird view slight forward tilt in degrees (0 = straight down)")]
        [Range(0f, 30f)]
        [SerializeField] private float birdViewTiltDeg = 5f;

        [Header("Camera Settings")]
        [SerializeField] private int captureWidth = 1920;
        [SerializeField] private int captureHeight = 1080;
        [SerializeField] private float fieldOfView = 90f;
        [SerializeField] private float nearClip = 0.01f;
        [SerializeField] private float farClip = 100f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0.08f, 0.15f);
        [Tooltip("Smoothing factor for camera position (lower = smoother, 0 = no smoothing)")]
        [Range(0f, 1f)]
        [SerializeField] private float positionSmoothFactor = 0.1f;
        [Tooltip("Smoothing factor for camera rotation (lower = smoother, 0 = no smoothing)")]
        [Range(0f, 1f)]
        [SerializeField] private float rotationSmoothFactor = 0.3f;

        [Header("FOV Visualization")]
        [Tooltip("Make the observer agent's FOV mesh visible as a semi-transparent overlay")]
        [SerializeField] private bool visualizeFOV;
        [Tooltip("Color tint for the FOV mesh fill")]
        [SerializeField] private Color fovMeshColor = new Color(0f, 1f, 1f, 0.15f);
        [Tooltip("Color for the FOV mesh edges")]
        [SerializeField] private Color fovEdgeColor = new Color(0f, 1f, 1f, 0.9f);
        [Tooltip("Edge width (screen-space)")]
        [Range(0.5f, 5f)]
        [SerializeField] private float fovEdgeWidth = 1.5f;

        [Header("Output Settings")]
        [SerializeField] private string scenarioName = "default";
        [SerializeField] private string outputRootPath = "";
        [SerializeField] private int targetFrameRate = 30;

        private Camera fpCamera;
        private RenderTexture renderTexture;
        private Texture2D readbackTexture;
        private Transform headBone;
        private List<AgentPipelineCoordinator> allCoordinators;
        private int observerCoordinatorIndex;
        private StreamWriter csvWriter;
        private int frameCount;
        private string framesDirectory;
        private string outputDirectory;
        private bool captureEnabled;
        private bool isInitialized;
        private bool isCleanedUp;
        private FOVActiveController observerFOVController;
        private Transform observerTransform;
        private Material fovVisualizationMaterial;
        private Dictionary<MeshRenderer, Material> originalFovMaterials;
        private Camera gameViewCamera;
        private Camera disabledMainCamera;
        private AgentPipelineCoordinator observerCoordinator;
        private List<Renderer> hiddenObserverRenderers;

        public bool IsCapturing => captureEnabled && isInitialized;
        public int FrameCount => frameCount;

        public void StartCapture()
        {
            if (captureEnabled) return;
            captureEnabled = true;
            isCleanedUp = false;
            Debug.Log("[DataCapture] Capture requested. Will initialize on next frame.");
        }

        public void StopCapture()
        {
            if (!captureEnabled) return;
            Cleanup();
            captureEnabled = false;
        }

        /// <summary>
        /// Returns all agents currently in the scene via AvatarCreatorQuickGraph.
        /// Used by the custom Editor for dropdown population.
        /// </summary>
        public List<GameObject> GetSceneAgents()
        {
            var creator = FindAvatarCreator();
            if (creator == null) return new List<GameObject>();
            return creator.instantiatedAvatars ?? new List<GameObject>();
        }

        private AvatarCreatorQuickGraph FindAvatarCreator()
        {
#if UNITY_2023_1_OR_NEWER
            return FindAnyObjectByType<AvatarCreatorQuickGraph>();
#else
            return FindObjectOfType<AvatarCreatorQuickGraph>();
#endif
        }

        private Coroutine captureCoroutine;

        private void LateUpdate()
        {
            if (!captureEnabled) return;

            if (!isInitialized)
            {
                Initialize();
                if (isInitialized && captureCoroutine == null)
                {
                    captureCoroutine = StartCoroutine(EndOfFrameCapture());
                }
                return;
            }

            UpdateCameraTransform();
            WriteTrajectoryFrame();
        }

        /// <summary>
        /// Captures after all LateUpdates and rendering have finished,
        /// ensuring MotionMatching bone updates are applied.
        /// </summary>
        private System.Collections.IEnumerator EndOfFrameCapture()
        {
            while (captureEnabled && isInitialized)
            {
                yield return new WaitForEndOfFrame();
                if (!captureEnabled || !isInitialized) break;
                CaptureFrame();
                frameCount++;
            }
            captureCoroutine = null;
        }

        private void Initialize()
        {
            allCoordinators = FindAllCoordinators();
            if (allCoordinators.Count == 0)
            {
                return; // Wait for agents to be spawned
            }

            if (observerAgentIndex < 0 || observerAgentIndex >= allCoordinators.Count)
            {
                Debug.LogError($"[DataCapture] observerAgentIndex {observerAgentIndex} out of range (0-{allCoordinators.Count - 1}).");
                captureEnabled = false;
                return;
            }

            observerCoordinatorIndex = observerAgentIndex;
            observerCoordinator = allCoordinators[observerAgentIndex];

            GameObject observerObj = observerCoordinator.gameObject;
            // Walk up to find the root avatar (coordinator may be on a child)
            if (observerObj.transform.parent != null)
            {
                observerObj = observerObj.transform.root.gameObject;
            }

            if (!SetupCamera(observerObj))
            {
                captureEnabled = false;
                return;
            }

            observerTransform = observerObj.transform;

            // Hide observer mesh in first-person view
            // Use the avatar root (parent of coordinator), not observerObj which may be scene root
            var creator = FindAvatarCreator();
            GameObject avatarRoot = null;
            if (creator != null && creator.instantiatedAvatars != null)
            {
                avatarRoot = creator.instantiatedAvatars[observerAgentIndex];
            }
            if (avatarRoot == null) avatarRoot = observerCoordinator.gameObject;
            HideObserverMesh(avatarRoot);

            if (visualizeFOV)
            {
                SetupFOVVisualization(observerObj);
            }

            SetupOutput();
            WriteCameraJson();

            Time.captureFramerate = targetFrameRate;
            isInitialized = true;

            Debug.Log($"[DataCapture] Started. Observer: {observerObj.name} (index {observerAgentIndex}), " +
                      $"Recording {allCoordinators.Count - 1} other agents, Output: {outputDirectory}");
        }

        private List<AgentPipelineCoordinator> FindAllCoordinators()
        {
            // Try instantiatedAvatars first
            var creator = FindAvatarCreator();
            if (creator != null && creator.instantiatedAvatars != null && creator.instantiatedAvatars.Count > 0)
            {
                var coordinators = new List<AgentPipelineCoordinator>(creator.instantiatedAvatars.Count);
                foreach (GameObject avatar in creator.instantiatedAvatars)
                {
                    if (avatar == null) continue;
                    var coord = avatar.GetComponentInChildren<AgentPipelineCoordinator>();
                    if (coord != null) coordinators.Add(coord);
                }
                if (coordinators.Count > 0) return coordinators;
            }

            // Fallback: find all AgentPipelineCoordinators directly in scene
#if UNITY_2023_1_OR_NEWER
            var found = FindObjectsByType<AgentPipelineCoordinator>(FindObjectsSortMode.None);
#else
            var found = FindObjectsOfType<AgentPipelineCoordinator>();
#endif
            return new List<AgentPipelineCoordinator>(found);
        }

        private void UpdateCameraTransform()
        {
            if (headBone == null || fpCamera == null || observerTransform == null) return;

            // --- First-person: head position, gaze direction (where the face is looking) ---
            Vector3 fpPos = headBone.TransformPoint(cameraOffset);
            Vector3 gazeDir = (observerCoordinator != null)
                ? observerCoordinator.GazeState.CurrentLookAtDirection
                : Vector3.forward;
            if (gazeDir.sqrMagnitude < 0.001f) gazeDir = Vector3.forward;
            // Keep horizontal only (no pitch from gaze, prevents camera looking at ground)
            Vector3 horizontalFwd = new Vector3(gazeDir.x, 0f, gazeDir.z).normalized;
            if (horizontalFwd.sqrMagnitude < 0.001f) horizontalFwd = Vector3.forward;
            Quaternion fpRot = Quaternion.LookRotation(horizontalFwd, Vector3.up);

            // --- Bird view: directly above, looking straight down ---
            Vector3 agentPos = (observerCoordinator != null)
                ? (Vector3)observerCoordinator.GetCurrentPosition()
                : observerTransform.position;
            Vector3 bvPos = agentPos + Vector3.up * birdViewHeight;
            Vector3 bvForward = Vector3.down;
            Vector3 agentRight = Vector3.Cross(Vector3.up, horizontalFwd).normalized;
            if (birdViewTiltDeg > 0f)
            {
                bvForward = Quaternion.AngleAxis(-birdViewTiltDeg, agentRight) * Vector3.down;
            }
            Quaternion bvRot = Quaternion.LookRotation(bvForward, horizontalFwd);

            // --- Blend ---
            float t = Mathf.Clamp01(perspectiveBlend);
            Vector3 targetPos = Vector3.Lerp(fpPos, bvPos, t);
            Quaternion targetRot = Quaternion.Slerp(fpRot, bvRot, t);

            Transform cam = fpCamera.transform;

            if (positionSmoothFactor <= 0f)
            {
                cam.position = targetPos;
            }
            else
            {
                cam.position = Vector3.Lerp(cam.position, targetPos, positionSmoothFactor);
            }

            if (rotationSmoothFactor <= 0f)
            {
                cam.rotation = targetRot;
            }
            else
            {
                cam.rotation = Quaternion.Slerp(cam.rotation, targetRot, rotationSmoothFactor);
            }
        }

        private bool SetupCamera(GameObject observerRoot)
        {
            Animator animator = observerRoot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("[DataCapture] Observer agent has no Animator.");
                return false;
            }

            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone == null)
            {
                Debug.LogError("[DataCapture] Observer agent has no Head bone.");
                return false;
            }

            var camObj = new GameObject("1PP_CaptureCamera");
            fpCamera = camObj.AddComponent<Camera>();
            // Don't parent to head bone — we'll manually follow with smoothing
            Vector3 targetPos = headBone.TransformPoint(cameraOffset);
            camObj.transform.position = targetPos;
            camObj.transform.rotation = observerRoot.transform.rotation;

            fpCamera.fieldOfView = fieldOfView;
            fpCamera.nearClipPlane = nearClip;
            fpCamera.farClipPlane = farClip;
            fpCamera.enabled = false;

            renderTexture = new RenderTexture(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
            fpCamera.targetTexture = renderTexture;

            readbackTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

            // Mirror capture camera to Game view via a separate child camera
            var gameViewObj = new GameObject("1PP_GameViewCamera");
            gameViewObj.transform.SetParent(camObj.transform, false);
            gameViewCamera = gameViewObj.AddComponent<Camera>();
            gameViewCamera.fieldOfView = fieldOfView;
            gameViewCamera.nearClipPlane = nearClip;
            gameViewCamera.farClipPlane = farClip;
            gameViewCamera.depth = 100;
            gameViewCamera.targetTexture = null; // Renders to screen (Game view)
            gameViewCamera.enabled = true;

            // Disable existing Main Camera so it doesn't conflict
            var mainCam = Camera.main;
            if (mainCam != null && mainCam != gameViewCamera)
            {
                disabledMainCamera = mainCam;
                disabledMainCamera.enabled = false;
            }

            return true;
        }

        private void SetupOutput()
        {
            // Default: output to parent project root (1PP2Crowd/) instead of Unity project root
            string root = string.IsNullOrEmpty(outputRootPath)
                ? Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName
                : outputRootPath;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string runFolder = $"{scenarioName}_{timestamp}";
            outputDirectory = Path.Combine(root, "dataset", runFolder);
            framesDirectory = Path.Combine(outputDirectory, "frames");
            Directory.CreateDirectory(framesDirectory);

            string csvPath = Path.Combine(outputDirectory, "trajectories.csv");
            csvWriter = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
            csvWriter.WriteLine("frame,timestamp,agent_id,x,y,z,vx,vy,vz,direction_x,direction_y,direction_z,speed,group_name,visible,vp_x,vp_y");
        }

        private void WriteTrajectoryFrame()
        {
            float timestamp = Time.time;
            int csvAgentId = 0;

            for (int i = 0; i < allCoordinators.Count; i++)
            {
                if (i == observerCoordinatorIndex) continue;

                AgentPipelineCoordinator coord = allCoordinators[i];
                float3 pos = coord.GetCurrentPosition();
                Vector3 dir = coord.GetCurrentDirection();
                float speed = coord.GetCurrentSpeed();
                Vector3 vel = dir * speed;
                string groupName = coord.GetGroupName() ?? "Individual";

                // Visibility check: viewport bounds + wall occlusion
                Vector3 agentWorldPos = (Vector3)pos + Vector3.up * AgentBodyCenterHeight;
                Vector3 vp = fpCamera.WorldToViewportPoint(agentWorldPos);
                bool inViewport = vp.z > 0 && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

                int visible = 0;
                if (inViewport)
                {
                    // Raycast from camera to agent — check if a wall/obstacle blocks line of sight
                    Vector3 toAgent = agentWorldPos - fpCamera.transform.position;
                    visible = 1;
                    if (Physics.Raycast(fpCamera.transform.position, toAgent.normalized, out var hit, toAgent.magnitude))
                    {
                        if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Obstacle"))
                        {
                            visible = 0;
                        }
                    }
                }

                csvWriter.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1:F6},{2},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12:F4},{13},{14},{15:F4},{16:F4}",
                    frameCount, timestamp, csvAgentId,
                    pos.x, pos.y, pos.z,
                    vel.x, vel.y, vel.z,
                    dir.x, dir.y, dir.z,
                    speed, groupName,
                    visible, vp.x, vp.y));

                csvAgentId++;
            }

            if (frameCount % 100 == 0)
            {
                csvWriter.Flush();
            }
        }

        private void CaptureFrame()
        {
            fpCamera.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = renderTexture;
            readbackTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            readbackTexture.Apply();
            RenderTexture.active = prev;

            byte[] pngBytes = readbackTexture.EncodeToPNG();
            string filePath = Path.Combine(framesDirectory, $"frame_{frameCount:D6}.png");
            File.WriteAllBytes(filePath, pngBytes);
        }

        private void SetupFOVVisualization(GameObject observerRoot)
        {
            observerFOVController = observerRoot.GetComponentInChildren<FOVActiveController>();

            if (observerFOVController == null)
            {
                Debug.LogWarning("[DataCapture] FOV visualization enabled but no FOVActiveController found on observer.");
                return;
            }

            // Create material from the custom FOV shader
            var shader = Shader.Find("Custom/FOVVisualization");
            if (shader == null)
            {
                Debug.LogError("[DataCapture] Custom/FOVVisualization shader not found.");
                return;
            }

            fovVisualizationMaterial = new Material(shader);
            fovVisualizationMaterial.SetColor("_FillColor", fovMeshColor);
            fovVisualizationMaterial.SetColor("_EdgeColor", fovEdgeColor);
            fovVisualizationMaterial.SetFloat("_EdgeWidth", fovEdgeWidth);

            // Save original materials and replace with visualization shader
            originalFovMaterials = new Dictionary<MeshRenderer, Material>();
            foreach (Transform child in observerFOVController.transform)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    originalFovMaterials[mr] = mr.sharedMaterial;
                    mr.sharedMaterial = fovVisualizationMaterial;
                    mr.enabled = true;
                }
            }

            Debug.Log($"[DataCapture] FOV visualization: {originalFovMaterials.Count} meshes using Custom/FOVVisualization shader.");
        }

        private void RestoreFOVMaterials()
        {
            if (originalFovMaterials == null) return;
            foreach (var kvp in originalFovMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.sharedMaterial = kvp.Value;
                }
            }
            originalFovMaterials = null;
        }

        private void HideObserverMesh(GameObject observerRoot)
        {
            hiddenObserverRenderers = new List<Renderer>();
            foreach (var r in observerRoot.GetComponentsInChildren<Renderer>(true))
            {
                // Skip FOV visualization meshes (they should stay visible)
                if (r.GetComponent<FOVActiveController>() != null) continue;
                if (r.transform.parent != null && r.transform.parent.GetComponent<FOVActiveController>() != null) continue;

                if (r.enabled)
                {
                    hiddenObserverRenderers.Add(r);
                    r.enabled = false;
                }
            }

            if (hiddenObserverRenderers.Count > 0)
            {
                Debug.Log($"[DataCapture] Hidden {hiddenObserverRenderers.Count} renderers on observer agent.");
            }
        }

        private void ShowObserverMesh()
        {
            if (hiddenObserverRenderers == null) return;
            foreach (var r in hiddenObserverRenderers)
            {
                if (r != null) r.enabled = true;
            }
            hiddenObserverRenderers = null;
        }

        private void WriteCameraJson()
        {
            float vFovRad = fieldOfView * Mathf.Deg2Rad;
            float aspect = captureWidth / (float)captureHeight;
            float focalLengthPx = (captureHeight / 2f) / Mathf.Tan(vFovRad / 2f);
            float hFovDeg = 2f * Mathf.Atan(Mathf.Tan(vFovRad / 2f) * aspect) * Mathf.Rad2Deg;

            var intrinsics = new CameraIntrinsics
            {
                fov_vertical_deg = fieldOfView,
                fov_horizontal_deg = hFovDeg,
                width = captureWidth,
                height = captureHeight,
                near_clip = nearClip,
                far_clip = farClip,
                focal_length_px = focalLengthPx,
                cx = captureWidth / 2f,
                cy = captureHeight / 2f,
                aspect_ratio = aspect,
                target_frame_rate = targetFrameRate,
                observer_agent_index = observerAgentIndex,
                perspective_blend = perspectiveBlend,
                bird_view_height = birdViewHeight,
                fov_visualization_enabled = visualizeFOV,
                agent_fov_deg = (observerFOVController != null) ? (float)observerFOVController.currentFOV : -1f
            };

            string json = JsonUtility.ToJson(intrinsics, true);
            string jsonPath = Path.Combine(outputDirectory, "camera.json");
            File.WriteAllText(jsonPath, json);
        }

        private void Cleanup()
        {
            if (isCleanedUp) return;
            isCleanedUp = true;
            isInitialized = false;

            if (captureCoroutine != null)
            {
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
            }

            if (csvWriter != null)
            {
                csvWriter.Flush();
                csvWriter.Close();
                csvWriter = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (readbackTexture != null)
            {
                Destroy(readbackTexture);
            }

            if (disabledMainCamera != null)
            {
                disabledMainCamera.enabled = true;
                disabledMainCamera = null;
            }

            if (fpCamera != null)
            {
                Destroy(fpCamera.gameObject);
            }

            gameViewCamera = null;

            RestoreFOVMaterials();
            ShowObserverMesh();

            if (fovVisualizationMaterial != null)
            {
                Destroy(fovVisualizationMaterial);
            }

            Time.captureFramerate = 0;

            Debug.Log($"[DataCapture] Stopped. Total frames: {frameCount}");
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnApplicationQuit()
        {
            Cleanup();
        }

        [Serializable]
        private struct CameraIntrinsics
        {
            public float fov_vertical_deg;
            public float fov_horizontal_deg;
            public int width;
            public int height;
            public float near_clip;
            public float far_clip;
            public float focal_length_px;
            public float cx;
            public float cy;
            public float aspect_ratio;
            public int target_frame_rate;
            public int observer_agent_index;
            public float perspective_blend;
            public float bird_view_height;
            public bool fov_visualization_enabled;
            public float agent_fov_deg;
        }
    }
}
