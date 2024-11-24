using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dog.miruku.inventory;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.menu;
using UnityEditor;
using UnityEditor.Animations;
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
            var menuInstaller = node.Value.GetComponent<ModularAvatarMenuInstaller>();
            var useMenuInstaller = menuInstaller != null && node.IsItem && node.Value.IntegrateMenuInstaller;
            if (useMenuInstaller)
            {
                parent = node.Avatar.transform;
            }

            ModularAvatarMenuItem menu;

            var menuItemsToInstall = node.MenuItemsToInstall.ToArray();

            // If it should be generated as a submenu
            if (node.IsInventory || menuItemsToInstall.Any())
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
            // Else don't create any menu
            else return null;

            // Copy menu installer to generated menu object
            if (useMenuInstaller)
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
                CreateMaMenu(child, menu.transform);
            }

            // Add menus installed by InventoryMenuInstaller
            foreach (var menuItem in menuItemsToInstall) menuItem.transform.SetParent(menu.transform);

            return menu;
        }

        public static void Generate(
            InventoryNode rootNode,
            Transform menuParent
        )
        {
            if (!rootNode.IsRoot) throw new System.Exception("Invalid root node");

            // If node is set to be installed in the root menu
            if (rootNode.Value.InstallMenuInRoot)
            {
                var menuItem = CreateMaMenu(rootNode, rootNode.Avatar.transform);
                if (menuItem == null) return;

                var installer = menuItem.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                installer.menuToAppend = rootNode.Avatar.expressionsMenu;
                return;
            }

            CreateMaMenu(rootNode, menuParent);
        }
    }
}