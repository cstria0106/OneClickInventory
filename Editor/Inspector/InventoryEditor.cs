using UnityEditor;
using UnityEngine;
using dog.miruku.inventory.runtime;
using VRC.SDK3.Avatars.Components;
using System.Linq;
using UnityEditorInternal;
using VRC.SDKBase;
using System;

namespace dog.miruku.inventory
{
    [CustomEditor(typeof(Inventory))]
    [CanEditMultipleObjects]
    public class InventoryEditor : Editor
    {
        private Inventory Inventory { get; set; }
        private SerializedProperty Name { get; set; }

        private SerializedProperty IsUnique { get; set; }

        private SerializedProperty Default { get; set; }
        private SerializedProperty AdditionalAnimations { get; set; }
        private SerializedProperty AdditionalObjects { get; set; }
        private SerializedProperty ObjectsToDisable { get; set; }
        private SerializedProperty IsNotItem { get; set; }
        private SerializedProperty BlendShapesToChange { get; set; }
        private SerializedProperty MaterialsToReplace { get; set; }
        private SerializedProperty ParameterDriverBindings { get; set; }
        private SerializedProperty LayerPriority { get; set; }

        private ReorderableList _blendShapesToChangeList;
        private ReorderableList _materialsToReplaceList;
        private ReorderableList _parameterDriverBindingsList;

        private static bool _showItems = false;
        private static AvatarHierarchyFolding _avatarHierarchyFolding = new AvatarHierarchyFolding();

        static InventoryEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIconOnWindowItem;
        }

        private void OnEnable()
        {
            Inventory = (Inventory)target;
            Name = serializedObject.FindProperty("_name");

            IsUnique = serializedObject.FindProperty("_isUnique");

            Default = serializedObject.FindProperty("_default");
            AdditionalObjects = serializedObject.FindProperty("_additionalObjects");
            AdditionalAnimations = serializedObject.FindProperty("_additionalAnimations");
            ObjectsToDisable = serializedObject.FindProperty("_objectsToDisable");
            IsNotItem = serializedObject.FindProperty("_isNotItem");
            LayerPriority = serializedObject.FindProperty("_layerPriority");

            BlendShapesToChange = serializedObject.FindProperty("_blendShapesToChange");
            _blendShapesToChangeList = new ReorderableList(serializedObject, BlendShapesToChange, true, true, true, true)
            {
                drawHeaderCallback = (rect) =>
                {
                    rect.x += 15;
                    rect.width -= 15;
                    var gab = 10f;
                    var valueWidth = 50f - gab / 2;
                    var otherWidth = (rect.width - valueWidth - gab) / 2 - gab / 2;
                    var rendererX = rect.x;
                    var nameX = rendererX + otherWidth + gab;
                    var valueX = nameX + otherWidth + gab;

                    EditorGUI.LabelField(new Rect(rect.x, rect.y, otherWidth, EditorGUIUtility.singleLineHeight), "Renderer");
                    EditorGUI.LabelField(new Rect(nameX, rect.y, otherWidth, EditorGUIUtility.singleLineHeight), "Name");
                    EditorGUI.LabelField(new Rect(valueX, rect.y, valueWidth, EditorGUIUtility.singleLineHeight), "Value");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = _blendShapesToChangeList.serializedProperty.GetArrayElementAtIndex(index);
                        var gab = 10f;
                        var valueWidth = 50f - gab / 2;
                        var otherWidth = (rect.width - valueWidth - gab) / 2 - gab / 2;
                        var rendererX = rect.x;
                        var nameX = rendererX + otherWidth + gab;
                        var valueX = nameX + otherWidth + gab;

                        EditorGUI.PropertyField(new Rect(rendererX, rect.y, otherWidth, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("renderer"), GUIContent.none);
                        EditorGUI.PropertyField(new Rect(nameX, rect.y, otherWidth, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("name"), GUIContent.none);
                        EditorGUI.PropertyField(new Rect(valueX, rect.y, valueWidth, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("value"), GUIContent.none);
                    }
            };

            MaterialsToReplace = serializedObject.FindProperty("_materialsToReplace");
            _materialsToReplaceList = new ReorderableList(serializedObject, MaterialsToReplace, true, true, true, true)
            {
                drawHeaderCallback = (rect) =>
                {
                    rect.x += 15;
                    rect.width -= 15;
                    var gab = 10f;
                    var width = (rect.width - gab * 2) / 3;
                    var rendererX = rect.x;
                    var fromX = rendererX + width + gab;
                    var toX = fromX + width + gab;


                    EditorGUI.LabelField(new Rect(rendererX, rect.y, width, EditorGUIUtility.singleLineHeight), "Renderer");
                    EditorGUI.LabelField(new Rect(fromX, rect.y, width, EditorGUIUtility.singleLineHeight), "From");
                    EditorGUI.LabelField(new Rect(toX, rect.y, width, EditorGUIUtility.singleLineHeight), "To");

                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = _materialsToReplaceList.serializedProperty.GetArrayElementAtIndex(index);
                        var gab = 10f;
                        var width = (rect.width - gab * 2) / 3;
                        var rendererX = rect.x;
                        var fromX = rendererX + width + gab;
                        var toX = fromX + width + gab;

                        EditorGUI.PropertyField(new Rect(rendererX, rect.y, width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("renderer"), GUIContent.none);
                        EditorGUI.PropertyField(new Rect(fromX, rect.y, width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("from"), GUIContent.none);
                        EditorGUI.PropertyField(new Rect(toX, rect.y, width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("to"), GUIContent.none);
                    },
            };

            // WIP
            // TODO: implement parameter driver editor
            ParameterDriverBindings = serializedObject.FindProperty("_parameterDriverBindings");
            _parameterDriverBindingsList = new ReorderableList(serializedObject, ParameterDriverBindings, true, true, true, true)
            {
                drawHeaderCallback = (rect) => { },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = ParameterDriverBindings.GetArrayElementAtIndex(index);
                    var originalLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 50;

                    var typeProperty = element.FindPropertyRelative("parameter.type");
                    var typeRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(typeRect, typeProperty, new GUIContent("Type"));
                    var type = (VRC_AvatarParameterDriver.ChangeType)typeProperty.enumValueIndex;

                    var parameterProperty = element.FindPropertyRelative("parameter.name");
                    var parameterRect = new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(parameterRect, parameterProperty, new GUIContent("Parameter"));

                    EditorGUIUtility.labelWidth = originalLabelWidth;
                },
                elementHeightCallback = (index) =>
                {
                    var element = _parameterDriverBindingsList.serializedProperty.GetArrayElementAtIndex(index);
                    var type = element.FindPropertyRelative("parameter.type").enumValueIndex;
                    switch ((VRC_AvatarParameterDriver.ChangeType)type)
                    {
                        case VRC_AvatarParameterDriver.ChangeType.Set:
                            return EditorGUIUtility.singleLineHeight * 2;
                        case VRC_AvatarParameterDriver.ChangeType.Add:
                            return EditorGUIUtility.singleLineHeight * 3;
                        case VRC_AvatarParameterDriver.ChangeType.Random:
                            return EditorGUIUtility.singleLineHeight * 3;
                        case VRC_AvatarParameterDriver.ChangeType.Copy:
                            return EditorGUIUtility.singleLineHeight * 3;
                    }
                    return EditorGUIUtility.singleLineHeight;
                }
            };
        }

        private static void DrawIconOnWindowItem(int instanceID, Rect rect)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent(out Inventory inventory))
            {
                var size = rect.height;
                var labelRect = new Rect(rect.xMax - size, rect.yMin, size, size);

                if (inventory.Default) GUI.DrawTexture(labelRect, CachedResource.Load<Texture2D>("InventoryActive.png"));
                else GUI.DrawTexture(labelRect, CachedResource.Load<Texture2D>("Inventory.png"));
            }
        }

        public override void OnInspectorGUI()
        {
            var avatar = Util.FindAvatar(Inventory.transform.parent);
            if (avatar == null)
            {
                EditorGUILayout.HelpBox(Localization.Get("noAvatar"), MessageType.Warning);
                return;
            }

            var node = InventoryNode.FindNodeByValue(avatar, Inventory);
            node.Root.Validate();

            serializedObject.Update();

            EditorGUILayout.LabelField(Localization.Get("menu"), InventoryEditorUtil.HeaderStyle);
            EditorGUILayout.PropertyField(Name, new GUIContent(Localization.Get("name")));
            var texture = AssetPreview.GetAssetPreview(Inventory.Icon);
            EditorGUILayout.LabelField(Localization.Get("customIcon"));
            GUILayout.BeginHorizontal();
            Inventory.Icon = (Texture2D)EditorGUILayout.ObjectField(Inventory.Icon, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100)); ;
            if (GUILayout.Button(Localization.Get("generateIcon")))
            {
                var icon = IconUtil.Generate(node);
                Inventory.Icon = icon;
            }
            GUILayout.EndHorizontal();

            if (node.IsInventory)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Localization.Get("inventory"), InventoryEditorUtil.HeaderStyle);
                EditorGUI.BeginDisabledGroup(true);
                _showItems = EditorGUILayout.Foldout(_showItems, Localization.Get("items"));
                if (_showItems)
                {
                    foreach (var child in node.Children)
                    {
                        EditorGUILayout.ObjectField(child.Value, typeof(Inventory), false);
                    }
                }
                if (node.DefaultChild != null)
                {
                    EditorGUILayout.ObjectField(Localization.Get("defaultItem"), node.DefaultChild.Value, typeof(Inventory), false);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(IsUnique, new GUIContent(Localization.Get("isUnique")));
            }

            if (node.CanBeItem)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Localization.Get("item"), InventoryEditorUtil.HeaderStyle);
                EditorGUILayout.PropertyField(IsNotItem, new GUIContent(Localization.Get("isNotItem")));
            }

            if (node.IsItem)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(Localization.Get("inventory"), node.Parent.Value, typeof(Inventory), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(Default, new GUIContent(node.ParentIsUnique ? Localization.Get("defaultUnique") : Localization.Get("default")));
                EditorGUILayout.PropertyField(AdditionalObjects, new GUIContent(Localization.Get("additionalObject")));
                EditorGUILayout.PropertyField(ObjectsToDisable, new GUIContent(Localization.Get("disableObject")));
                BlendShapesToChange.isExpanded = EditorGUILayout.Foldout(BlendShapesToChange.isExpanded, Localization.Get("setBlendShape"));
                if (BlendShapesToChange.isExpanded) _blendShapesToChangeList.DoLayoutList();
                MaterialsToReplace.isExpanded = EditorGUILayout.Foldout(MaterialsToReplace.isExpanded, Localization.Get("replaceMaterial"));
                if (MaterialsToReplace.isExpanded) _materialsToReplaceList.DoLayoutList();

                // TODO: implement parameter driver editor
                if (false)
                {
                    ParameterDriverBindings.isExpanded = EditorGUILayout.Foldout(ParameterDriverBindings.isExpanded, Localization.Get("parameterDrivers"));
                    if (ParameterDriverBindings.isExpanded) _parameterDriverBindingsList.DoLayoutList();
                }
                EditorGUILayout.PropertyField(ParameterDriverBindings, new GUIContent(Localization.Get("parameterDrivers") + " (WIP)"));

                EditorGUILayout.PropertyField(AdditionalAnimations, new GUIContent(Localization.Get("additionalAnimations")));

                EditorGUILayout.PropertyField(LayerPriority, new GUIContent(Localization.Get("layerPriority")));
            }

            InventoryEditorUtil.Footer(node.Avatar, _avatarHierarchyFolding);
            serializedObject.ApplyModifiedProperties();

            // disable other default
            if (node.ParentIsUnique && node.Value.Default)
            {
                foreach (var e in node.Parent.Children.Where(e => e.Value != Inventory))
                {
                    e.Value.Default = false;
                }
            }
        }
    }
}