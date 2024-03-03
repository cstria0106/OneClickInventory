using System.Collections.Generic;
using System.Linq;
using dog.miruku.inventory;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

public class MenuGenerator
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

    private static ModularAvatarMenuItem AddToggleMenu(string name, Texture2D icon, string parameter, int value, Transform parent)
    {
        var menuObject = new GameObject(name);
        menuObject.transform.SetParent(parent);
        var menu = menuObject.AddComponent<ModularAvatarMenuItem>();

        menu.Control = new VRCExpressionsMenu.Control()
        {
            name = name,
            icon = icon,
            type = VRCExpressionsMenu.Control.ControlType.Toggle,
            parameter = new VRCExpressionsMenu.Control.Parameter()
            {
                name = parameter
            },
            value = value,
        };
        return menu;
    }

    private static void CreateMAMenu(InventoryNode node, Transform parent)
    {
        if (node.IsInventory)
        {
            var submenu = AddSubmenu(node.Value.Name, node.Value.Icon, parent);
            if (node.IsItem) AddToggleMenu(Localization.Get("enable"), node.Value.Icon, node.ParameterName, node.ParameterValue, submenu.transform);
            foreach (var child in node.Children)
            {
                CreateMAMenu(child, submenu.transform);
            }
        }
        else if (node.IsItem)
        {
            AddToggleMenu(node.Value.Name, node.Value.Icon, node.ParameterName, node.ParameterValue, parent);
        }
    }

    private static Dictionary<string, ParameterConfig> GetMAParameterConfigs(InventoryNode node, Dictionary<string, ParameterConfig> configs = null)
    {
        if (configs == null) configs = new Dictionary<string, ParameterConfig>();

        if (node.IsItem)
        {
            configs[node.ParameterName] = new ParameterConfig()
            {
                nameOrPrefix = node.ParameterName,
                syncType = ParameterSyncType.Int,
                defaultValue = node.ParameterDefault,
                saved = true,
                localOnly = true,
            };

            foreach (var (name, defaultValue) in AnimationGenerator.Encode(node.ParameterName, node.ParameterBits, node.ParameterDefault))
            {
                configs[name] =
                    new ParameterConfig()
                    {
                        nameOrPrefix = name,
                        syncType = ParameterSyncType.Bool,
                        defaultValue = defaultValue,
                        saved = true,
                        localOnly = false
                    };
            }
        }


        foreach (var child in node.Children) configs = GetMAParameterConfigs(child, configs);

        return configs;
    }

    private static void CreateMAParameters(InventoryNode node)
    {
        var parametersObject = new GameObject($"Parameters");
        parametersObject.transform.SetParent(node.Root.Value.transform, false);
        var parameters = parametersObject.AddComponent<ModularAvatarParameters>();
        var configs = GetMAParameterConfigs(node);
        parameters.parameters = configs.Values.ToList();
    }

    private static void CreateMAMergeAnimator(InventoryNode node, IEnumerable<AnimatorController> controllers)
    {
        // Add merge animator
        foreach (var controller in controllers)
        {
            var mergeAnimator = node.Value.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            mergeAnimator.animator = controller;
            mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            mergeAnimator.deleteAttachedAnimator = true;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            mergeAnimator.matchAvatarWriteDefaults = false;
            mergeAnimator.layerPriority = node.Value.LayerPriority;
        }
    }

    public static void Generate(InventoryNode node, IEnumerable<AnimatorController> controllers, Transform menuParent)
    {
        CreateMAMergeAnimator(node, controllers);
        CreateMAParameters(node);
        CreateMAMenu(node, menuParent);
    }
}
