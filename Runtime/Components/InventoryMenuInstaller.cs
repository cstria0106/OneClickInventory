using UnityEngine;
using VRC.SDKBase;

namespace dog.miruku.inventory.runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("One-Click Inventory/Inventory Menu Installer")]
    public class InventoryMenuInstaller : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private Inventory _inventory;
        public Inventory Inventory => _inventory;
    }
}