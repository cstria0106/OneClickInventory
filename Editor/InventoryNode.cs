using System.Collections.Generic;
using System.Linq;
using dog.miruku.inventory.runtime;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    public class InventoryNode
    {
        // node
        public string Key { get; }
        public int Index { get; }
        public Inventory Value { get; }
        public string EscapedName => Util.EscapeStateMachineName(Value.Name);

        // tree
        public VRCAvatarDescriptor Avatar { get; }
        public InventoryNode Parent { get; }
        public IEnumerable<InventoryNode> Children { get; }
        public IEnumerable<InventoryNode> ChildItems => Children.Where(e => e.IsItem);
        public bool HasChildren => Children.Any();
        public bool HasChildItems => ChildItems.Any();
        public InventoryNode Root => Parent != null ? Parent.Root : this;
        public bool IsRoot => Root == this;

        public IEnumerable<GameObject> RelatedGameObjects =>
            Value.GameObjects.Concat(Children.SelectMany(e => e.RelatedGameObjects));

        public IEnumerable<InventoryMenuInstaller> MenuItemsToInstall => Root.Avatar
            .GetComponentsInChildren<InventoryMenuInstaller>(includeInactive: true).Where(e => e.Inventory == Value);

        public int UsedParameterMemory => (IsInventory ? Value.IsUnique ? ChildrenBits : ChildItems.Count() : 0) +
                                          Children.Select(e => e.UsedParameterMemory).Sum();

        // as a inventory
        public bool IsInventory => HasChildItems;

        public InventoryNode DefaultChild =>
            Value.IsUnique ? ChildItems.FirstOrDefault(e => e.Value.Default) : null;

        public int MaxChildrenIndex => ChildItems.Select(e => e.Index).DefaultIfEmpty(0).Max();
        public int ChildrenBits => Mathf.CeilToInt(Mathf.Log(MaxChildrenIndex + 1, 2));

        // as a item
        public bool CanBeItem => !IsRoot;
        public bool IsItem => CanBeItem && !Value.IsNotItem;
        public bool ParentIsUnique => Parent != null && Parent.Value.IsUnique;
        public string ParameterName => ParentIsUnique ? Parent.Key : $"{Key}/Toggle";
        public int ParameterValue => ParentIsUnique ? Index : 1;
        public int ParameterBits => ParentIsUnique ? Parent.ChildrenBits : 1;
        public int ParameterDefault => ParentIsUnique ? 0 : Value.Default ? 1 : 0;

        private InventoryNode(VRCAvatarDescriptor avatar, InventoryNode parent, Inventory value, int index,
            Dictionary<string, int> nameCount)
        {
            if (!nameCount.TryAdd(value.Name, 1)) nameCount[value.Name] += 1;

            var name = nameCount[value.Name] > 1 ? $"{value.Name}_{nameCount[value.Name]}" : value.Name;

            Key = parent != null
                ? $"{parent.Key}/{Util.EscapeStateMachineName(name)}"
                : $"OCInv/{Util.EscapeStateMachineName(name)}";
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
            return node.Value == value
                ? node
                : node.Children.Select(child => FindNodeByValue(child, value)).FirstOrDefault(found => found != null);
        }

        public static InventoryNode FindNodeByValue(VRCAvatarDescriptor avatar, Inventory value) => GetRootNodes(avatar)
            .Select(e => FindNodeByValue(e, value)).FirstOrDefault(e => e != null);


        private static List<InventoryNode> ResolveChildren(Transform transform, VRCAvatarDescriptor avatar,
            InventoryNode parent = null)
        {
            return ResolveChildren(transform, avatar, parent, new Dictionary<string, int>());
        }

        private static List<InventoryNode> ResolveChildren(Transform transform, VRCAvatarDescriptor avatar,
            InventoryNode parent, Dictionary<string, int> nameCount, int index = 1)
        {
            var children = new List<InventoryNode>();
            if (transform == null) return children;
            foreach (Transform childTransform in transform)
            {
                if (childTransform.TryGetComponent(out Inventory value))
                {
                    if (parent != null && parent.Value.IsUnique &&
                        value.Default) // Is parent unique and this node is default
                    {
                        children.Add(new InventoryNode(avatar, parent, value, 0, nameCount));
                    }
                    else
                    {
                        if (value.IsNotItem)
                        {
                            children.Add(new InventoryNode(avatar, parent, value, -1, nameCount));
                        }
                        else
                        {
                            children.Add(new InventoryNode(avatar, parent, value, index, nameCount));
                            index += 1;
                        }
                    }
                }
                else
                {
                    var resolved = ResolveChildren(childTransform, avatar, parent, nameCount, index);
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
    }
}