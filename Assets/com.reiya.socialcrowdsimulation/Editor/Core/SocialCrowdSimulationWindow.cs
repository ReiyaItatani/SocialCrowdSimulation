using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using MotionMatching;

#if UNITY_EDITOR
using UnityEditor;

namespace CollisionAvoidance
{
    public class SocialCrowdSimulationWindow : EditorWindow
    {
        // --- Section toggle states ---
        private bool _sceneSetupOpen = true;
        private bool _autoSetupOpen = true;
        private bool _playerCreatorOpen;

        // --- Auto Setup Agent state ---
        private AgentPrefabConfig _agentConfig;
        private bool _agentDefaultsLoaded;
        private List<string> _agentMissingAssets = new List<string>();
        private bool _agentAdvancedFoldout;

        // --- Create Player state ---
        private MotionMatchingData _playerMMData;
        private GameObject _playerHumanoid;

        private Vector2 _scrollPosition;

        [MenuItem("CollisionAvoidance/Social Crowd Simulation")]
        public static void ShowWindow()
        {
            var window = GetWindow<SocialCrowdSimulationWindow>("Social Crowd Simulation");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            LoadAgentDefaults();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawTitle();
            DrawSceneSetupSection();
            DrawAutoSetupSection();
            DrawPlayerCreatorSection();

            EditorGUILayout.EndScrollView();
        }

        // =============================================
        // Title
        // =============================================
        private void DrawTitle()
        {
            EditorGUILayout.LabelField("Social Crowd Simulation", EditorStyles.largeLabel);
            EditorGUILayout.LabelField(
                "Setup tools for crowd simulation agents and players.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);
        }

        // =============================================
        // Section 1: Scene Setup
        // =============================================
        private void DrawSceneSetupSection()
        {
            _sceneSetupOpen = DrawSectionHeader("Scene Setup - Add Tags & AvatarCreator", _sceneSetupOpen);
            if (!_sceneSetupOpen) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "Add required tags (Agent, Group, Wall, Object, Obstacle) and create " +
                "the AvatarCreator GameObject in the current scene. Run this once per scene.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Create AvatarCreator", GUILayout.Height(30)))
            {
                CreateAvatarCreator();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }

        private void CreateAvatarCreator()
        {
            AddTag("Agent");
            AddTag("Group");
            AddTag("Wall");
            AddTag("Object");
            AddTag("Obstacle");

            GameObject avatarCreator = new GameObject("AvatarCreator");
            avatarCreator.AddComponent<AvatarCreatorQuickGraph>();

            Selection.activeGameObject = avatarCreator;
            Debug.Log("AvatarCreator created with required tags.");
        }

        // =============================================
        // Section 2: Auto Setup Agent
        // =============================================
        private void DrawAutoSetupSection()
        {
            _autoSetupOpen = DrawSectionHeader("Auto Setup - Create Agent Prefab from Humanoid", _autoSetupOpen);
            if (!_autoSetupOpen) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "Drag a Humanoid model below to automatically generate a fully-configured " +
                "agent prefab. All required components (PathController, MotionMatching, " +
                "CollisionAvoidance, GazeController, etc.) are added automatically.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            DrawAgentStatusBanner();
            DrawAgentDropZone();
            DrawAgentAdvancedSettings();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }

        private void DrawAgentStatusBanner()
        {
            if (!_agentDefaultsLoaded)
            {
                EditorGUILayout.HelpBox(
                    "Default assets could not be located. Expand Advanced Settings to assign them manually.",
                    MessageType.Error);
            }
            else if (_agentMissingAssets.Count > 0)
            {
                string msg = "Missing: " + string.Join(", ", _agentMissingAssets) +
                             ". Expand Advanced Settings to assign.";
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
            }
        }

        private void DrawAgentDropZone()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 70f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop Humanoid Models Here", EditorStyles.helpBox);
            HandleAgentDrop(dropArea);
            EditorGUILayout.Space(4);
        }

        private void HandleAgentDrop(Rect dropArea)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        ProcessAgentDrop(DragAndDrop.objectReferences);
                        evt.Use();
                    }
                    break;
            }
        }

        private void ProcessAgentDrop(Object[] objects)
        {
            List<string> errors = new List<string>();
            int successCount = 0;

            foreach (Object obj in objects)
            {
                GameObject go = obj as GameObject;
                if (go == null)
                {
                    errors.Add($"'{obj.name}' is not a GameObject.");
                    continue;
                }

                string error = AgentPrefabFactory.Validate(go, _agentConfig);
                if (error != null)
                {
                    errors.Add($"'{go.name}': {error}");
                    continue;
                }

                string path = AgentPrefabFactory.CreatePrefab(go, _agentConfig);
                if (path != null)
                    successCount++;
            }

            if (successCount > 0)
                AssetDatabase.Refresh();

            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Auto Setup Agent",
                    $"Created {successCount} prefab(s).\n\nErrors:\n" + string.Join("\n", errors),
                    "OK");
            }
            else if (successCount > 0)
            {
                EditorUtility.DisplayDialog(
                    "Auto Setup Agent",
                    $"Successfully created {successCount} prefab(s).",
                    "OK");
            }
        }

        private void DrawAgentAdvancedSettings()
        {
            _agentAdvancedFoldout = EditorGUILayout.Foldout(_agentAdvancedFoldout, "Advanced Settings", true);
            if (!_agentAdvancedFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            _agentConfig.MMData = (MotionMatchingData)EditorGUILayout.ObjectField(
                "Motion Matching Data", _agentConfig.MMData, typeof(MotionMatchingData), false);
            _agentConfig.FOVMeshPrefab = (GameObject)EditorGUILayout.ObjectField(
                "FOV Mesh", _agentConfig.FOVMeshPrefab, typeof(GameObject), false);
            _agentConfig.AnimatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller", _agentConfig.AnimatorController, typeof(RuntimeAnimatorController), false);
            _agentConfig.AvatarMask = (AvatarMaskData)EditorGUILayout.ObjectField(
                "Avatar Mask", _agentConfig.AvatarMask, typeof(AvatarMaskData), false);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("SmartPhone Settings", EditorStyles.boldLabel);
            _agentConfig.PhonePrefab = (GameObject)EditorGUILayout.ObjectField(
                "SmartPhone Mesh", _agentConfig.PhonePrefab, typeof(GameObject), false);
            _agentConfig.PositionOffset = EditorGUILayout.Vector3Field("Position Offset", _agentConfig.PositionOffset);
            _agentConfig.RotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", _agentConfig.RotationOffset);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Audio Clips", EditorStyles.boldLabel);
            if (_agentConfig.AudioClips != null)
            {
                for (int i = 0; i < _agentConfig.AudioClips.Length; i++)
                {
                    _agentConfig.AudioClips[i] = (AudioClip)EditorGUILayout.ObjectField(
                        $"Clip {i + 1}", _agentConfig.AudioClips[i], typeof(AudioClip), false);
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add AudioClip"))
            {
                List<AudioClip> clips = new List<AudioClip>(_agentConfig.AudioClips ?? new AudioClip[0]);
                clips.Add(null);
                _agentConfig.AudioClips = clips.ToArray();
            }
            if (_agentConfig.AudioClips != null && _agentConfig.AudioClips.Length > 0)
            {
                if (GUILayout.Button("Remove AudioClip"))
                {
                    List<AudioClip> clips = new List<AudioClip>(_agentConfig.AudioClips);
                    clips.RemoveAt(clips.Count - 1);
                    _agentConfig.AudioClips = clips.ToArray();
                }
            }
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                RefreshAgentMissingAssets();

            EditorGUI.indentLevel--;
        }

        private void LoadAgentDefaults()
        {
            _agentConfig = DefaultAssetLocator.LoadDefaults();
            _agentDefaultsLoaded = _agentConfig.MMData != null
                                || _agentConfig.FOVMeshPrefab != null
                                || _agentConfig.AnimatorController != null;
            RefreshAgentMissingAssets();
        }

        private void RefreshAgentMissingAssets()
        {
            _agentMissingAssets.Clear();
            if (_agentConfig.MMData == null) _agentMissingAssets.Add("MotionMatchingData");
            if (_agentConfig.FOVMeshPrefab == null) _agentMissingAssets.Add("FOV Mesh");
            if (_agentConfig.AnimatorController == null) _agentMissingAssets.Add("Animator Controller");
            if (_agentConfig.AvatarMask == null) _agentMissingAssets.Add("Avatar Mask");
            if (_agentConfig.PhonePrefab == null) _agentMissingAssets.Add("Phone Mesh");
        }

        // =============================================
        // Section 3: Create Player
        // =============================================
        private void DrawPlayerCreatorSection()
        {
            _playerCreatorOpen = DrawSectionHeader("Create Player - First-Person Controllable Character", _playerCreatorOpen);
            if (!_playerCreatorOpen) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "Create a first-person controllable player from a Humanoid model. " +
                "Adds MotionMatching, InputManager, head camera, and physics components.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            _playerMMData = (MotionMatchingData)EditorGUILayout.ObjectField(
                "Motion Matching Data", _playerMMData, typeof(MotionMatchingData), false);

            _playerHumanoid = (GameObject)EditorGUILayout.ObjectField(
                "Humanoid Avatar", _playerHumanoid, typeof(GameObject), true);

            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(_playerHumanoid == null);
            if (GUILayout.Button("Create Player", GUILayout.Height(30)))
            {
                CreatePlayer();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }

        private void CreatePlayer()
        {
            if (_playerHumanoid == null) return;

            Animator anim = _playerHumanoid.GetComponent<Animator>();
            if (anim == null || !anim.isHuman)
            {
                EditorUtility.DisplayDialog("Create Player",
                    "The selected GameObject is not a Humanoid. Set the rig type to Humanoid in the model's import settings.",
                    "OK");
                return;
            }

            GameObject playerParent = new GameObject("Player");
            playerParent.tag = "Agent";

            GameObject instance = Instantiate(_playerHumanoid, Vector3.zero, Quaternion.identity, playerParent.transform);
            instance.name = _playerHumanoid.name + "_PlayerInstance";
            instance.tag = "Agent";

            Rigidbody rigidBody = instance.AddComponent<Rigidbody>();
            rigidBody.mass = 60f;
            rigidBody.useGravity = false;

            CapsuleCollider capsuleCollider = instance.AddComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = true;
            capsuleCollider.center = new Vector3(0, 0.9f, 0);
            capsuleCollider.radius = 0.25f;
            capsuleCollider.height = 1.8f;

            var motionMatchingRenderer = instance.AddComponent<MotionMatching.MotionMatchingSkinnedMeshRenderer>();
            motionMatchingRenderer.AvoidToesFloorPenetration = true;
            motionMatchingRenderer.ToesSoleOffset = new Vector3(0, 0, -0.02f);

            instance.AddComponent<SpringParameterManager>();

            Transform headTransform = instance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
            GameObject cameraGameObject = new GameObject("HeadCamera");
            cameraGameObject.AddComponent<Camera>();
            cameraGameObject.transform.parent = headTransform;
            cameraGameObject.transform.localPosition = Vector3.zero;
            cameraGameObject.transform.localRotation = Quaternion.identity;

            GameObject motionMatchingGO = CreateChild(playerParent, "MotionMatching");
            var motionMatchingController = motionMatchingGO.AddComponent<MotionMatchingController>();
            motionMatchingController.MMData = _playerMMData;
            motionMatchingController.FootLock = false;
            motionMatchingRenderer.MotionMatching = motionMatchingController;

            GameObject controllerGO = CreateChild(playerParent, "CharacterController");
            controllerGO.AddComponent<InputManager>();
            controllerGO.AddComponent<InputCharacterController>();
            var springController = controllerGO.AddComponent<CollisionAvoidance.SpringCharacterController>();
            springController.MotionMatching = motionMatchingController;
            motionMatchingController.CharacterController = springController;
            instance.GetComponent<SpringParameterManager>().springCharacterController = springController;

            Selection.activeGameObject = playerParent;
            EditorUtility.SetDirty(playerParent);

            Debug.Log("Player created successfully.");
        }

        // =============================================
        // Shared Utilities
        // =============================================
        private bool DrawSectionHeader(string title, bool isOpen)
        {
            EditorGUILayout.BeginHorizontal("box");
            isOpen = EditorGUILayout.Foldout(isOpen, title, true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();
            return isOpen;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void AddTag(string tag)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                    return;
            }

            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif
