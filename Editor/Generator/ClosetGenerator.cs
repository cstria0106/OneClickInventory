using System.Collections;
using System.Collections.Generic;
using dog.miruku.ndcloset.runtime;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

public class ClosetGenerator
{
    private readonly Closet _closet;
    private readonly string _id;
    private readonly Dictionary<ClosetItem, int> _itemIndexes = new Dictionary<ClosetItem, int>();
    private VRCAvatarDescriptor _avatar;

    public ClosetGenerator(Closet closet, VRCAvatarDescriptor avatar)
    {
        _avatar = avatar;
        _closet = closet;
        _id = $"{closet.ClosetName}_{GUID.Generate()}";
    }



}
