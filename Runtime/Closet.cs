using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace dog.miruku.ndcloset.runtime
{
    [AddComponentMenu("Non-Destructive Closet/Closet")]
    public class Closet : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private string _closetName;
        [SerializeField] private bool _isUnique;

        public string ClosetName { get => _closetName; }


        public ClosetItem DefaultItem { get => Items.FirstOrDefault(item => item.Default); }

        public bool IsUnique { get => _isUnique; }

        public ClosetItem[] Items { get => GetComponentsInChildren<ClosetItem>(includeInactive: true); }

        [SerializeField] private Texture2D _customIcon;
        public Texture2D CustomIcon { get => _customIcon; }


        private void Reset()
        {
            _closetName = gameObject.name;
        }

        public void Validate()
        {
            if (_isUnique)
            {
                var items = Items;
                foreach (var item in items.Where(item => item.Default).Skip(1))
                {
                    item.Default = false;
                }
            }
        }
    }
}