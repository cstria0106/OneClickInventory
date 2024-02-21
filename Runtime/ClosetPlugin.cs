
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using dog.miruku.ndcloset.runtime;

[assembly: ExportsPlugin(typeof(ClosetPlugin))]
public class ClosetPlugin : Plugin<ClosetPlugin>
{
    protected override void Configure()
    {
        InPhase(BuildPhase.Generating)
            .BeforePlugin("nadena.dev.modular-avatar")
            .Run("Generating closet", ctx =>
        {
            ClosetUtil.ClearGeneratedAssets();

            Texture2D menuIcon = null;

            string menuName = "Closet";
            if (ctx.AvatarRootObject.TryGetComponent<ClosetAvatarConfig>(out var config))
            {
                menuName = config.CustomMenuName;
                menuIcon = config.CustomIcon;
            }

            var menuInstallerObject = new GameObject(menuName);
            menuInstallerObject.transform.SetParent(ctx.AvatarRootTransform);
            var menuInstaller = menuInstallerObject.AddComponent<ModularAvatarMenuInstaller>();
            menuInstaller.menuToAppend = ctx.AvatarDescriptor.expressionsMenu;

            var menuItem = menuInstallerObject.AddComponent<ModularAvatarMenuItem>();
            menuItem.Control = new VRCExpressionsMenu.Control()
            {
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                name = menuName,
                icon = menuIcon
            };
            menuItem.MenuSource = SubmenuSource.Children;

            var closets = ctx.AvatarRootTransform.GetComponentsInChildren<Closet>();
            foreach (var closet in closets)
            {
                closet.Setup(ctx.AvatarDescriptor, menuInstallerObject.transform);

                foreach (var item in closet.Items)
                {
                    Object.Destroy(item);
                }
                Object.Destroy(closet);
            }
        });
    }
}