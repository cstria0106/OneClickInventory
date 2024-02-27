using System.Collections;
using System.Collections.Generic;
using dog.miruku.inventory.runtime;
using UnityEditor;
using UnityEngine;

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
