using System.Collections.Generic;
using System.Linq;
using dog.miruku.inventory.runtime;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    public class InventoryNode
    {
        public string Key { get; }
        public int Index { get; }
        public Inventory Value { get; }
        public IEnumerable<GameObject> RelatedGameObjects => Value.GameObjects.Concat(Children.SelectMany(e => e.RelatedGameObjects));

        public VRCAvatarDescriptor Avatar { get; }
        public InventoryNode Parent { get; }
        public IEnumerable<InventoryNode> Children { get; }
        public bool HasChildren => Children.Count() > 0;
        public InventoryNode Root => Parent != null ? Parent.Root : this;
        public bool IsRoot => Root == this;
        public bool IsItem => !IsRoot;
        public bool IsInventory => HasChildren;
        public bool ParentIsUnique => Parent != null && Parent.Value.IsUnique;
        public InventoryNode DefaultChild => Value.IsUnique ? Children.Where(e => e.Value.Default).FirstOrDefault() : null;

        public string IndexKey => Key + "_index";
        public bool ParameterIsIndex => ParentIsUnique;
        public string ParameterName => Parent == null ? null : ParameterIsIndex ? Parent.IndexKey : Key;
        public int ParameterIntValue => ParameterIsIndex ? Index : 1;
        public int ParameterDefaultValue => ParameterIsIndex ? 0 : Value.Default ? 1 : 0;

        public InventoryNode(VRCAvatarDescriptor avatar, InventoryNode parent, Inventory value, int index)
        {
            Key = parent != null ? $"{parent.Key}_{index}" : $"{avatar.gameObject.name.Replace("_", "__")}_inventory_{index}";
            Index = index;
            Value = value;

            Avatar = avatar;
            Parent = parent;
            Children = ResolveChildren(value.transform, avatar, this);
        }

        public static List<InventoryNode> GetRootNodes(VRCAvatarDescriptor avatar)
        {
            return ResolveChildren(avatar.transform, avatar);
        }

        private static InventoryNode FindNodeByValue(InventoryNode node, Inventory value)
        {
            if (node.Value == value) return node;
            foreach (var child in node.Children)
            {
                var found = FindNodeByValue(child, value);
                if (found != null) return found;
            }
            return null;
        }

        public InventoryNode FindNodeByValue(Inventory value) => FindNodeByValue(this, value);
        public static InventoryNode FindNodeByValue(VRCAvatarDescriptor avatar, Inventory value) => GetRootNodes(avatar).Select(e => FindNodeByValue(e, value)).Where(e => e != null).FirstOrDefault();

        private static List<InventoryNode> ResolveChildren(Transform transform, VRCAvatarDescriptor avatar, InventoryNode parent = null, int index = 1)
        {
            var children = new List<InventoryNode>();
            if (transform == null) return children;
            foreach (Transform childTransform in transform)
            {
                if (childTransform.TryGetComponent(out Inventory value))
                {
                    if (parent != null && parent.Value.IsUnique && value.Default) // Is parent unique and this node is default
                    {
                        children.Add(new InventoryNode(avatar, parent, value, 0));
                    }
                    else
                    {
                        children.Add(new InventoryNode(avatar, parent, value, index));
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

        public int UsedParameterMemory => (IsInventory ? Value.IsUnique ? 8 : Children.Count() : 0) + Children.Select(e => e.UsedParameterMemory).Sum();
    }
}