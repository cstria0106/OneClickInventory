using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace dog.miruku.ndcloset.runtime
{
    [Serializable]
    public struct SetBlendShapeBinding
    {
        public SkinnedMeshRenderer renderer;
        public string name;
        public int value;
    }

    [Serializable]
    public struct ReplaceMaterialBinding
    {
        public Renderer renderer;
        public Material from;
        public Material to;
    }

    [Serializable]
    public struct ParameterDriverBinding
    {
        public VRC_AvatarParameterDriver.Parameter parameter;
    }

    [AddComponentMenu("Non-Destructive Closet/Closet")]
    public class Closet : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private string _closetName;
        public string Name { get => _closetName; set => _closetName = value; }

        [SerializeField] private Texture2D _customIcon;
        public Texture2D Icon { get => _customIcon; set => _customIcon = value; }

        // properties as a category
        [SerializeField] private bool _isUnique;
        public bool IsUnique { get => _isUnique; set => _isUnique = value; }

        // properties as a item
        [SerializeField] bool _default;
        public bool Default { get => _default; set => _default = value; }

        [SerializeField] private List<GameObject> _additionalObjects = new List<GameObject>();
        public IEnumerable<GameObject> AdditionalObjects
        {
            get => _additionalObjects.Where(e => e != null);
            set => _additionalObjects = value.ToList();
        }
        public IEnumerable<GameObject> GameObjects => new GameObject[] { gameObject }.Concat(_additionalObjects.Where(e => e != null));
        [SerializeField] private List<AnimationClip> _additionalAnimations = new List<AnimationClip>();
        public IEnumerable<AnimationClip> AdditionalAnimations
        {
            get => _additionalAnimations.Where(e => e != null);
            set => _additionalAnimations = value.ToList();
        }

        [SerializeField] private List<GameObject> _objectsToDisable = new List<GameObject>();
        public IEnumerable<GameObject> ObjectsToDisable => _objectsToDisable.Where(e => e != null);

        [SerializeField] private List<SetBlendShapeBinding> _blendShapesToChange = new List<SetBlendShapeBinding>();
        public IEnumerable<SetBlendShapeBinding> BlendShapesToChange => _blendShapesToChange.Where(e => e.renderer != null);

        [SerializeField] private List<ReplaceMaterialBinding> _materialsToReplace = new List<ReplaceMaterialBinding>();
        public IEnumerable<ReplaceMaterialBinding> MaterialsToReplace => _materialsToReplace.Where(e => e.renderer != null && e.from != null && e.to != null);

        [SerializeField] private List<ParameterDriverBinding> _parameterDriverBindings = new List<ParameterDriverBinding>();
        public IEnumerable<ParameterDriverBinding> ParameterDriverBindings => _parameterDriverBindings;

        private void Reset()
        {
            _closetName = gameObject.name;
        }
    }
}