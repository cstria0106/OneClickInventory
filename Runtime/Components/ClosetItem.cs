using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDKBase;

namespace dog.miruku.inventory.runtime
{
    [DisallowMultipleComponent]
    public class Deprecated : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private string _itemName;
        public string Name => _itemName;

        [SerializeField] private Texture2D _customIcon;
        public Texture2D Icon { get => _customIcon; set => _customIcon = value; }

        // properties as a item
        [SerializeField] bool _default;
        public bool Default => _default;

        [SerializeField] private List<GameObject> _additionalObjects = new List<GameObject>();
        public IEnumerable<GameObject> AdditionalObjects { get => _additionalObjects.Where(e => e != null); }
        public IEnumerable<GameObject> GameObjects => new GameObject[] { gameObject }.Concat(_additionalObjects.Where(e => e != null));
        [SerializeField] private List<AnimationClip> _enabledAdditionalAnimations = new List<AnimationClip>();
        public IEnumerable<AnimationClip> AdditionalAnimations => _enabledAdditionalAnimations.Where(e => e != null);

        private void Reset()
        {
            _itemName = gameObject.name;
        }
    }
}