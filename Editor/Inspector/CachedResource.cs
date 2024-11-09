using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dog.miruku.inventory
{
    public abstract class CachedResource
    {
        private const string RESOURCES_PATH_GUID = "7d8b3a244de977846b2055b8aed045a1";
        private static readonly string ResourcesPathRoot = AssetDatabase.GUIDToAssetPath(RESOURCES_PATH_GUID);

        private static readonly Dictionary<string, Object> Cache = new();

        public static T Load<T>(string path) where T : Object
        {
            if (Cache.TryGetValue(path, out var value)) return value as T;
            var asset = AssetDatabase.LoadAssetAtPath<T>(ResourcesPathRoot + "/" + path);
            Cache[path] = asset;
            return asset;
        }
    }
}