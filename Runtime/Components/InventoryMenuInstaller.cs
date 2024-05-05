
using UnityEngine;

namespace dog.miruku.inventory.runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("One-Click Inventory/Inventory Menu Installer")]
    public class InventoryMenuInstaller : MonoBehaviour
    {
        [SerializeField] private Inventory _inventory;
        public Inventory Inventory { get => _inventory; set => _inventory = value; }
    }
}