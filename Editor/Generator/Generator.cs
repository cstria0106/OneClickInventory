using System.Collections.Generic;
using System.Linq;
using dog.miruku.inventory.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace dog.miruku.inventory
{
    public abstract class Generator
    {
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

        public static void Generate(VRCAvatarDescriptor avatar)
        {
            // Get avatar inventory config
            var menuName = "Inventory";
            Texture2D menuIcon = null;
            if (avatar.TryGetComponent<InventoryConfig>(out var config))
            {
                menuName = config.CustomMenuName;
                menuIcon = config.CustomIcon;
            }

            // Resolve root nodes
            var rootNodes = InventoryNode.ResolveRootNodes(avatar);

            // Create inventory object
            var inventoryObject = new GameObject(menuName);
            inventoryObject.transform.SetParent(avatar.transform);

            // Create root inventory menu when there are any non-root menu items
            if (rootNodes.Any(e => !e.Value.InstallMenuInRoot))
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
                // Generate animation
                var controllers = AnimationGenerator.GenerateControllers(node);
                var mergeAnimator = new GameObject("MergeAnimator");
                mergeAnimator.transform.SetParent(node.Root.Value.transform, false);
                CreateMaMergeAnimator(controllers, mergeAnimator);

                // Generate parameters
                CreateMaParameters(node);

                // Generate menu
                MenuGenerator.Generate(node, inventoryObject.transform);
            }

            // Remove Inventory components
            var types = new[] {typeof(Inventory), typeof(InventoryMenuInstaller), typeof(InventoryConfig)};
            foreach (var type in types)
            {
                foreach (var component in avatar.GetComponentsInChildren(type, true))
                {
                    Object.DestroyImmediate(component);
                }
            }
        }
    }
}