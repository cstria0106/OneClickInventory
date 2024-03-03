using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    public class Util
    {
        public static T GetOrAddComponent<T>(GameObject o) where T : Component
        {
            return o.GetComponent<T>() ?? o.AddComponent<T>();
        }

        public static VRCAvatarDescriptor FindAvatar(Transform t)
        {
            if (t == null) return null;
            if (t.TryGetComponent<VRCAvatarDescriptor>(out var descriptor)) return descriptor;
            return FindAvatar(t.parent);
        }

        public static bool IsInAvatar(VRCAvatarDescriptor avatar, Transform t) => FindAvatar(t) == avatar;

        public static string EscapeStateMachineName(string name) => name.Replace('.', '_').Replace(' ', '_').Replace('/', '_').Replace('(', '_').Replace(')', '_');
    }
}