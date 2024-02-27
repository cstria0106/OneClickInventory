
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    public class DebugUtil
    {
        private static string InventoryNodeToString(InventoryNode node, int level = 0)
        {
            var s = "";
            for (int i = 0; i < level; i++) s += "    ";
            s += $"{node.Key}\n";
            foreach (var child in node.Children)
            {
                s += InventoryNodeToString(child, level + 1);
            }
            return s;
        }

        private static string AvatarInventoryHierarchyToString(VRCAvatarDescriptor avatar)
        {
            var s = "";
            foreach (var node in InventoryNode.GetRootNodes(avatar))
            {
                s += InventoryNodeToString(node);
            }
            return s;
        }

        public static void PrintAvatarInventoryHierarchy(VRCAvatarDescriptor avatar)
        {
            Debug.Log(AvatarInventoryHierarchyToString(avatar));
        }

        public static void CloneAndApply(VRCAvatarDescriptor avatar)
        {
            var cloned = GameObject.Instantiate(avatar.gameObject);
            cloned.name = avatar.gameObject.name + "(Clone)";
            try
            {
                Generator.Generate(cloned.GetComponent<VRCAvatarDescriptor>());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                GameObject.DestroyImmediate(cloned);
            }
        }
    }
}