using UnityEngine;
using UnityEditor;
using dog.miruku.inventory.runtime;

namespace dog.miruku.inventory
{
    [CustomEditor(typeof(Deprecated))]
    [CanEditMultipleObjects]
    public class DeprecatedEditor : Editor
    {
        static DeprecatedEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIconOnWindowItem;
        }


        private static void DrawIconOnWindowItem(int instanceID, Rect rect)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent<Deprecated>(out _))
            {
                var labelRect = new Rect(rect.xMax - 100, rect.yMin, rect.xMax - rect.width, rect.height);
                EditorGUI.LabelField(labelRect, "Need convert");
            }
        }
        private void ConvertToInventory()
        {
            // backward compatibility
            var item = target as Deprecated;
            var node = item.gameObject.AddComponent<Inventory>();
            node.Name = item.Name;
            node.Icon = item.Icon;
            node.Default = item.Default;
            node.AdditionalObjects = item.AdditionalObjects;
            node.AdditionalAnimations = item.AdditionalAnimations;
            DestroyImmediate(item);
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Convert to Inventory"))
                ConvertToInventory();
        }
    }
}