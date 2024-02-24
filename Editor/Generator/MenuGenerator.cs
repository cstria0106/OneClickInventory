using System.Collections.Generic;
using dog.miruku.ndcloset;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

public class MenuGenerator
{
    private readonly GeneratorContext _context;
    private GeneratorContext Ctx => _context;

    public MenuGenerator(GeneratorContext context)
    {
        _context = context;
    }

    private void CreateMAMenu(Transform parent)
    {
        var closetMenuObject = new GameObject(Ctx.Closet.ClosetName);
        closetMenuObject.transform.SetParent(parent);

        foreach (var item in Ctx.Closet.Items)
        {
            var itemMenuObject = new GameObject(item.ItemName);
            itemMenuObject.transform.SetParent(closetMenuObject.transform);
            var itemMenu = itemMenuObject.AddComponent<ModularAvatarMenuItem>();
            itemMenu.Control = new VRCExpressionsMenu.Control()
            {
                name = item.ItemName,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                value = Ctx.Closet.IsUnique ? Ctx.ItemIndex(item) : 1,
                icon = item.CustomIcon,
                parameter = new VRCExpressionsMenu.Control.Parameter()
                {
                    name = Ctx.Closet.IsUnique ? Ctx.ClosetId : Ctx.NonUniqueParameterName(item),
                },
            };
        }

        var closetMenu = closetMenuObject.AddComponent<ModularAvatarMenuItem>();
        closetMenu.Control = new VRCExpressionsMenu.Control()
        {
            name = Ctx.Closet.ClosetName,
            icon = Ctx.Closet.CustomIcon,
            type = VRCExpressionsMenu.Control.ControlType.SubMenu,
        };
        closetMenu.MenuSource = SubmenuSource.Children;
    }

    private void CreateMAParameters()
    {
        if (Ctx.Closet.IsUnique)
        {
            var maParameters = Util.GetOrAddComponent<ModularAvatarParameters>(Ctx.Closet.gameObject);
            if (maParameters.parameters == null) maParameters.parameters = new List<ParameterConfig>();
            maParameters.parameters.Add(new ParameterConfig()
            {
                nameOrPrefix = Ctx.ClosetId,
                syncType = ParameterSyncType.Int,
                defaultValue = 0,
                saved = true,
                localOnly = false,
            });
        }
        else
        {
            foreach (var item in Ctx.Closet.Items)
            {
                var maParameters = Util.GetOrAddComponent<ModularAvatarParameters>(item.gameObject);
                if (maParameters.parameters == null) maParameters.parameters = new List<ParameterConfig>();
                maParameters.parameters.Add(new ParameterConfig()
                {
                    nameOrPrefix = Ctx.NonUniqueParameterName(item),
                    syncType = ParameterSyncType.Bool,
                    defaultValue = item.Default ? 1 : 0,
                    saved = true,
                    localOnly = false,
                });
            }
        }
    }

    private void CreateMAMergeAnimator(IEnumerable<AnimatorController> controllers)
    {
        // Add merge animator
        foreach (var controller in controllers)
        {
            var maMergeAnimator = Ctx.Closet.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMergeAnimator.animator = controller;
            maMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            maMergeAnimator.deleteAttachedAnimator = true;
            maMergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            maMergeAnimator.matchAvatarWriteDefaults = true;
        }
    }

    public void Generate(Transform parent, IEnumerable<AnimatorController> controllers)
    {
        CreateMAMergeAnimator(controllers);
        CreateMAParameters();
        CreateMAMenu(parent);
    }
}
