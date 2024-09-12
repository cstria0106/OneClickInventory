using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace dog.miruku.inventory
{
    public class Localization
    {
        private static readonly string _localizationPathGuid = "d9780e86d63caeb4b9287b9a4df854d9";
        private static readonly string _localizationPathRoot = AssetDatabase.GUIDToAssetPath(_localizationPathGuid);

        private static readonly string _fallbackLanguage = "en";

        private static string _language = null;
        public static string Language
        {
            get => _language ?? EditorPrefs.GetString("one-click-inventory.language", _fallbackLanguage);
            set
            {
                _language = value;
                EditorPrefs.SetString("one-click-inventory.language", _language);
                Preload(_language);
            }
        }

        private static readonly Dictionary<string, Dictionary<string, string>> _cache = new Dictionary<string, Dictionary<string, string>>();

        public static List<(string, string)> Languages
        {
            get => new List<(string, string)>() {
                ("en", "English"),
                ("ko", "한국어"),
                ("zho", "繁體中文"),
            };
        }

        private static string Get(string language, string key)
        {
            Preload(language);
            if (_cache.ContainsKey(language) && _cache[language].ContainsKey(key))
            {
                return _cache[language][key];
            }

            Preload(_fallbackLanguage);
            return _cache[_fallbackLanguage].ContainsKey(key) ? _cache[_fallbackLanguage][key] : key;
        }

        public static string Get(string key)
        {
            return Get(Language, key);
        }

        private static void Preload(string language)
        {
            if (_cache.ContainsKey(language)) return;
            var filename = _localizationPathRoot + "/" + language + ".json";
            var text = File.ReadAllText(filename);
            _cache[language] = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
        }
    }
}
