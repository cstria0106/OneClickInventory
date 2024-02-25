using UnityEditor;
using UnityEngine;
using dog.miruku.ndcloset.runtime;
using VRC.SDK3.Avatars.Components;
using System.Linq;
using System.Collections.Generic;

namespace dog.miruku.ndcloset
{
    [CustomEditor(typeof(Closet))]
    [CanEditMultipleObjects]
    public class ClosetEditor : Editor
    {
        private Closet Closet { get; set; }
        private SerializedProperty Name { get; set; }

        private SerializedProperty IsUnique { get; set; }

        private SerializedProperty Default { get; set; }
        private SerializedProperty AdditionalObjects { get; set; }
        private SerializedProperty AdditionalAnimations { get; set; }

        private bool _showItems;
        private ClosetEditorUtil.AvatarHierarchyFolding _avatarHierarchyFolding;

        static ClosetEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIconOnWindowItem;
        }

        private void OnEnable()
        {
            Closet = (Closet)target;
            Name = serializedObject.FindProperty("_closetName");

            IsUnique = serializedObject.FindProperty("_isUnique");

            Default = serializedObject.FindProperty("_default");
            AdditionalObjects = serializedObject.FindProperty("_additionalObjects");
            AdditionalAnimations = serializedObject.FindProperty("_additionalAnimations");

            _showItems = false;
            _avatarHierarchyFolding = new ClosetEditorUtil.AvatarHierarchyFolding();
        }

        private static void DrawIconOnWindowItem(int instanceID, Rect rect)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent(out Closet closet))
            {
                var size = rect.height;
                var labelRect = new Rect(rect.xMax - size, rect.yMin, size, size);

                if (closet.Default) GUI.DrawTexture(labelRect, CachedResource.Load<Texture2D>("ClosetDefault.png"));
                else GUI.DrawTexture(labelRect, CachedResource.Load<Texture2D>("Closet.png"));
            }
        }

        private static VRCAvatarDescriptor FindAvatar(Transform t)
        {
            if (t == null) return null;
            if (t.TryGetComponent<VRCAvatarDescriptor>(out var descriptor)) return descriptor;
            return FindAvatar(t.parent);
        }


        public override void OnInspectorGUI()
        {
            var avatar = FindAvatar(Closet.transform.parent);
            if (avatar == null)
            {
                EditorGUILayout.HelpBox(Localization.Get("noAvatar"), MessageType.Warning);
                return;
            }

            var node = ClosetNode.FindNodeByValue(avatar, Closet);
            node.Root.Validate();

            serializedObject.Update();

            EditorGUILayout.LabelField(Localization.Get("menu"), ClosetEditorUtil.HeaderStyle);
            EditorGUILayout.PropertyField(Name, new GUIContent(Localization.Get("name")));
            var texture = AssetPreview.GetAssetPreview(Closet.Icon);
            EditorGUILayout.LabelField(Localization.Get("customIcon"));
            GUILayout.BeginHorizontal();
            Closet.Icon = (Texture2D)EditorGUILayout.ObjectField(Closet.Icon, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100)); ;
            if (GUILayout.Button(Localization.Get("generateIcon")))
            {
                var icon = IconUtil.Generate(node);
                Closet.Icon = icon;
            }
            GUILayout.EndHorizontal();

            if (node.IsCloset)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Localization.Get("closet"), ClosetEditorUtil.HeaderStyle);
                EditorGUI.BeginDisabledGroup(true);
                _showItems = EditorGUILayout.Foldout(_showItems, Localization.Get("items"));
                if (_showItems)
                {
                    foreach (var child in node.Children)
                    {
                        EditorGUILayout.ObjectField(child.Value, typeof(Closet), false);
                    }
                }
                if (node.DefaultChild != null)
                {
                    EditorGUILayout.ObjectField(Localization.Get("defaultItem"), node.DefaultChild.Value, typeof(Closet), false);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(IsUnique, new GUIContent(Localization.Get("isUnique")));
            }

            if (node.IsItem)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Localization.Get("item"), ClosetEditorUtil.HeaderStyle);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(Localization.Get("closet"), node.Parent.Value, typeof(Closet), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(Default, new GUIContent(node.ParentIsUnique ? Localization.Get("defaultUnique") : Localization.Get("default")));
                EditorGUILayout.PropertyField(AdditionalObjects, new GUIContent(Localization.Get("additionalObjects")));
                EditorGUILayout.PropertyField(AdditionalAnimations, new GUIContent(Localization.Get("enabledAnimations")));
            }

            ClosetEditorUtil.Footer(node.Avatar, _avatarHierarchyFolding);
            serializedObject.ApplyModifiedProperties();

            // disable other default
            if (node.ParentIsUnique && node.Value.Default)
            {
                foreach (var e in node.Parent.Children.Where(e => e.Value != Closet))
                {
                    e.Value.Default = false;
                }
            }
        }
    }
}