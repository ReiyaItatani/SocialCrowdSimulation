using UnityEngine;
using MotionMatching;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace CollisionAvoidance
{
    public class AutoSetupWindow : EditorWindow
    {
        private AgentPrefabConfig _config;
        private bool _advancedFoldout;
        private bool _defaultsLoaded;
        private List<string> _missingAssets = new List<string>();

        // Menu item moved to SocialCrowdSimulationWindow.
        public static void ShowWindow()
        {
            GetWindow<AutoSetupWindow>("Auto Setup Agent");
        }

        private void OnEnable()
        {
            TryLoadDefaults();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("box");

            DrawHeader();
            DrawStatusBanner();
            DrawDropZone();
            DrawAdvancedSettings();

            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            GUILayout.Label("Auto Setup Agent", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Drop humanoid models below to generate Social Crowd Simulation prefabs automatically.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();
        }

        private void DrawStatusBanner()
        {
            if (!_defaultsLoaded)
            {
                EditorGUILayout.HelpBox(
                    "Default assets could not be loaded. Use Advanced Settings to assign them manually.",
                    MessageType.Error);
            }
            else if (_missingAssets.Count > 0)
            {
                string msg = "Some default assets were not found:\n- " +
                             string.Join("\n- ", _missingAssets) +
                             "\n\nExpand Advanced Settings to assign them manually.";
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("All default assets loaded. Ready to create prefabs.", MessageType.Info);
            }

            EditorGUILayout.Space();
        }

        private void DrawDropZone()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 80f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop Humanoid Models Here", EditorStyles.helpBox);
            HandleDrop(dropArea);
            EditorGUILayout.Space();
        }

        private void HandleDrop(Rect dropArea)
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
                        ProcessDroppedObjects(DragAndDrop.objectReferences);
                        evt.Use();
                    }
                    break;
            }
        }

        private void ProcessDroppedObjects(Object[] objects)
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

                string error = AgentPrefabFactory.Validate(go, _config);
                if (error != null)
                {
                    errors.Add($"'{go.name}': {error}");
                    continue;
                }

                string path = AgentPrefabFactory.CreatePrefab(go, _config);
                if (path != null)
                    successCount++;
            }

            if (successCount > 0)
            {
                AssetDatabase.Refresh();
            }

            if (errors.Count > 0)
            {
                string errorMsg = string.Join("\n", errors);
                EditorUtility.DisplayDialog(
                    "Auto Setup Agent",
                    $"Created {successCount} prefab(s).\n\nErrors:\n{errorMsg}",
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

        private void DrawAdvancedSettings()
        {
            _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, "Advanced Settings", true);
            if (!_advancedFoldout)
                return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            _config.MMData = (MotionMatchingData)EditorGUILayout.ObjectField(
                "Motion Matching Data", _config.MMData, typeof(MotionMatchingData), false);

            _config.FOVMeshPrefab = (GameObject)EditorGUILayout.ObjectField(
                "FOV Mesh", _config.FOVMeshPrefab, typeof(GameObject), false);

            _config.AnimatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller", _config.AnimatorController, typeof(RuntimeAnimatorController), false);

            _config.AvatarMask = (AvatarMaskData)EditorGUILayout.ObjectField(
                "Avatar Mask", _config.AvatarMask, typeof(AvatarMaskData), false);

            EditorGUILayout.Space();
            GUILayout.Label("SmartPhone Settings", EditorStyles.boldLabel);

            _config.PhonePrefab = (GameObject)EditorGUILayout.ObjectField(
                "SmartPhone Mesh", _config.PhonePrefab, typeof(GameObject), false);

            _config.PositionOffset = EditorGUILayout.Vector3Field("Position Offset", _config.PositionOffset);
            _config.RotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", _config.RotationOffset);

            EditorGUILayout.Space();
            GUILayout.Label("Audio Clips", EditorStyles.boldLabel);

            if (_config.AudioClips != null)
            {
                for (int i = 0; i < _config.AudioClips.Length; i++)
                {
                    _config.AudioClips[i] = (AudioClip)EditorGUILayout.ObjectField(
                        $"Clip {i + 1}", _config.AudioClips[i], typeof(AudioClip), false);
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add AudioClip"))
            {
                List<AudioClip> clips = new List<AudioClip>(_config.AudioClips ?? new AudioClip[0]);
                clips.Add(null);
                _config.AudioClips = clips.ToArray();
            }
            if (_config.AudioClips != null && _config.AudioClips.Length > 0)
            {
                if (GUILayout.Button("Remove AudioClip"))
                {
                    List<AudioClip> clips = new List<AudioClip>(_config.AudioClips);
                    clips.RemoveAt(clips.Count - 1);
                    _config.AudioClips = clips.ToArray();
                }
            }
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                RefreshMissingAssets();
            }

            EditorGUI.indentLevel--;
        }

        private void TryLoadDefaults()
        {
            _config = DefaultAssetLocator.LoadDefaults();
            _defaultsLoaded = _config.MMData != null
                           || _config.FOVMeshPrefab != null
                           || _config.AnimatorController != null;
            RefreshMissingAssets();
        }

        private void RefreshMissingAssets()
        {
            _missingAssets.Clear();

            if (_config.MMData == null) _missingAssets.Add("MotionMatchingData");
            if (_config.FOVMeshPrefab == null) _missingAssets.Add("FOV Mesh Prefab");
            if (_config.AnimatorController == null) _missingAssets.Add("Animator Controller");
            if (_config.AvatarMask == null) _missingAssets.Add("Avatar Mask");
            if (_config.PhonePrefab == null) _missingAssets.Add("Phone Mesh Prefab");
        }
    }
}
#endif
