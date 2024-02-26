using nadena.dev.ndmf;
using UnityEngine;
using dog.miruku.ndcloset.runtime;

[assembly: ExportsPlugin(typeof(dog.miruku.ndcloset.NDMFPlugin))]

namespace dog.miruku.ndcloset
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
            InPhase(BuildPhase.Optimizing).Run("Clear closet components", ctx =>
            {
                ClearComponents<ClosetAvatarConfig>(ctx.AvatarRootTransform);
                ClearComponents<Closet>(ctx.AvatarRootTransform);
                ClearComponents<ClosetItem>(ctx.AvatarRootTransform);
            });

            InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Generating closet", ctx =>
            {
                Generator.Generate(ctx.AvatarDescriptor);
            });
        }
    }
}