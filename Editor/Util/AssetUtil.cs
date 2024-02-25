using System.IO;
using UnityEditor;

public class AssetUtil
{
    private static readonly string _generatedPathGuid = "6385f8da0e893d142aaaef7ed709f4bd";
    private static readonly string _generatedPathRoot = AssetDatabase.GUIDToAssetPath(_generatedPathGuid);


    private static void AcquireDirectory(string path)
    {

        var directoryPath = Path.GetDirectoryName(path);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }
    }

    public static string GetPath(string key)
    {
        var assetPath = $"{_generatedPathRoot}/{key}";
        AcquireDirectory(assetPath);
        return assetPath;
    }

    public static string GetPersistentPath(string key)
    {
        var assetPath = $"Assets/Closet/{key}";
        AcquireDirectory(assetPath);
        return assetPath;
    }

    public static void ClearGeneratedAssets()
    {
        if (Directory.Exists(_generatedPathRoot))
        {
            Directory.Delete(_generatedPathRoot, true);
        }
        Directory.CreateDirectory(_generatedPathRoot);
        File.Create(_generatedPathRoot + "/dummy");
        AssetDatabase.Refresh();
    }

}
