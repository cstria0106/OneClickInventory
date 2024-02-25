using System.Collections.Generic;
using System.Linq;
using dog.miruku.ndcloset.runtime;
using nadena.dev.ndmf.util;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.ndcloset
{
    public class ClosetEditorUtil
    {
        private static int SelectedLanguage { get; set; }

        public static GUIStyle HeaderStyle => new GUIStyle(EditorStyles.boldLabel);

        public class AvatarHierarchyFolding
        {
            public bool show = false;
            public Dictionary<string, bool> nodesShow = new Dictionary<string, bool>();
        }

        private static void AvatarHierarchy(ClosetNode node, int level, AvatarHierarchyFolding folding)
        {
            if (!folding.nodesShow.ContainsKey(node.Key)) folding.nodesShow[node.Key] = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(level * 10, false);
            if (node.HasChildren)
            {
                folding.nodesShow[node.Key] = EditorGUILayout.BeginFoldoutHeaderGroup(folding.nodesShow[node.Key], GUIContent.none, new GUIStyle(EditorStyles.foldoutHeader) { padding = new RectOffset(0, 0, 0, 0), stretchWidth = false });
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(node.Value, typeof(Closet), true);
            EditorGUI.EndDisabledGroup();
            if (node.HasChildren)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.EndHorizontal();
            if (folding.nodesShow[node.Key])
            {
                foreach (var child in node.Children)
                {
                    AvatarHierarchy(child, level + 1, folding);
                }
            }
        }

        private static void AvatarHierarchy(VRCAvatarDescriptor avatar, AvatarHierarchyFolding folding)
        {
            var rootNodes = ClosetNode.GetRootNodes(avatar);
            foreach (var node in rootNodes)
            {
                AvatarHierarchy(node, 0, folding);
            }
        }

        public static void Footer(VRCAvatarDescriptor avatar, AvatarHierarchyFolding folding)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Localization.Get("avatar"), HeaderStyle);

            folding.show = EditorGUILayout.Foldout(folding.show, Localization.Get("avatarHierarchy"));
            if (folding.show)
            {
                AvatarHierarchy(avatar, folding);
            }

            var usedParameterMemory = ClosetNode.GetRootNodes(avatar).Select(e => e.UsedParameterMemory).Sum();
            EditorGUILayout.LabelField($"{Localization.Get("usedParameterMemory")} : {usedParameterMemory}");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Localization.Get("etc"), HeaderStyle);
            var selectedLanguage = EditorGUILayout.Popup(Localization.Get("language"), SelectedLanguage, Localization.Languages.Select(e => e.Item2).ToArray());
            if (selectedLanguage != SelectedLanguage)
            {
                SelectedLanguage = selectedLanguage;
                Localization.Language = Localization.Languages[SelectedLanguage].Item1;
                Localization.Get(Localization.Languages[SelectedLanguage].Item1);
            }
        }
    }
}