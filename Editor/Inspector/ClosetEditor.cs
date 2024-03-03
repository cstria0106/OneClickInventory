using dog.miruku.inventory.runtime;
using UnityEditor;

[CustomEditor(typeof(Closet))]
public class ClosetEditor : Editor
{
    private void OnEnable()
    {
        Closet closet = (Closet)target;
        closet.gameObject.AddComponent<Inventory>();
        DestroyImmediate(closet);
    }
}
