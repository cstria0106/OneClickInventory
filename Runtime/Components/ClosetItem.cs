using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDKBase;

namespace dog.miruku.ndcloset.runtime
{
    [AddComponentMenu("Non-Destructive Closet/Closet Item")]
    public class ClosetItem : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private string _itemName;
        public string ItemName { get => _itemName; private set => _itemName = value; }
        [SerializeField] private bool _default;
        public bool Default { get => _default; set => _default = value; }
        [SerializeField] private List<GameObject> _additionalObjects = new List<GameObject>();

        [SerializeField] private Texture2D _customIcon;
        public Texture2D CustomIcon { get => _customIcon; set => _customIcon = value; }

        public Closet Closet { get => GetCloset(transform); }

        public IEnumerable<GameObject> GameObjects { get => new GameObject[] { gameObject }.Concat(_additionalObjects.Where(e => e != null)); }

        [SerializeField] private List<AnimationClip> _enabledAdditionalAnimations = new List<AnimationClip>();
        public IEnumerable<AnimationClip> EnabledAdditionalAnimations => _enabledAdditionalAnimations.Where(e => e != null);

        [SerializeField] private List<VRC_AvatarParameterDriver.Parameter> _enabledParameters = new List<VRC_AvatarParameterDriver.Parameter>();
        public IEnumerable<VRC_AvatarParameterDriver.Parameter> EnabledParameters => _enabledParameters;

        public void Validate()
        {
            var closet = Closet;
            if (closet != null && closet.IsUnique && _default)
            {
                var items = closet.Items;
                var others = items.Where(item => item != this);
                foreach (var other in others)
                {
                    other._default = false;
                }
            }
        }

        private void Reset()
        {
            ItemName = gameObject.name;
        }

        private Closet GetCloset(Transform t)
        {
            if (t == null) return null;
            if (t.TryGetComponent<Closet>(out var closet))
            {
                return closet;
            }
            return GetCloset(t.parent);
        }
    }
}