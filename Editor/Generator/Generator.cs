using System.Collections.Generic;
using System.Linq;
using dog.miruku.inventory.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    public abstract class Generator
    {
        private static void CreateMaMergeAnimator(InventoryNode node, Dictionary<AnimatorController, int> controllers)
        {
            var mergeAnimatorObject = new GameObject("MergeAnimator");
            mergeAnimatorObject.transform.SetParent(node.Root.Value.transform, false);

            // Add merge animator
            foreach (var entry in controllers)
            {
                var mergeAnimator = mergeAnimatorObject.AddComponent<ModularAvatarMergeAnimator>();
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
            // Resolve root nodes
            var rootNodes = InventoryNode.ResolveRootNodes(avatar).ToArray();

            MenuGenerator.Generate(avatar, rootNodes);
            foreach (var node in rootNodes)
            {
                // Generate animation
                var controllers = AnimationGenerator.GenerateControllers(node);
                CreateMaMergeAnimator(node, controllers);

                // Generate parameters
                CreateMaParameters(node);
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