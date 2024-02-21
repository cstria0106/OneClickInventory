using UnityEditor;
using UnityEngine;
using dog.miruku.ndcloset.runtime;

namespace dog.miruku.ndcloset
{
    [CustomEditor(typeof(Closet))]
    public class ClosetEditor : Editor
    {
        private Closet _closet;
        private SerializedProperty _closetNameProperty;
        private SerializedProperty _isUniqueProperty;
        private SerializedProperty _customIconProperty;

        static ClosetEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIconOnWindowItem;
        }

        private void OnEnable()
        {
            _closet = (Closet)target;
            _closetNameProperty = serializedObject.FindProperty("_closetName");
            _isUniqueProperty = serializedObject.FindProperty("_isUnique");
            _customIconProperty = serializedObject.FindProperty("_customIcon");
        }

        private static void DrawIconOnWindowItem(int instanceID, Rect rect)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent<Closet>(out var closet))
            {
                var labelRect = new Rect(rect.xMax - 100,
                                       rect.yMin,
                                       rect.width,
                                       rect.height);
                EditorGUI.LabelField(labelRect, Localization.Get("closet"));
            }
        }

        public override void OnInspectorGUI()
        {
            var avatar = _closet.FindAvatar();
            serializedObject.Update();
            ClosetEditorUtil.Default();
            if (avatar == null)
            {
                EditorGUILayout.HelpBox(Localization.Get("noAvatar"), MessageType.Warning);
            }
            EditorGUILayout.PropertyField(_closetNameProperty, new GUIContent(Localization.Get("name")));
            EditorGUILayout.PropertyField(_isUniqueProperty, new GUIContent(Localization.Get("isUnique")));
            EditorGUILayout.PropertyField(_customIconProperty, new GUIContent(Localization.Get("customIcon")));
            serializedObject.ApplyModifiedProperties();
            _closet.Validate();
        }
    }
}