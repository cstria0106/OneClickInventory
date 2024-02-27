using nadena.dev.ndmf;
using UnityEngine;
using dog.miruku.inventory.runtime;

[assembly: ExportsPlugin(typeof(dog.miruku.inventory.NDMFPlugin))]

namespace dog.miruku.inventory
{
    public class NDMFPlugin : Plugin<NDMFPlugin>
    {
        private static void ClearComponents<T>(Transform t) where T : Component
        {
            foreach (var e in t.GetComponentsInChildren<T>())
            {
                Object.DestroyImmediate(e);
            }
        }

        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing).Run("Clear inventory components", ctx =>
            {
                ClearComponents<InventoryConfig>(ctx.AvatarRootTransform);
                ClearComponents<Inventory>(ctx.AvatarRootTransform);
                ClearComponents<Deprecated>(ctx.AvatarRootTransform);
            });

            InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Generating inventory", ctx =>
            {
                Generator.Generate(ctx.AvatarDescriptor);
            });
        }
    }
}