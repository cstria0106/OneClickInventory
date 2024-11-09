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
            if (inventoryConfig != null && !inventoryConfig.TryGetComponent<VRCAvatarDescriptor>(out _))
            {
                EditorUtility.DisplayDialog("Error",
                    "InventoryConfig must be attached to the same GameObject as VRC Avatar Descriptor.", "OK");
                DestroyImmediate(inventoryConfig);
                return;
            }

            _customMenuNameProperty = serializedObject.FindProperty("_customMenuName");
            _customIconProperty = serializedObject.FindProperty("_customIcon");

            _avatarHierarchyFolding = new AvatarHierarchyFolding();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(
                "이 컴포넌트로 인벤토리 루트 메뉴의 속성을 설정할 수 있습니다.",
                new GUIStyle(EditorStyles.label) {wordWrap = true}
            );
            EditorGUILayout.Space();

            var avatar = (target as InventoryConfig)?.GetComponent<VRCAvatarDescriptor>();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_customMenuNameProperty, new GUIContent(L.Get("customMenuName")));
            EditorGUILayout.PropertyField(_customIconProperty, new GUIContent(L.Get("customMenuIcon")));

            InventoryEditorUtil.Footer(avatar, _avatarHierarchyFolding);
            serializedObject.ApplyModifiedProperties();
        }
    }
}