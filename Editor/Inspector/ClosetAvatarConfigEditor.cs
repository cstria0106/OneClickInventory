using UnityEngine;
using UnityEditor;
using dog.miruku.ndcloset.runtime;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.ndcloset
{
    [CustomEditor(typeof(ClosetAvatarConfig))]
    [CanEditMultipleObjects]
    public class ClosetAvatarConfigEditor : Editor
    {
        private SerializedProperty _customMenuNameProperty;
        private SerializedProperty _customIconProperty;
        private ClosetEditorUtil.AvatarHierarchyFolding _avatarHierarchyFolding;

        public void OnEnable()
        {
            _customMenuNameProperty = serializedObject.FindProperty("_customMenuName");
            _customIconProperty = serializedObject.FindProperty("_customIcon");

            _avatarHierarchyFolding = new ClosetEditorUtil.AvatarHierarchyFolding();
        }

        public override void OnInspectorGUI()
        {
            var avatar = (target as ClosetAvatarConfig).GetComponent<VRCAvatarDescriptor>();
            serializedObject.Update();
            ClosetEditorUtil.Footer(avatar, _avatarHierarchyFolding);
            EditorGUILayout.PropertyField(_customMenuNameProperty, new GUIContent(Localization.Get("customMenuName")));
            EditorGUILayout.PropertyField(_customIconProperty, new GUIContent(Localization.Get("customMenuIcon")));

            if (GUILayout.Button("Clone and apply for test"))
            {
                ClosetDebug.CloneAndApply(avatar);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}