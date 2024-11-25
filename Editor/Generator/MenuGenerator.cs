using System.Linq;
using dog.miruku.inventory.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.menu;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace dog.miruku.inventory
{
    public abstract class MenuGenerator
    {
        private static ModularAvatarMenuItem AddSubmenu(string name, Texture2D icon, Transform parent)
        {
            var menuObject = new GameObject(name);
            menuObject.transform.SetParent(parent);
            var menu = menuObject.AddComponent<ModularAvatarMenuItem>();

            menu.Control = new VRCExpressionsMenu.Control
            {
                name = name,
                icon = icon,
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                value = 0,
            };
            menu.MenuSource = SubmenuSource.Children;
            return menu;
        }

        private static ModularAvatarMenuItem AddToggleMenu(
            string name, Texture2D icon, string parameter, int value, Transform parent
        )
        {
            var menuObject = new GameObject(name);
            menuObject.transform.SetParent(parent);
            var menu = menuObject.AddComponent<ModularAvatarMenuItem>();

            menu.Control = new VRCExpressionsMenu.Control
            {
                name = name,
                icon = icon,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter
                {
                    name = parameter
                },
                value = value
            };
            return menu;
        }

        private static ModularAvatarMenuItem CreateMaMenu(InventoryNode node, Transform parent)
        {
            // If MAMenuInstaller is found, set the parent to the avatar
            var menuInstaller = node.IntegratedMenuInstaller;
            if (menuInstaller) parent = node.Avatar.transform;

            ModularAvatarMenuItem menu = null;

            var menuItemsToInstall = node.MenuItemsToInstall.ToArray();

            // If it should be generated as a submenu
            if (node.ShouldBeSubmenu)
            {
                menu = AddSubmenu(node.Value.Name, node.Value.Icon, parent);
                if (node.IsItem)
                    AddToggleMenu(L.Get("enable"), node.Value.Icon, node.ParameterName, node.ParameterValue,
                        menu.transform);
            }
            // Else if it should be generated as a toggle menu
            else if (node.IsItem)
            {
                menu = AddToggleMenu(node.Value.Name, node.Value.Icon, node.ParameterName, node.ParameterValue, parent);
            }

            // Copy menu installer to generated menu object
            if (menu && menuInstaller)
            {
                var newMenuInstaller = menu.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                newMenuInstaller.menuToAppend = menuInstaller.menuToAppend;
                newMenuInstaller.installTargetMenu = menuInstaller.installTargetMenu;

                // Replace all reference to the original installer with the new one
                foreach (var component in node.Avatar.GetComponentsInChildren<MenuSourceComponent>())
                {
                    if (component.GetType().Name == "ModularAvatarMenuInstallTarget")
                    {
                        // Set serialized field "installer" to the new installer
                        component.GetType().GetField("installer")?.SetValue(component, newMenuInstaller);
                        EditorUtility.SetDirty(component);
                    }
                }

                // Remove original installer
                Object.DestroyImmediate(menuInstaller);
            }

            // Recursively create children
            foreach (var child in node.Children)
            {
                CreateMaMenu(child, menu?.transform ?? parent);
            }

            // Add menus installed by InventoryMenuInstaller
            if (menu)
                foreach (var menuItem in menuItemsToInstall)
                    menuItem.transform.SetParent(menu.transform);

            return menu;
        }

        private static void Generate(
            InventoryNode rootNode,
            Transform menuParent
        )
        {
            if (!rootNode.IsRoot) throw new System.Exception("Invalid root node");

            if (!rootNode.Value.InstallMenuInRoot)
            {
                CreateMaMenu(rootNode, menuParent);
                return;
            }

            // If node is set to be installed in the root menu
            var menuItem = CreateMaMenu(rootNode, rootNode.Avatar.transform);
            if (menuItem == null) return;

            var installer = menuItem.gameObject.AddComponent<ModularAvatarMenuInstaller>();
            installer.menuToAppend = rootNode.Avatar.expressionsMenu;
        }

        public static void Generate(VRCAvatarDescriptor avatar, InventoryNode[] rootNodes)
        {
            // Get avatar inventory config
            var menuName = L.Get("inventory");
            Texture2D menuIcon = null;
            if (avatar.TryGetComponent<InventoryConfig>(out var config))
            {
                menuName = config.CustomMenuName;
                menuIcon = config.CustomIcon;
            }

            // Create inventory object
            var inventoryObject = new GameObject(menuName);
            inventoryObject.transform.SetParent(avatar.transform);

            // Create root inventory menu when there are any non-root menu items
            if (rootNodes.Any(e => !e.Value.InstallMenuInRoot && e.ShouldBeSubmenu))
            {
                // Create menu installer
                var menuInstaller = inventoryObject.AddComponent<ModularAvatarMenuInstaller>();
                menuInstaller.menuToAppend = avatar.expressionsMenu;

                // Create root menu
                var menuItem = inventoryObject.AddComponent<ModularAvatarMenuItem>();
                menuItem.Control = new VRCExpressionsMenu.Control
                {
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    name = menuName,
                    icon = menuIcon
                };
                menuItem.MenuSource = SubmenuSource.Children;
            }

            foreach (var node in rootNodes)
            {
                // Generate menu
                Generate(node, inventoryObject.transform);
            }
        }
    }
}