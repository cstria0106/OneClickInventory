
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using dog.miruku.ndcloset.runtime;
using System.Collections;
using System.Collections.Generic;

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
                AssetUtil.ClearGeneratedAssets();

                // Get avatar closet config
                string menuName = "Closet";
                Texture2D menuIcon = null;
                if (ctx.AvatarRootObject.TryGetComponent<ClosetAvatarConfig>(out var config))
                {
                    menuName = config.CustomMenuName;
                    menuIcon = config.CustomIcon;
                }

                // Create menu installer
                var menuInstallerObject = new GameObject(menuName);
                menuInstallerObject.transform.SetParent(ctx.AvatarRootTransform);
                var menuInstaller = menuInstallerObject.AddComponent<ModularAvatarMenuInstaller>();
                menuInstaller.menuToAppend = ctx.AvatarDescriptor.expressionsMenu;

                // Create root menu
                var menuItem = menuInstallerObject.AddComponent<ModularAvatarMenuItem>();
                menuItem.Control = new VRCExpressionsMenu.Control()
                {
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    name = menuName,
                    icon = menuIcon
                };
                menuItem.MenuSource = SubmenuSource.Children;

                // Generate closets
                var closets = ctx.AvatarRootTransform.GetComponentsInChildren<Closet>();
                foreach (var closet in closets)
                {
                    var generatorContext = new GeneratorContext(closet, ctx.AvatarDescriptor);

                    var animationGenerator = new AnimationGenerator(generatorContext);
                    var controllers = animationGenerator.GenerateControllers();

                    var menuGenerator = new MenuGenerator(generatorContext);
                    menuGenerator.Generate(menuItem.transform, controllers);
                }
            });
        }
    }
}