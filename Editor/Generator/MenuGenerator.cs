using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using dog.miruku.ndcloset;
using dog.miruku.ndcloset.runtime;
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

    private static void CreateMAMenu(ClosetNode node, Transform parent)
    {
        if (node.IsCloset)
        {
            var submenu = AddSubmenu(node.Value.Name, node.Value.Icon, parent);
            if (node.Parent != null) AddToggleMenu(Localization.Get("enable"), node.Value.Icon, node.ParameterName, node.ParameterIntValue, submenu.transform);
            foreach (var child in node.Children)
            {
                CreateMAMenu(child, submenu.transform);
            }
        }
        else if (node.IsItem)
        {
            AddToggleMenu(node.Value.Name, node.Value.Icon, node.ParameterName, node.ParameterIntValue, parent);
        }
    }

    private static Dictionary<string, ParameterConfig> GetMAParameterConfigs(ClosetNode node, Dictionary<string, ParameterConfig> configs = null)
    {
        if (configs == null) configs = new Dictionary<string, ParameterConfig>();

        if (node.ParameterName != null && !configs.ContainsKey(node.ParameterName))
        {
            configs[node.ParameterName] =
                new ParameterConfig()
                {
                    nameOrPrefix = node.ParameterName,
                    syncType = node.ParameterIsIndex ? ParameterSyncType.Int : ParameterSyncType.Bool,
                    defaultValue = node.ParameterDefaultValue,
                    saved = true,
                    localOnly = false
                };
        }

        foreach (var child in node.Children) configs = GetMAParameterConfigs(child, configs);

        return configs;
    }

    private static void CreateMAParameters(ClosetNode node)
    {
        var parameters = node.Value.gameObject.AddComponent<ModularAvatarParameters>();
        var configs = GetMAParameterConfigs(node);
        parameters.parameters = configs.Values.ToList();
    }

    private static void CreateMAMergeAnimator(ClosetNode node, IEnumerable<AnimatorController> controllers)
    {
        // Add merge animator
        foreach (var controller in controllers)
        {
            var mergeAnimator = node.Value.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            mergeAnimator.animator = controller;
            mergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            mergeAnimator.deleteAttachedAnimator = true;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            mergeAnimator.matchAvatarWriteDefaults = true;
        }
    }

    public static void Generate(ClosetNode node, IEnumerable<AnimatorController> controllers, Transform menuParent)
    {
        CreateMAMergeAnimator(node, controllers);
        CreateMAParameters(node);
        CreateMAMenu(node, menuParent);
    }
}
