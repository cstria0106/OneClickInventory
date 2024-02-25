
using dog.miruku.ndcloset;
using dog.miruku.ndcloset.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

public class Generator
{
    public static void Generate(VRCAvatarDescriptor avatar)
    {
        var deprecatedItems = avatar.GetComponentsInChildren<ClosetItem>(true);
        if (deprecatedItems.Length > 0)
        {
            EditorUtility.DisplayDialog("ClosetItem is deprecated", "ClosetItem is deprecated. Please convert into Closet. ClosetItem 컴포넌트는 더이상 사용되지 않습니다. Closet 컴포넌트로 변환해주세요.", "OK");
            throw new System.Exception("ClosetItem is deprecated. Please convert into Closet. ClosetItem 컴포넌트는 더이상 사용되지 않습니다. Closet 컴포넌트로 변환해주세요");
        }

        // get avatar closet config
        string menuName = "Closet";
        Texture2D menuIcon = null;
        if (avatar.TryGetComponent<ClosetAvatarConfig>(out var config))
        {
            menuName = config.CustomMenuName;
            menuIcon = config.CustomIcon;
        }

        // create menu installer
        var menuObject = new GameObject(menuName);
        menuObject.transform.SetParent(avatar.transform);
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

        // generate closets
        var rootNodes = ClosetNode.GetRootNodes(avatar);
        foreach (var node in rootNodes)
        {
            var controllers = AnimationGenerator.GenerateControllers(node);
            MenuGenerator.Generate(node, controllers, menuObject.transform);
        }
    }
}
