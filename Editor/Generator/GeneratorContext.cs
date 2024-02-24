using System.Collections.Generic;
using System.Linq;
using dog.miruku.ndcloset.runtime;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.ndcloset
{
    public class GeneratorContext
    {
        private readonly Closet _closet;
        private readonly string _closetId;
        private readonly Dictionary<ClosetItem, int> _itemIndexes = new Dictionary<ClosetItem, int>();
        private readonly VRCAvatarDescriptor _avatar;

        public Closet Closet => _closet;
        public string ClosetId => _closetId;
        public VRCAvatarDescriptor Avatar => _avatar;

        public GeneratorContext(Closet closet, VRCAvatarDescriptor avatar)
        {
            _closet = closet;
            _avatar = avatar;
            _closetId = $"{closet.ClosetName}_{GUID.Generate()}";
            _itemIndexes = GenerateItemIndexes(closet);
        }

        private static Dictionary<ClosetItem, int> GenerateItemIndexes(Closet closet)
        {
            var indexes = new Dictionary<ClosetItem, int>();
            int index = 1;
            foreach (var item in closet.Items)
            {
                if (closet.IsUnique && item.Default) indexes[item] = 0;
                else indexes[item] = index++;
            }
            return indexes;
        }

        public int ItemIndex(ClosetItem item)
        {
            return _itemIndexes[item];
        }

        public int MinIndex => _itemIndexes.Values.Min();
        public int MaxIndex => _itemIndexes.Values.Max();

        public string NonUniqueParameterName(ClosetItem item)
        {
            return $"{_closetId}_{ItemIndex(item)}";
        }
    }
}