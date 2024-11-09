using System.Collections.Generic;
using System.Linq;
using dog.miruku.inventory.runtime;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    public class AvatarHierarchyFolding
    {
        public bool Show;
        public readonly Dictionary<string, bool> NodesShow = new();
    }

    public abstract class InventoryEditorUtil
    {
        private static int SelectedLanguage { get; set; }

        public static GUIStyle HeaderStyle => new(EditorStyles.boldLabel);

        private static void AvatarHierarchy(InventoryNode node, int level, AvatarHierarchyFolding folding)
        {
            folding.NodesShow.TryAdd(node.Key, false);
            var menuItemsToInstall = node.MenuItemsToInstall.ToArray();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(level * 10, false);
            if (node.HasChildren || menuItemsToInstall.Length > 0)
            {
                folding.NodesShow[node.Key] = EditorGUILayout.BeginFoldoutHeaderGroup(folding.NodesShow[node.Key],
                    GUIContent.none,
                    new GUIStyle(EditorStyles.foldoutHeader)
                        {padding = new RectOffset(0, 0, 0, 0), stretchWidth = false});
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(node.Value, typeof(Inventory), true);
            EditorGUI.EndDisabledGroup();
            if (node.HasChildren || menuItemsToInstall.Length > 0)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.EndHorizontal();
            if (folding.NodesShow[node.Key])
            {
                foreach (var child in node.Children)
                {
                    AvatarHierarchy(child, level + 1, folding);
                }

                foreach (var menuItem in menuItemsToInstall)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space((level + 1) * 10, false);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(menuItem, typeof(Inventory), true);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private static void AvatarHierarchy(VRCAvatarDescriptor avatar, AvatarHierarchyFolding folding)
        {
            var rootNodes = InventoryNode.ResolveRootNodes(avatar);
            foreach (var node in rootNodes)
            {
                AvatarHierarchy(node, 0, folding);
            }
        }

        public static void Footer(VRCAvatarDescriptor avatar, AvatarHierarchyFolding folding)
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L.Get("avatar"), HeaderStyle);

            folding.Show = EditorGUILayout.Foldout(folding.Show, L.Get("avatarHierarchy"));
            if (folding.Show)
            {
                AvatarHierarchy(avatar, folding);
            }

            var usedParameterMemory = InventoryNode.ResolveRootNodes(avatar).Select(e => e.UsedParameterMemory).Sum();
            EditorGUILayout.LabelField($"{L.Get("usedParameterMemory")} : {usedParameterMemory}");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(L.Get("etc"), HeaderStyle);
            var selectedLanguage = EditorGUILayout.Popup(L.Get("language"), SelectedLanguage,
                L.Languages.Select(e => e.Item2).ToArray());
            if (selectedLanguage != SelectedLanguage)
            {
                SelectedLanguage = selectedLanguage;
                L.Language = L.Languages[SelectedLanguage].Item1;
                L.Get(L.Languages[SelectedLanguage].Item1);
            }
        }
    }
}