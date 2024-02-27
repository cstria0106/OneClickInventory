using UnityEngine;
using UnityEditor;
using dog.miruku.inventory.runtime;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    [CustomEditor(typeof(InventoryConfig))]
    [CanEditMultipleObjects]
    public class InventoryConfigEditor : Editor
    {
        private SerializedProperty _customMenuNameProperty;
        private SerializedProperty _customIconProperty;
        private AvatarHierarchyFolding _avatarHierarchyFolding;

        public void OnEnable()
        {
            var inventoryConfig = target as InventoryConfig;
            if (!inventoryConfig.TryGetComponent<VRCAvatarDescriptor>(out _))
            {
                EditorUtility.DisplayDialog("Error", "InventoryConfig must be attached to the same GameObject as VRC Avatar Descriptor.", "OK");
                DestroyImmediate(inventoryConfig);
                return;
            }

            _customMenuNameProperty = serializedObject.FindProperty("_customMenuName");
            _customIconProperty = serializedObject.FindProperty("_customIcon");

            _avatarHierarchyFolding = new AvatarHierarchyFolding();
        }

        public override void OnInspectorGUI()
        {
            var avatar = (target as InventoryConfig).GetComponent<VRCAvatarDescriptor>();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_customMenuNameProperty, new GUIContent(Localization.Get("customMenuName")));
            EditorGUILayout.PropertyField(_customIconProperty, new GUIContent(Localization.Get("customMenuIcon")));

            if (GUILayout.Button("Clone and apply for test"))
            {
                DebugUtil.CloneAndApply(avatar);
            }
            InventoryEditorUtil.Footer(avatar, _avatarHierarchyFolding);
            serializedObject.ApplyModifiedProperties();
        }
    }
}