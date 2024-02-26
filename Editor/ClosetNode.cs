using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Codice.Client.Common.TreeGrouper;
using dog.miruku.ndcloset.runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.ndcloset
{
    public class ClosetNode
    {
        public string Key { get; }
        public int Index { get; }
        public Closet Value { get; }
        public IEnumerable<GameObject> RelatedGameObjects => Value.GameObjects.Concat(Children.SelectMany(e => e.RelatedGameObjects));

        public VRCAvatarDescriptor Avatar { get; }
        public ClosetNode Parent { get; }
        public IEnumerable<ClosetNode> Children { get; }
        public bool HasChildren => Children.Count() > 0;
        public ClosetNode Root => Parent != null ? Parent.Root : this;
        public bool IsRoot => Root == this;
        public bool IsItem => !IsRoot;
        public bool IsCloset => HasChildren;
        public bool ParentIsUnique => Parent != null && Parent.Value.IsUnique;
        public ClosetNode DefaultChild => Value.IsUnique ? Children.Where(e => e.Value.Default).FirstOrDefault() : null;

        public string IndexKey => Key + "_index";
        public bool ParameterIsIndex => ParentIsUnique;
        public string ParameterName => Parent == null ? null : ParameterIsIndex ? Parent.IndexKey : Key;
        public int ParameterIntValue => ParameterIsIndex ? Index : 1;
        public int ParameterDefaultValue => ParameterIsIndex ? 0 : Value.Default ? 1 : 0;

        public ClosetNode(VRCAvatarDescriptor avatar, ClosetNode parent, Closet value, int index)
        {
            Key = parent != null ? $"{parent.Key}_{index}" : $"{avatar.gameObject.name.Replace("_", "__")}_closet_{index}";
            Index = index;
            Value = value;

            Avatar = avatar;
            Parent = parent;
            Children = ResolveChildren(value.transform, avatar, this);
        }

        public static List<ClosetNode> GetRootNodes(VRCAvatarDescriptor avatar)
        {
            return ResolveChildren(avatar.transform, avatar);
        }

        private static ClosetNode FindNodeByValue(ClosetNode node, Closet value)
        {
            if (node.Value == value) return node;
            foreach (var child in node.Children)
            {
                var found = FindNodeByValue(child, value);
                if (found != null) return found;
            }
            return null;
        }

        public ClosetNode FindNodeByValue(Closet value) => FindNodeByValue(this, value);
        public static ClosetNode FindNodeByValue(VRCAvatarDescriptor avatar, Closet value) => GetRootNodes(avatar).Select(e => FindNodeByValue(e, value)).Where(e => e != null).FirstOrDefault();

        private static List<ClosetNode> ResolveChildren(Transform transform, VRCAvatarDescriptor avatar, ClosetNode parent = null, int index = 1)
        {
            var children = new List<ClosetNode>();
            if (transform == null) return children;
            foreach (Transform childTransform in transform)
            {
                if (childTransform.TryGetComponent(out Closet value))
                {
                    if (parent != null && parent.Value.IsUnique && value.Default) // Is parent unique and this node is default
                    {
                        children.Add(new ClosetNode(avatar, parent, value, 0));
                    }
                    else
                    {
                        children.Add(new ClosetNode(avatar, parent, value, index));
                        index += 1;
                    }
                }
                else
                {
                    var resolved = ResolveChildren(childTransform, avatar, parent, index);
                    children.AddRange(resolved);
                    index += resolved.Count;
                }
            }
            return children;
        }

        public void Validate()
        {
            // make only one child to be default
            if (Value.IsUnique)
            {
                foreach (var child in Children.Where(e => e.Value.Default).Skip(1))
                {
                    child.Value.Default = false;
                }
            }

            foreach (var child in Children)
            {
                child.Validate();
            }
        }

        public int UsedParameterMemory => (IsCloset ? Value.IsUnique ? 8 : Children.Count() : 0) + Children.Select(e => e.UsedParameterMemory).Sum();
    }
}