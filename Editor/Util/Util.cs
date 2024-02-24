using UnityEngine;

namespace dog.miruku.ndcloset
{
    public class Util
    {
        public static T GetOrAddComponent<T>(GameObject o) where T : Component
        {
            return o.GetComponent<T>() ?? o.AddComponent<T>();
        }
    }
}