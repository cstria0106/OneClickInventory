using UnityEngine;
using UnityEditor;
using dog.miruku.ndcloset.runtime;

namespace dog.miruku.ndcloset
{
    [CustomEditor(typeof(ClosetItem))]
    [CanEditMultipleObjects]
    public class ClosetItemEditor : Editor
    {
        static ClosetItemEditor()
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

            if (gameObject.TryGetComponent<ClosetItem>(out _))
            {
                var labelRect = new Rect(rect.xMax - 100, rect.yMin, rect.xMax - rect.width, rect.height);
                EditorGUI.LabelField(labelRect, "Need convert");
            }
        }
        private void ConvertToCloset()
        {
            // backward compatibility
            var item = target as ClosetItem;
            var closet = item.gameObject.AddComponent<Closet>();
            closet.Name = item.Name;
            closet.Icon = item.Icon;
            closet.Default = item.Default;
            closet.AdditionalObjects = item.AdditionalObjects;
            closet.AdditionalAnimations = item.AdditionalAnimations;
            DestroyImmediate(item);
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Convert to Closet"))
                ConvertToCloset();
        }
    }
}