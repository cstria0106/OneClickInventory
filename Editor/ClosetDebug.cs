
using System;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.ndcloset
{
    public class ClosetDebug
    {
        private static string ClosetNodeToString(ClosetNode node, int level = 0)
        {
            var s = "";
            for (int i = 0; i < level; i++) s += "    ";
            s += $"{node.Key}\n";
            foreach (var child in node.Children)
            {
                s += ClosetNodeToString(child, level + 1);
            }
            return s;
        }

        private static string AvatarClosetNodeTreeToString(VRCAvatarDescriptor avatar)
        {
            var s = "";
            foreach (var node in ClosetNode.GetRootNodes(avatar))
            {
                s += ClosetNodeToString(node);
            }
            return s;
        }

        public static void PrintAvatarClosetNodeTree(VRCAvatarDescriptor avatar)
        {
            Debug.Log(AvatarClosetNodeTreeToString(avatar));
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