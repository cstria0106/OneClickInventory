using System.Linq;
using dog.miruku.inventory.runtime;
using UnityEditor;

namespace dog.miruku.inventory
{
    public abstract class Menu
    {
        [MenuItem("GameObject/One-Click Inventory/Setup Inventory", false, 10)]
        public static void CreateInventoryOrItem()
        {
            var undoID = Undo.GetCurrentGroup();
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

        [MenuItem("GameObject/One-Click Inventory/Manual Bake", false, 11)]
        public static void ManualBake()
        {
            var undoID = Undo.GetCurrentGroup();
            foreach (var g in Selection.gameObjects)
            {
                var avatar = g.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                DebugUtil.CloneAndApply(avatar);
            }

            Undo.CollapseUndoOperations(undoID);
        }

        [MenuItem("GameObject/One-Click Inventory/Manual Bake", true)]
        public static bool ValidateManualBake()
        {
            return Selection.gameObjects.All(g =>
                g.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>() != null);
        }
    }
}