
using dog.miruku.inventory.runtime;
using UnityEditor;

namespace dog.miruku.inventory
{
    public class Menu
    {
        [MenuItem("GameObject/One-Click Inventory/Setup Inventory", false, 10)]
        public static void CreateInventoryOrItem()
        {
            int undoID = Undo.GetCurrentGroup();
            foreach (var g in Selection.gameObjects)
            {
                var inventory = g.GetComponent<Inventory>();
                if (inventory == null)
                {
                    inventory = g.AddComponent<Inventory>();
                }

                Undo.RegisterCreatedObjectUndo(inventory, "Create Inventory Or Item");
                Undo.CollapseUndoOperations(undoID);
            }
        }
    }
}