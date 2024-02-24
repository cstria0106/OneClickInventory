using System.Linq;
using UnityEditor;
using dog.miruku.ndcloset.runtime;

namespace dog.miruku.ndcloset
{
    public class ClosetInspector
    {
        private static int _selectedLanguage;
        public static void Default()
        {
            var newSelectedLanguage = EditorGUILayout.Popup(Localization.Get("language"), _selectedLanguage, Localization.Languages.Select(e => e.Item2).ToArray());
            if (newSelectedLanguage != _selectedLanguage)
            {
                _selectedLanguage = newSelectedLanguage;
                Localization.Language = Localization.Languages[_selectedLanguage].Item1;
                Localization.Get(Localization.Languages[_selectedLanguage].Item1);
            }
        }
    }
}