using dog.miruku.inventory.runtime;
using UnityEditor;
using UnityEngine;

namespace dog.miruku.inventory
{
    [CustomEditor(typeof(InventoryMenuInstaller))]
    [CanEditMultipleObjects]
    public class InventoryMenuInstallerEditor : Editor
    {
        private SerializedProperty _inventory;
        private readonly AvatarHierarchyFolding _folding = new();

        private void OnEnable()
        {
            _inventory = serializedObject.FindProperty("_inventory");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(
                "이 컴포넌트를 Menu Item 혹은 Menu Item Group와 함께 넣어두면 메뉴 생성 시 해당 인벤토리 혹은 아이템의 서브 메뉴로 설치됩니다.",
                new GUIStyle(EditorStyles.label) {wordWrap = true}
            );

            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUILayout.PropertyField(_inventory, new GUIContent(L.Get("inventory")));


            var avatar = Util.FindAvatar(
                (serializedObject.targetObject as InventoryMenuInstaller)?.transform
            );

            InventoryEditorUtil.Footer(avatar, _folding);

            serializedObject.ApplyModifiedProperties();
        }
    }
}