using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CachedResource
{
    private static readonly string _resourcesPathGuid = "7d8b3a244de977846b2055b8aed045a1";
    private static readonly string _resourcesPathRoot = AssetDatabase.GUIDToAssetPath(_resourcesPathGuid);

    private static Dictionary<string, Object> cache = new Dictionary<string, Object>();

    public static T Load<T>(string path) where T : Object
    {
        if (cache.ContainsKey(path)) return cache[path] as T;
        var asset = AssetDatabase.LoadAssetAtPath<T>(_resourcesPathRoot + "/" + path);
        cache[path] = asset;
        return asset;
    }
}
