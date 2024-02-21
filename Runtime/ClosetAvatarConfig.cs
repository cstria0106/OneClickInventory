using UnityEngine;
using VRC.SDKBase;

namespace dog.miruku.ndcloset.runtime
{
    [AddComponentMenu("Non-Destructive Closet/Closet Avatar Config")]
    public class ClosetAvatarConfig : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private string _customMenuName;
        public string CustomMenuName { get => _customMenuName; }
        [SerializeField] private Texture2D _customIcon;
        public Texture2D CustomIcon { get => _customIcon; }

        private void Reset()
        {
            _customMenuName = "Closet";
        }
    }
}