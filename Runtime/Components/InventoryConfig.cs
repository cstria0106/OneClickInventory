using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace dog.miruku.inventory.runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("One-Click Inventory/Inventory Config")]
    public class InventoryConfig : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private string _customMenuName;
        public string CustomMenuName { get => _customMenuName; }
        [SerializeField] private Texture2D _customIcon;
        public Texture2D CustomIcon { get => _customIcon; }

        private void Reset()
        {
            _customMenuName = "Inventory";
        }
    }
}