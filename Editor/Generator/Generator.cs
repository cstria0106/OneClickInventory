using System.Linq;
using dog.miruku.inventory.runtime;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace dog.miruku.inventory
{
    public abstract class Generator
    {
        public static void Generate(VRCAvatarDescriptor avatar)
        {
            // get avatar inventory config
            var menuName = "Inventory";
            Texture2D menuIcon = null;
            if (avatar.TryGetComponent<InventoryConfig>(out var config))
            {
                menuName = config.CustomMenuName;
                menuIcon = config.CustomIcon;
            }

            var rootNodes = InventoryNode.GetRootNodes(avatar);
            var menuObject = new GameObject(menuName);
            menuObject.transform.SetParent(avatar.transform);

            // Create root menu when there are non-root menu items
            if (rootNodes.Any(e => !e.Value.InstallMenuInRoot))
            {
                // create menu installer
                var menuInstaller = menuObject.AddComponent<ModularAvatarMenuInstaller>();
                menuInstaller.menuToAppend = avatar.expressionsMenu;

                // create root menu
                var menuItem = menuObject.AddComponent<ModularAvatarMenuItem>();
                menuItem.Control = new VRCExpressionsMenu.Control()
                {
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    name = menuName,
                    icon = menuIcon
                };
                menuItem.MenuSource = SubmenuSource.Children;
            }


            // generate inventories
            foreach (var node in rootNodes)
            {
                var controllers = AnimationGenerator.GenerateControllers(node);
                MenuGenerator.Generate(node, controllers, menuObject.transform);
            }
        }
    }
}