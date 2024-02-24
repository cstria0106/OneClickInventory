using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dog.miruku.ndcloset.runtime;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace dog.miruku.ndcloset
{
    public class ClosetUtil
    {
        private static readonly string _generatedPathGuid = "6385f8da0e893d142aaaef7ed709f4bd";
        private static readonly string _generatedPathRoot = AssetDatabase.GUIDToAssetPath(_generatedPathGuid);

        private static string GetRelativePath(Transform o, Transform ancestor, String path)
        {
            if (o == null) throw new Exception("Invalid ancestor");
            if (o == ancestor) return path;

            return GetRelativePath(o.parent, ancestor, $"{o.name}/{path}");
        }

        private static string GetRelativePath(Transform o, Transform ancestor)
        {
            return GetRelativePath(o.parent, ancestor, o.name);
        }

        private static void AcquireDirectory(string path)
        {

            var directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static string GetAssetPath(string key)
        {
            var assetPath = $"{_generatedPathRoot}/{key}";
            AcquireDirectory(assetPath);
            return assetPath;
        }

        public static string GetPersistentAssetPath(string key)
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
        }

        public static void CopyAnimationClip(AnimationClip from, AnimationClip to)
        {
            var curves = AnimationUtility.GetCurveBindings(from).ToList();
            curves.AddRange(AnimationUtility.GetObjectReferenceCurveBindings(from));
            foreach (var curve in curves)
            {
                to.SetCurve(curve.path, curve.type, curve.propertyName, AnimationUtility.GetEditorCurve(from, curve));
            }
        }

        public static AnimationClip GenerateAnimationClip(
            string key,
            VRCAvatarDescriptor avatar,
            IEnumerable<GameObject> enabledObjects,
            IEnumerable<GameObject> disabledObjects,
            IEnumerable<AnimationClip> additionalAnimations
        )
        {
            var clip = new AnimationClip();
            var enabledKeys = new Keyframe[1] { new Keyframe(0.0f, 1f) };
            var disabledKeys = new Keyframe[1] { new Keyframe(0.0f, 0f) };
            foreach (var o in enabledObjects)
            {
                clip.SetCurve(GetRelativePath(o.transform, avatar.transform), typeof(GameObject), "m_IsActive", new AnimationCurve(enabledKeys));
            }
            foreach (var o in disabledObjects)
            {
                clip.SetCurve(GetRelativePath(o.transform, avatar.transform), typeof(GameObject), "m_IsActive", new AnimationCurve(disabledKeys));
            }
            foreach (var c in additionalAnimations)
            {
                CopyAnimationClip(c, clip);
            }

            var path = GetAssetPath($"Animations/{key}.anim");
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] rpixels = result.GetPixels(0);
            float incX = 1.0f / targetWidth;
            float incY = 1.0f / targetHeight;
            for (int px = 0; px < rpixels.Length; px++)
            {
                rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
            }
            result.SetPixels(rpixels, 0);
            result.Apply();
            return result;
        }

        public static Texture2D GenerateIcon(ClosetItem item)
        {
            var cloned = new GameObject();
            foreach (var o in item.GameObjects)
            {
                var clone = GameObject.Instantiate(o);
                clone.transform.SetParent(cloned.transform);
                clone.SetActive(true);
            }

            // Setup camera
            cloned.transform.position = Vector3.zero;
            var cameraObject = new GameObject();
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Nothing;
            camera.nearClipPlane = 0.00001f;

            // Calculate bound
            var boundList = cloned.GetComponentsInChildren<Renderer>().Select<Renderer, Bounds?>(e =>
            {
                if (e.TryGetComponent(out SkinnedMeshRenderer renderer))
                {
                    if (renderer.sharedMesh == null) return null;
                    return new Bounds(renderer.bounds.center, renderer.sharedMesh.bounds.size);
                }
                return e.bounds;
            }).OfType<Bounds>().ToArray();

            var bounds = boundList.Length > 0 ? boundList[0] : new Bounds();
            foreach (var b in boundList.Skip(1))
            {
                bounds.Encapsulate(b);
            }

            // Calculate positions
            cameraObject.transform.eulerAngles = new Vector3(0, -180, 0);
            var maxExtent = bounds.extents.magnitude;
            var minDistance = (maxExtent) / Mathf.Sin(Mathf.Deg2Rad * camera.fieldOfView / 2.0f);
            var center = bounds.center;

            cloned.transform.position = new Vector3(5000, 5000, 5000);
            camera.transform.position = center + new Vector3(5000, 5000, 5000) + Vector3.forward * minDistance;

            var captureWidth = 2048;
            var captureHeight = 2048;

            // Capture
            var rt = new RenderTexture(captureWidth, captureHeight, 0);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = camera.targetTexture;
            var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.ARGB32, false);
            image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            image.alphaIsTransparency = true;
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            GameObject.DestroyImmediate(rt);
            GameObject.DestroyImmediate(camera.gameObject);
            GameObject.DestroyImmediate(cloned.gameObject);

            // Clip alpha
            int minX = captureWidth, maxX = 0, minY = captureHeight, maxY = 0;
            for (int x = 0; x < captureWidth; x++)
            {
                for (int y = 0; y < captureHeight; y++)
                {
                    var pixel = image.GetPixel(x, y);
                    if (pixel.a != 0)
                    {
                        if (minX > x) minX = x;
                        if (maxX < x) maxX = x;
                        if (minY > y) minY = y;
                        if (maxY < y) maxY = y;
                    }
                }
            }

            int centerX = (minX + maxX) / 2, centerY = (minY + maxY) / 2;
            var size = Mathf.Max(maxX - minX, maxY - minY);
            if (size < 0)
            {
                size = 1;
            }
            var pixels = image.GetPixels(centerX - size / 2, centerY - size / 2, size, size);
            var clippedIcon = new Texture2D(size, size, TextureFormat.ARGB32, false);
            clippedIcon.SetPixels(pixels);
            clippedIcon.Apply();


            // Resize and save
            var resizedIcon = ResizeTexture(clippedIcon, 256, 256);
            var bytes = resizedIcon.EncodeToPNG();


            var path = GetPersistentAssetPath("Icons/" + GUID.Generate() + ".png");
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static T GetOrAddComponent<T>(GameObject o) where T : Component
        {
            return o.GetComponent<T>() ?? o.AddComponent<T>();
        }

        private class ClosetSetup
        {
            Closet _closet;
            string _id;
            private Dictionary<ClosetItem, int> _itemIndexes;


            private string ItemId(ClosetItem item)
            {
                return $"{_id}_{_itemIndexes[item]}";
            }

            private (Dictionary<ClosetItem, AnimationClip>, Dictionary<ClosetItem, AnimationClip>) GenerateNonUniqueClips(VRCAvatarDescriptor avatar)
            {
                var enabledClips = new Dictionary<ClosetItem, AnimationClip>();
                var disabledClips = new Dictionary<ClosetItem, AnimationClip>();
                var items = _closet.Items;

                foreach (var item in items)
                {
                    var itemId = ItemId(item);
                    enabledClips.Add(item, GenerateAnimationClip($"{_id}/{itemId}_enabled", avatar, new GameObject[] { item.gameObject }, Enumerable.Empty<GameObject>(), item.EnabledAdditionalAnimations));
                    disabledClips.Add(item, GenerateAnimationClip($"{_id}/{itemId}_disabled", avatar, Enumerable.Empty<GameObject>(), new GameObject[] { item.gameObject }, Enumerable.Empty<AnimationClip>()));
                }

                return (enabledClips, disabledClips);
            }


            private (Dictionary<ClosetItem, AnimationClip>, AnimationClip) GenerateUniqueClips(VRCAvatarDescriptor avatar)
            {
                var clips = new Dictionary<ClosetItem, AnimationClip>();
                var items = _closet.Items;

                Dictionary<ClosetItem, Tuple<List<GameObject>, List<GameObject>>> groups = new Dictionary<ClosetItem, Tuple<List<GameObject>, List<GameObject>>>();
                HashSet<GameObject> allObjects = new HashSet<GameObject>();
                foreach (var item in items)
                {
                    groups[item] = (new List<GameObject>(), new List<GameObject>()).ToTuple();
                    foreach (var o in item.GameObjects)
                    {
                        allObjects.Add(o);
                    }
                }

                foreach (var item in items)
                {
                    var gameObjects = item.GameObjects;
                    groups[item].Item1.AddRange(gameObjects);
                    // Add only not enabled object into disabled object
                    groups[item].Item2.AddRange(allObjects.Where(o => !gameObjects.Contains(o)));
                }

                foreach (var item in groups.Keys)
                {
                    var (enabled, disabled) = groups[item];
                    var clip = GenerateAnimationClip($"{_id}/{ItemId(item)}", avatar, enabled, disabled, item.EnabledAdditionalAnimations);
                    clips[item] = clip;
                }

                var disableAllClip = GenerateAnimationClip(
                    $"{_id}/disable_all",
                    avatar,
                    Enumerable.Empty<GameObject>(),
                    allObjects,
                    Enumerable.Empty<AnimationClip>()
                );

                return (clips, disableAllClip);
            }

            private static void SetupTransition(AnimatorStateTransition transition, bool hasExitTime = false)
            {
                transition.hasExitTime = hasExitTime;
                transition.exitTime = 0;
                transition.duration = 0;
                transition.canTransitionToSelf = false;
            }

            private AnimatorController GenerateNonUniqueAnimatorController(ClosetItem item, AnimationClip enabledClip, AnimationClip disabledClip)
            {
                var itemId = ItemId(item);
                var controller = new AnimatorController();
                var layer = new AnimatorControllerLayer
                {
                    stateMachine = new AnimatorStateMachine(),
                    name = item.ItemName
                };

                controller.AddLayer(layer);
                controller.AddParameter(itemId, AnimatorControllerParameterType.Bool);

                layer.stateMachine.defaultState = layer.stateMachine.AddState("Idle", new Vector3(0, 0));
                layer.stateMachine.entryPosition = new Vector3(-200, 0);
                layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

                var enabledPosition = new Vector3(0, 50);
                var disabledPosition = new Vector3(0, 100);

                var enabledState = layer.stateMachine.AddState("Enabled", enabledPosition);
                var disabledState = layer.stateMachine.AddState("Disabled", disabledPosition);

                {
                    enabledState.motion = enabledClip;
                    // TODO: implement parameter driver
                    // var driver = enabledState.AddStateMachineBehaviour<VRC_AvatarParameterDriver>();
                    // driver.parameters = item.EnabledParameters.ToList();
                    var transition = layer.stateMachine.AddAnyStateTransition(enabledState);
                    SetupTransition(transition);
                    transition.AddCondition(AnimatorConditionMode.If, 0, itemId);
                }
                {
                    disabledState.motion = disabledClip;
                    var transition = layer.stateMachine.AddAnyStateTransition(disabledState);
                    SetupTransition(transition);
                    transition.AddCondition(AnimatorConditionMode.IfNot, 0, itemId);
                }
                var path = GetAssetPath($"Controllers/{itemId}.controller");
                AssetDatabase.CreateAsset(controller, path);
                return controller;
            }

            private List<AnimatorController> GenerateNonUniqueAnimatorControllers(Dictionary<ClosetItem, AnimationClip> enabledClips, Dictionary<ClosetItem, AnimationClip> disabledClips)
            {
                var controllers = new List<AnimatorController>();
                foreach (var item in enabledClips.Keys)
                {
                    var enabledClip = enabledClips[item];
                    var disabledClip = disabledClips[item];
                    controllers.Add(GenerateNonUniqueAnimatorController(item, enabledClip, disabledClip));
                }
                return controllers;
            }

            private AnimatorController GenerateUniqueAnimatorController(Dictionary<ClosetItem, AnimationClip> clips, AnimationClip disableAllClip)
            {
                var defaultItem = _closet.DefaultItem;
                var controller = new AnimatorController();
                var layer = new AnimatorControllerLayer
                {
                    stateMachine = new AnimatorStateMachine(),
                    name = _closet.ClosetName
                };

                controller.AddLayer(layer);
                controller.AddParameter(_id, AnimatorControllerParameterType.Int);

                layer.stateMachine.entryPosition = new Vector3(-200, 0);
                layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

                // Default or disable animation
                var defaultState = layer.stateMachine.AddState("Default", new Vector3(0, 0));
                layer.stateMachine.defaultState = defaultState;

                defaultState.motion = defaultItem != null ? clips[defaultItem] : disableAllClip;
                {
                    var transition = layer.stateMachine.AddEntryTransition(defaultState);
                    transition.AddCondition(AnimatorConditionMode.Less, _itemIndexes.Values.Min(), _id);
                }
                {
                    var transition = layer.stateMachine.AddEntryTransition(defaultState);
                    transition.AddCondition(AnimatorConditionMode.Greater, _itemIndexes.Values.Max(), _id);
                }
                {
                    var transition = defaultState.AddExitTransition();
                    SetupTransition(transition);
                    transition.AddCondition(AnimatorConditionMode.Greater, _itemIndexes.Values.Min() - 1, _id);
                    transition.AddCondition(AnimatorConditionMode.Less, _itemIndexes.Values.Max() + 1, _id);
                }

                var position = new Vector3(0, 50);
                var yGab = new Vector3(0, 50);

                // Enabled animations
                foreach (var item in clips.Keys)
                {
                    var enabledClip = clips[item];
                    var state = layer.stateMachine.AddState(ItemId(item), position);
                    state.motion = enabledClip;

                    // setup entry
                    var entryTransition = layer.stateMachine.AddEntryTransition(state);
                    entryTransition.AddCondition(AnimatorConditionMode.Equals, _itemIndexes[item], _id);
                    // setup exit
                    var exitTransition = state.AddExitTransition();
                    SetupTransition(exitTransition);
                    exitTransition.AddCondition(AnimatorConditionMode.NotEqual, _itemIndexes[item], _id);

                    position += yGab;
                }

                layer.stateMachine.exitPosition = new Vector3(400, 0);

                var path = GetAssetPath($"Controllers/{_id}.controller");
                AssetDatabase.CreateAsset(controller, path);
                return controller;
            }

            public void CreateMAMenu(Transform parent)
            {
                var closetMenuObject = new GameObject(_closet.ClosetName);
                closetMenuObject.transform.SetParent(parent);

                foreach (var item in _closet.Items)
                {
                    var itemMenuObject = new GameObject(item.ItemName);
                    itemMenuObject.transform.SetParent(closetMenuObject.transform);
                    var itemMenu = itemMenuObject.AddComponent<ModularAvatarMenuItem>();
                    itemMenu.Control = new VRCExpressionsMenu.Control()
                    {
                        name = item.ItemName,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        value = _closet.IsUnique ? _itemIndexes[item] : 1,
                        icon = item.CustomIcon,
                        parameter = new VRCExpressionsMenu.Control.Parameter()
                        {
                            name = _closet.IsUnique ? _id : ItemId(item),
                        },
                    };
                }

                var closetMenu = closetMenuObject.AddComponent<ModularAvatarMenuItem>();
                closetMenu.Control = new VRCExpressionsMenu.Control()
                {
                    name = _closet.ClosetName,
                    icon = _closet.CustomIcon,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                };
                closetMenu.MenuSource = SubmenuSource.Children;
            }

            private void CreateMAParameters()
            {
                if (_closet.IsUnique)
                {
                    var maParameters = ClosetUtil.GetOrAddComponent<ModularAvatarParameters>(_closet.gameObject);
                    if (maParameters.parameters == null) maParameters.parameters = new List<ParameterConfig>();
                    maParameters.parameters.Add(new ParameterConfig()
                    {
                        nameOrPrefix = _id,
                        syncType = ParameterSyncType.Int,
                        defaultValue = 0,
                        saved = true,
                        localOnly = false,
                    });
                }
                else
                {
                    foreach (var item in _closet.Items)
                    {
                        var maParameters = ClosetUtil.GetOrAddComponent<ModularAvatarParameters>(item.gameObject);
                        if (maParameters.parameters == null) maParameters.parameters = new List<ParameterConfig>();
                        maParameters.parameters.Add(new ParameterConfig()
                        {
                            nameOrPrefix = ItemId(item),
                            syncType = ParameterSyncType.Bool,
                            defaultValue = item.Default ? 1 : 0,
                            saved = true,
                            localOnly = false,
                        });
                    }
                }
            }

            private void CreateMAMergeAnimator(IEnumerable<AnimatorController> controllers)
            {
                // Add merge animator
                foreach (var controller in controllers)
                {
                    var maMergeAnimator = _closet.gameObject.AddComponent<ModularAvatarMergeAnimator>();
                    maMergeAnimator.animator = controller;
                    maMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                    maMergeAnimator.deleteAttachedAnimator = true;
                    maMergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
                    maMergeAnimator.matchAvatarWriteDefaults = true;
                }
            }

            private IEnumerable<AnimatorController> GenerateAnimation(VRCAvatarDescriptor avatar)
            {
                List<AnimatorController> controllers;
                if (_closet.IsUnique)
                {
                    var (clips, disableAllClip) = GenerateUniqueClips(avatar);
                    controllers = new List<AnimatorController>() { GenerateUniqueAnimatorController(clips, disableAllClip) };
                }
                else
                {
                    var (enabledClips, disabledClips) = GenerateNonUniqueClips(avatar);
                    controllers = GenerateNonUniqueAnimatorControllers(enabledClips, disabledClips);
                }
                return controllers;
            }

            private void SetupItemIndexes()
            {
                _itemIndexes = new Dictionary<ClosetItem, int>();
                int index = 1;
                foreach (var item in _closet.Items)
                {
                    if (_closet.IsUnique && item.Default)
                    {
                        _itemIndexes[item] = 0;
                    }
                    else
                    {
                        _itemIndexes[item] = index++;
                    }
                }
            }

            public void Setup(Closet closet, VRCAvatarDescriptor avatar, Transform menuParent)
            {
                _closet = closet;
                _id = $"{_closet.ClosetName}_{GUID.Generate()}";
                SetupItemIndexes();
                var controllers = GenerateAnimation(avatar);
                CreateMAMergeAnimator(controllers);
                CreateMAParameters();
                CreateMAMenu(menuParent);
            }
        }

        public static void Setup(Closet closet, VRCAvatarDescriptor avatar, Transform menuParent)
        {
            var setup = new ClosetSetup();
            setup.Setup(closet, avatar, menuParent);
        }
    }
}