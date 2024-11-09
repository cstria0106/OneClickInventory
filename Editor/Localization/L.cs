using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace dog.miruku.inventory
{
    public abstract class L
    {
        private const string LOCALIZATION_PATH_GUID = "d9780e86d63caeb4b9287b9a4df854d9";
        private static readonly string LocalizationPathRoot = AssetDatabase.GUIDToAssetPath(LOCALIZATION_PATH_GUID);

        private const string FALLBACK_LANGUAGE = "en";

        private static string _language;

        public static string Language
        {
            get => _language ?? EditorPrefs.GetString("one-click-inventory.language", FALLBACK_LANGUAGE);
            set
            {
                _language = value;
                EditorPrefs.SetString("one-click-inventory.language", _language);
                Preload(_language);
            }
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Cache = new();

        public static List<(string, string)> Languages =>
            new()
            {
                ("en", "English"),
                ("ko", "한국어"),
                ("zh-Hant", "繁體中文")
            };

        private static string Get(string language, string key)
        {
            Preload(language);
            if (Cache.ContainsKey(language) && Cache[language].ContainsKey(key))
            {
                return Cache[language][key];
            }

            Preload(FALLBACK_LANGUAGE);
            return Cache[FALLBACK_LANGUAGE].ContainsKey(key) ? Cache[FALLBACK_LANGUAGE][key] : key;
        }

        public static string Get(string key)
        {
            return Get(Language, key);
        }

        private static void Preload(string language)
        {
            if (Cache.ContainsKey(language)) return;
            var filename = LocalizationPathRoot + "/" + language + ".json";
            var text = File.ReadAllText(filename);
            Cache[language] = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
        }
    }
}