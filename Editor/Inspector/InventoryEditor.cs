using System;
using UnityEditor;
using UnityEngine;
using dog.miruku.inventory.runtime;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditorInternal;
using VRC.SDKBase;

namespace dog.miruku.inventory
{
    [CustomEditor(typeof(Inventory))]
    [CanEditMultipleObjects]
    public class InventoryEditor : Editor
    {
        private Inventory Inventory { get; set; }
        private SerializedProperty InstallMenuInRoot { get; set; }
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
        private SerializedProperty Save { get; set; }
        private SerializedProperty IntegrateMenuInstaller { get; set; }

        private ReorderableList _blendShapesToChangeList;
        private ReorderableList _materialsToReplaceList;
        private ReorderableList _parameterDriverBindingsList;

        private static bool _showItems;
        private static readonly AvatarHierarchyFolding AvatarHierarchyFolding = new();

        static InventoryEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIconOnWindowItem;
        }

        private void OnEnable()
        {
            Inventory = (Inventory) target;
            Name = serializedObject.FindProperty("_name");

            IsUnique = serializedObject.FindProperty("_isUnique");
            InstallMenuInRoot = serializedObject.FindProperty("_installMenuInRoot");

            Default = serializedObject.FindProperty("_default");
            AdditionalObjects = serializedObject.FindProperty("_additionalObjects");
            AdditionalAnimations = serializedObject.FindProperty("_additionalAnimations");
            ObjectsToDisable = serializedObject.FindProperty("_objectsToDisable");
            IsNotItem = serializedObject.FindProperty("_isNotItem");
            LayerPriority = serializedObject.FindProperty("_layerPriority");
            Save = serializedObject.FindProperty("_saved");
            IntegrateMenuInstaller = serializedObject.FindProperty("_integrateMenuInstaller");

            BlendShapesToChange = serializedObject.FindProperty("_blendShapesToChange");
            _blendShapesToChangeList =
                new ReorderableList(serializedObject, BlendShapesToChange, true, true, true, true)
                {
                    drawHeaderCallback = rect =>
                    {
                        rect.x += 15;
                        rect.width -= 15;
                        const float gab = 10f;
                        const float valueWidth = 50f - gab / 2;
                        var otherWidth = (rect.width - valueWidth - gab) / 2 - gab / 2;
                        var rendererX = rect.x;
                        var nameX = rendererX + otherWidth + gab;
                        var valueX = nameX + otherWidth + gab;

                        EditorGUI.LabelField(new Rect(rect.x, rect.y, otherWidth, EditorGUIUtility.singleLineHeight),
                            "Renderer");
                        EditorGUI.LabelField(new Rect(nameX, rect.y, otherWidth, EditorGUIUtility.singleLineHeight),
                            "Name");
                        EditorGUI.LabelField(new Rect(valueX, rect.y, valueWidth, EditorGUIUtility.singleLineHeight),
                            "Value");
                    },
                    drawElementCallback = (rect, index, _, _) =>
                    {
                        var element = _blendShapesToChangeList.serializedProperty.GetArrayElementAtIndex(index);
                        var renderer =
                            element.FindPropertyRelative("renderer").objectReferenceValue as SkinnedMeshRenderer;
                        var blendShapes = renderer != null
                            ? Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                                .Select(i => renderer.sharedMesh.GetBlendShapeName(i)).ToArray()
                            : Array.Empty<string>();
                        const float gab = 10f;
                        const float valueWidth = 50f - gab / 2;
                        var otherWidth = (rect.width - valueWidth - gab) / 2 - gab / 2;
                        var rendererX = rect.x;
                        var nameX = rendererX + otherWidth + gab;
                        var valueX = nameX + otherWidth + gab;

                        EditorGUI.PropertyField(
                            new Rect(rendererX, rect.y, otherWidth, EditorGUIUtility.singleLineHeight),
                            element.FindPropertyRelative("renderer"), GUIContent.none);
                        using var blendShapeProperty = element.FindPropertyRelative("name");
                        var blendShapeIndex =
                            EditorGUI.Popup(new Rect(nameX, rect.y, otherWidth, EditorGUIUtility.singleLineHeight),
                                blendShapes.ToList().IndexOf(blendShapeProperty.stringValue), blendShapes);
                        blendShapeProperty.stringValue = blendShapeIndex >= 0 ? blendShapes[blendShapeIndex] : "";
                        EditorGUI.PropertyField(new Rect(valueX, rect.y, valueWidth, EditorGUIUtility.singleLineHeight),
                            element.FindPropertyRelative("value"), GUIContent.none);
                    }
                };

            MaterialsToReplace = serializedObject.FindProperty("_materialsToReplace");
            _materialsToReplaceList = new ReorderableList(serializedObject, MaterialsToReplace, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    rect.x += 15;
                    rect.width -= 15;
                    const float gab = 10f;
                    var width = (rect.width - gab * 2) / 3;
                    var rendererX = rect.x;
                    var fromX = rendererX + width + gab;
                    var toX = fromX + width + gab;


                    EditorGUI.LabelField(new Rect(rendererX, rect.y, width, EditorGUIUtility.singleLineHeight),
                        "Renderer");
                    EditorGUI.LabelField(new Rect(fromX, rect.y, width, EditorGUIUtility.singleLineHeight), "From");
                    EditorGUI.LabelField(new Rect(toX, rect.y, width, EditorGUIUtility.singleLineHeight), "To");
                },
                drawElementCallback = (rect, index, _, _) =>
                {
                    var element = _materialsToReplaceList.serializedProperty.GetArrayElementAtIndex(index);
                    const float gab = 10f;
                    var width = (rect.width - gab * 2) / 3;
                    var rendererX = rect.x;
                    var fromX = rendererX + width + gab;
                    var toX = fromX + width + gab;

                    var renderer = element.FindPropertyRelative("renderer").objectReferenceValue as Renderer;
                    var materials = renderer != null ? renderer.sharedMaterials.Distinct().ToArray() : new Material[0];

                    EditorGUI.PropertyField(new Rect(rendererX, rect.y, width, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("renderer"), GUIContent.none);
                    var from = element.FindPropertyRelative("from");
                    var fromIndex = EditorGUI.Popup(new Rect(fromX, rect.y, width, EditorGUIUtility.singleLineHeight),
                        materials.ToList().IndexOf(from.objectReferenceValue as Material),
                        materials.Select(e => e != null ? e.name : "").ToArray());
                    from.objectReferenceValue = fromIndex >= 0 ? materials[fromIndex] : null;
                    EditorGUI.PropertyField(new Rect(toX, rect.y, width, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("to"), GUIContent.none);
                },
            };

            // WIP
            // TODO: implement parameter driver editor
            ParameterDriverBindings = serializedObject.FindProperty("_parameterDriverBindings");
            _parameterDriverBindingsList =
                new ReorderableList(serializedObject, ParameterDriverBindings, true, true, true, true)
                {
                    drawHeaderCallback = _ => { },
                    drawElementCallback = (rect, index, _, _) =>
                    {
                        var element = ParameterDriverBindings.GetArrayElementAtIndex(index);
                        var originalLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 50;

                        var typeProperty = element.FindPropertyRelative("parameter.type");
                        var typeRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(typeRect, typeProperty, new GUIContent("Type"));

                        var parameterProperty = element.FindPropertyRelative("parameter.name");
                        var parameterRect = new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2,
                            EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(parameterRect, parameterProperty, new GUIContent("Parameter"));

                        EditorGUIUtility.labelWidth = originalLabelWidth;
                    },
                    elementHeightCallback = index =>
                    {
                        var element = _parameterDriverBindingsList.serializedProperty.GetArrayElementAtIndex(index);
                        var type = element.FindPropertyRelative("parameter.type").enumValueIndex;
                        switch ((VRC_AvatarParameterDriver.ChangeType) type)
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

                if (inventory.Default)
                    GUI.DrawTexture(labelRect, CachedResource.Load<Texture2D>("InventoryActive.png"));
                else GUI.DrawTexture(labelRect, CachedResource.Load<Texture2D>("Inventory.png"));
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(
                "이 컴포넌트가 있는 오브젝트는 인벤토리 혹은 아이템으로 설정됩니다. 속성 이름에 마우스를 대서 설명을 볼 수 있습니다.",
                new GUIStyle(EditorStyles.label) {wordWrap = true}
            );
            EditorGUILayout.Space();

            var avatar = Util.FindAvatar(Inventory.transform.parent);
            if (avatar == null)
            {
                EditorGUILayout.HelpBox(L.Get("noAvatar"), MessageType.Warning);
                return;
            }

            var node = InventoryNode.FindNodeByValue(avatar, Inventory);
            node.Root.Validate();

            serializedObject.Update();

            EditorGUILayout.LabelField(L.Get("menu"), InventoryEditorUtil.HeaderStyle);
            if (node.IsRoot)
            {
                EditorGUILayout.PropertyField(InstallMenuInRoot,
                    new GUIContent(L.Get("installMenuInRoot"), "체크하면 메뉴가 서브 메뉴가 아닌 최상위에 설치됩니다."));
            }

            EditorGUILayout.PropertyField(Name, new GUIContent(L.Get("name")));
            AssetPreview.GetAssetPreview(Inventory.Icon);
            EditorGUILayout.LabelField(L.Get("customIcon"));
            GUILayout.BeginHorizontal();
            Inventory.Icon = (Texture2D) EditorGUILayout.ObjectField(Inventory.Icon, typeof(Texture2D), false,
                GUILayout.Width(100), GUILayout.Height(100));
            if (GUILayout.Button(L.Get("generateIcon")))
            {
                var icon = IconUtil.Generate(node);
                Inventory.Icon = icon;
            }

            GUILayout.EndHorizontal();


            if (node.IsInventory)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(L.Get("inventory"), InventoryEditorUtil.HeaderStyle);
                EditorGUI.BeginDisabledGroup(true);
                _showItems = EditorGUILayout.Foldout(_showItems, L.Get("items"));
                if (_showItems)
                {
                    foreach (var child in node.ChildItems)
                    {
                        EditorGUILayout.ObjectField(child.Value, typeof(Inventory), false);
                    }
                }

                if (node.DefaultChild != null)
                {
                    EditorGUILayout.ObjectField(L.Get("defaultItem"), node.DefaultChild.Value,
                        typeof(Inventory), false);
                }

                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(IsUnique,
                    new GUIContent(L.Get("isUnique"),
                        "체크하면 하위 아이템을 하나만 선택할 수 있게 됩니다.\n즉 옷이나 헤어와 같이 같이 하나를 입으면 다른 것은 비활성화 돼야 하는 경우에 사용합니다.\n액세서리와 같이 여러 개를 동시에 활성화 할 수 있는 경우에는 체크하지 않을 수 있습니다.\n만약 체크되어있고 기본 아이템이 설정되지 않은 경우에는 아무것도 활성화 되지 않은 상태가 기본이 됩니다."));

                if (node.Value.IsUnique)
                {
                    EditorGUILayout.PropertyField(LayerPriority,
                        new GUIContent(L.Get("layerPriority"),
                            "FX 레이어 상의 레이어 우선순위를 결정합니다. Modular Avatar와 연동됩니다."));
                    EditorGUILayout.PropertyField(Save,
                        new GUIContent(L.Get("saved"),
                            "체크하면 이 인벤토리의 선택된 아이템이 월드 간에 저장됩니다."));
                }
            }

            if (node.CanBeItem)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(L.Get("item"), InventoryEditorUtil.HeaderStyle);
                EditorGUILayout.PropertyField(IsNotItem,
                    new GUIContent(L.Get("isNotItem"), "체크하면 이것은 아이템이 아니게 됩니다. 주로 카테고리를 나타내기 위해 사용합니다."));
            }

            if (node.IsItem)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(L.Get("inventory"), node.Parent.Value, typeof(Inventory), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(Default,
                    new GUIContent(
                        node.ParentIsUnique ? L.Get("defaultUnique") : L.Get("default"),
                        "아바타 초기 상태일 때 (Reset Avatar 시) 기본적으로 활성화되게 됩니다. 또한 상위 인벤토리가 '하나의 아이템만 활성화' 상태인 경우, 기본 아이템으로 설정합니다."));
                EditorGUILayout.PropertyField(AdditionalObjects,
                    new GUIContent(L.Get("additionalObject"),
                        "해당 아이템 활성화 시 함께 활성화 할 오브젝트를 지정합니다. 속옷 등을 옷과 같이 입는 경우에 유용합니다."));
                EditorGUILayout.PropertyField(ObjectsToDisable,
                    new GUIContent(L.Get("disableObject"),
                        "해당 아이템 활성화 시 비활성화 할 오브젝트를 지정합니다. 후드 티를 뚫는 헤어 파츠를 비활성화 하는 등에 유용합니다."));
                BlendShapesToChange.isExpanded =
                    EditorGUILayout.Foldout(BlendShapesToChange.isExpanded, L.Get("setBlendShape"));
                if (BlendShapesToChange.isExpanded)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(
                        "Blend Shape를 변경합니다. Shrink 블렌드 셰이프나 Foot Heel(까치발) 블렌드 셰이프과 함께 사용하는 옷에 유용합니다.",
                        new GUIStyle(EditorStyles.label) {wordWrap = true, fontSize = 11});
                    EditorGUILayout.Space();
                    _blendShapesToChangeList.DoLayoutList();
                }

                MaterialsToReplace.isExpanded = EditorGUILayout.Foldout(MaterialsToReplace.isExpanded,
                    L.Get("replaceMaterial"));

                if (MaterialsToReplace.isExpanded)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("매터리얼을 변경합니다. 옷의 색상을 선택하게 하고 싶거나, 액세서리의 색상을 옷과 매칭되게 하는 등에 유용합니다.",
                        new GUIStyle(EditorStyles.label) {wordWrap = true, fontSize = 11});
                    EditorGUILayout.Space();
                    _materialsToReplaceList.DoLayoutList();
                }

                // TODO: implement parameter driver editor
#if false
                    ParameterDriverBindings.isExpanded = EditorGUILayout.Foldout(ParameterDriverBindings.isExpanded,
                        L.Get("parameterDrivers"));
                    if (ParameterDriverBindings.isExpanded) _parameterDriverBindingsList.DoLayoutList();
#endif

                EditorGUILayout.PropertyField(ParameterDriverBindings,
                    new GUIContent(L.Get("parameterDrivers") + " (WIP)",
                        "옷이 활성화 될 때 아바타의 파라미터를 변경하게 합니다. 기믹 등과 연동하기 좋습니다."));

                EditorGUILayout.PropertyField(AdditionalAnimations,
                    new GUIContent(L.Get("additionalAnimations"), "활성화 시 추가 애니메이션을 재생하도록 합니다."));

                if (!node.ParentIsUnique)
                {
                    EditorGUILayout.PropertyField(LayerPriority,
                        new GUIContent(L.Get("layerPriority"),
                            "FX 레이어 상의 레이어 우선순위를 결정합니다. Modular Avatar와 연동됩니다."));

                    EditorGUILayout.PropertyField(Save,
                        new GUIContent(L.Get("saved"),
                            "체크하면 이 아이템이 월드 간에 저장됩니다."));
                }

                if(Inventory.TryGetComponent<ModularAvatarMenuInstaller>(out _))
                {
                    EditorGUILayout.PropertyField(IntegrateMenuInstaller,
                        new GUIContent(L.Get("integrateMenuInstaller"),
                            "체크하면 MA Menu Installer로 지정한 메뉴에 아이템 메뉴가 설치됩니다."));
                }
            }

            InventoryEditorUtil.Footer(node.Avatar, AvatarHierarchyFolding);
            serializedObject.ApplyModifiedProperties();

            // disable other default
            if (node.ParentIsUnique && node.Value.Default)
            {
                foreach (var e in node.Parent.ChildItems.Where(e => e.Value != Inventory))
                {
                    e.Value.Default = false;
                }
            }
        }
    }
}