using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dog.miruku.inventory;
using nadena.dev.modular_avatar.core;
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

            menu.Control = new VRCExpressionsMenu.Control()
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
            var menuItemsToInstall = node.MenuItemsToInstall.ToArray();

            // If the node is a leaf node
            if (!node.HasChildren && menuItemsToInstall.Length <= 0)
                return node.IsItem
                    ? AddToggleMenu(node.Value.Name, node.Value.Icon, node.ParameterName, node.ParameterValue, parent)
                    : AddSubmenu(node.Value.Name, node.Value.Icon, parent);

            // Create a submenu
            var submenu = AddSubmenu(node.Value.Name, node.Value.Icon, parent);

            // Add a toggle menu if the node is an item
            if (node.IsItem)
                AddToggleMenu(L.Get("enable"), node.Value.Icon, node.ParameterName, node.ParameterValue,
                    submenu.transform);

            // Recursively create children
            foreach (var child in node.Children)
            {
                CreateMaMenu(child, submenu.transform);
            }

            // Add menus installed by InventoryMenuInstaller
            foreach (var menuItem in menuItemsToInstall) menuItem.transform.SetParent(submenu.transform);

            return submenu;
        }

        private static Dictionary<string, ParameterConfig> GetMaParameterConfigs(InventoryNode node,
            Dictionary<string, ParameterConfig> configs = null)
        {
            configs ??= new Dictionary<string, ParameterConfig>();

            if (node.IsItem)
            {
                configs[node.ParameterName] = new ParameterConfig
                {
                    nameOrPrefix = node.ParameterName,
                    syncType = ParameterSyncType.Int,
                    defaultValue = 0,
                    saved = false,
                    localOnly = true
                };

                configs[AnimationGenerator.GetSyncedParameterName(node.ParameterName)] = new ParameterConfig
                {
                    nameOrPrefix = AnimationGenerator.GetSyncedParameterName(node.ParameterName),
                    syncType = ParameterSyncType.Bool,
                    defaultValue = 0,
                    saved = false,
                    localOnly = true
                };

                foreach (var (name, defaultValue) in AnimationGenerator.Encode(node.ParameterName, node.ParameterBits,
                             node.ParameterDefault))
                {
                    var saved = node.ParentIsUnique ? node.Parent.Value.Saved : node.Value.Saved;

                    configs[name] =
                        new ParameterConfig
                        {
                            nameOrPrefix = name,
                            syncType = ParameterSyncType.Bool,
                            defaultValue = defaultValue,
                            saved = saved,
                            localOnly = false
                        };
                }
            }

            return node.Children.Aggregate(configs, (current, child) => GetMaParameterConfigs(child, current));
        }

        private static void CreateMaParameters(InventoryNode node)
        {
            var parametersObject = new GameObject("Parameters");
            parametersObject.transform.SetParent(node.Root.Value.transform, false);
            var parameters = parametersObject.AddComponent<ModularAvatarParameters>();
            var configs = GetMaParameterConfigs(node);
            parameters.parameters = configs.Values.ToList();
        }

        private static void CreateMaMergeAnimator(Dictionary<AnimatorController, int> controllers, GameObject parent)
        {
            // Add merge animator
            foreach (var entry in controllers)
            {
                var mergeAnimator = parent.AddComponent<ModularAvatarMergeAnimator>();
                mergeAnimator.animator = entry.Key;
                mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                mergeAnimator.deleteAttachedAnimator = true;
                mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
                mergeAnimator.matchAvatarWriteDefaults = false;
                mergeAnimator.layerPriority = entry.Value;
            }
        }

        public static void Generate(InventoryNode node, Dictionary<AnimatorController, int> controllers,
            Transform menuParent)
        {
            var mergeAnimatorParent = new GameObject("MergeAnimator");
            mergeAnimatorParent.transform.SetParent(node.Root.Value.transform, false);
            CreateMaMergeAnimator(controllers, mergeAnimatorParent);
            CreateMaParameters(node);
            if (node.IsRoot && node.Value.InstallMenuInRoot)
            {
                var menuItem = CreateMaMenu(node, node.Avatar.transform);
                var installer = menuItem.gameObject.AddComponent<ModularAvatarMenuInstaller>();
                installer.menuToAppend = node.Avatar.expressionsMenu;
            }
            else
            {
                CreateMaMenu(node, menuParent);
            }
        }
    }
}