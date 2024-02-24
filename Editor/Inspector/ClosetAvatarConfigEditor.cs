using UnityEngine;
using UnityEditor;
using dog.miruku.ndcloset.runtime;

namespace dog.miruku.ndcloset
{
    [CustomEditor(typeof(ClosetAvatarConfig))]
    public class ClosetAvatarConfigEditor : Editor
    {
        private SerializedProperty _customMenuNameProperty;
        private SerializedProperty _customIconProperty;

        public void OnEnable()
        {
            _customMenuNameProperty = serializedObject.FindProperty("_customMenuName");
            _customIconProperty = serializedObject.FindProperty("_customIcon");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ClosetInspector.Default();
            EditorGUILayout.PropertyField(_customMenuNameProperty, new GUIContent(Localization.Get("customMenuName")));
            EditorGUILayout.PropertyField(_customIconProperty, new GUIContent(Localization.Get("customMenuIcon")));
            serializedObject.ApplyModifiedProperties();
        }
    }
}