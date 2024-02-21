
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace dog.miruku.ndcloset.runtime
{
    [AddComponentMenu("Non-Destructive Closet/Closet")]
    public class Closet : MonoBehaviour
    {
        [SerializeField] private string _closetName;
        [SerializeField] private bool _isUnique;


        // Must be set in setup method
        private string _id;
        private Dictionary<ClosetItem, int> _itemIndexes;


        public ClosetItem DefaultItem { get => Items.FirstOrDefault(item => item.Default); }

        public bool IsUnique { get => _isUnique; }

        public ClosetItem[] Items { get => GetComponentsInChildren<ClosetItem>(includeInactive: true); }

        [SerializeField] private Texture2D _customIcon;
        public Texture2D CustomIcon { get => _customIcon; }


        private void Reset()
        {
            _closetName = gameObject.name;
        }

        public void Validate()
        {
            if (_isUnique)
            {
                var items = Items;
                foreach (var item in items.Where(item => item.Default).Skip(1))
                {
                    item.Default = false;
                }
            }
        }

        private string ItemId(ClosetItem item)
        {
            return $"{_id}_{_itemIndexes[item]}";
        }

        private (Dictionary<ClosetItem, AnimationClip>, Dictionary<ClosetItem, AnimationClip>) GenerateNonUniqueClips(VRCAvatarDescriptor avatar)
        {
            var enabledClips = new Dictionary<ClosetItem, AnimationClip>();
            var disabledClips = new Dictionary<ClosetItem, AnimationClip>();
            var items = Items;

            foreach (var item in items)
            {
                var itemId = ItemId(item);
                enabledClips.Add(item, ClosetUtil.GenerateAnimationClip($"{_id}/{itemId}_enabled", avatar, new GameObject[] { item.gameObject }, Enumerable.Empty<GameObject>()));
                disabledClips.Add(item, ClosetUtil.GenerateAnimationClip($"{_id}/{itemId}_disabled", avatar, Enumerable.Empty<GameObject>(), new GameObject[] { item.gameObject }));
            }

            return (enabledClips, disabledClips);
        }

        private (Dictionary<ClosetItem, AnimationClip>, AnimationClip) GenerateUniqueClips(VRCAvatarDescriptor avatar)
        {
            var clips = new Dictionary<ClosetItem, AnimationClip>();
            var items = Items;

            Dictionary<string, Tuple<ClosetItem, List<GameObject>, List<GameObject>>> groups = new();
            HashSet<GameObject> allObjects = new();
            foreach (var item in items)
            {
                groups[item.ItemName] = (item, new List<GameObject>(), new List<GameObject>()).ToTuple();
                foreach (var o in item.GameObjects)
                {
                    allObjects.Add(o);
                }
            }

            foreach (var item in items)
            {
                var gameObjects = item.GameObjects;
                groups[item.ItemName].Item2.AddRange(gameObjects);
                // Add only not enabled object into disabled object
                groups[item.ItemName].Item3.AddRange(allObjects.Where(o => !gameObjects.Contains(o)));
            }

            foreach (var (_, (item, enabled, disabled)) in groups)
            {
                var clip = ClosetUtil.GenerateAnimationClip($"{_id}/{ItemId(item)}", avatar, enabled, disabled);
                clips[item] = clip;
            }

            var disableAllClip = ClosetUtil.GenerateAnimationClip(
                $"{_id}/disable_all",
                avatar,
                Enumerable.Empty<GameObject>(),
                allObjects
            );

            return (clips, disableAllClip);
        }

        private static void SetupTransition(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
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
            var path = ClosetUtil.GetAssetPath($"Controllers/{itemId}.controller");
            AssetDatabase.CreateAsset(controller, path);
            return controller;
        }

        private List<AnimatorController> GenerateNonUniqueAnimatorControllers(Dictionary<ClosetItem, AnimationClip> enabledClips, Dictionary<ClosetItem, AnimationClip> disabledClips)
        {
            var controllers = new List<AnimatorController>();
            foreach (var (item, enabledClip) in enabledClips)
            {
                var disabledClip = disabledClips[item];
                controllers.Add(GenerateNonUniqueAnimatorController(item, enabledClip, disabledClip));
            }
            return controllers;
        }

        private AnimatorController GenerateUniqueAnimatorController(Dictionary<ClosetItem, AnimationClip> clips, AnimationClip disableAllClip)
        {
            var defaultItem = DefaultItem;
            var controller = new AnimatorController();
            var layer = new AnimatorControllerLayer
            {
                stateMachine = new AnimatorStateMachine(),
                name = _closetName
            };

            controller.AddLayer(layer);
            controller.AddParameter(_id, AnimatorControllerParameterType.Int);

            layer.stateMachine.defaultState = layer.stateMachine.AddState("Idle", new Vector3(0, 0));
            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);
            var position = new Vector3(0, 100);
            var gab = new Vector3(0, 50);
            foreach (var (item, clip) in clips)
            {
                var state = layer.stateMachine.AddState(ItemId(item), position);
                state.motion = clip;
                var transition = layer.stateMachine.AddAnyStateTransition(state);
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.Equals, _itemIndexes[item], _id);
                position += gab;
            }

            // Default or disable all state
            var defaultState = layer.stateMachine.AddState("Default", new Vector3(0, 50));
            defaultState.motion = defaultItem != null ? clips[defaultItem] : disableAllClip;
            {
                var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.Less, _itemIndexes.Values.Min(), _id);
            }
            {
                var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.Greater, _itemIndexes.Values.Max(), _id);
            }

            var path = ClosetUtil.GetAssetPath($"Controllers/{_id}.controller");
            AssetDatabase.CreateAsset(controller, path);
            return controller;
        }

        public void CreateMAMenu(Transform parent)
        {
            var closetMenuObject = new GameObject(_closetName);
            closetMenuObject.transform.SetParent(parent);

            foreach (var item in Items)
            {
                var itemMenuObject = new GameObject(item.ItemName);
                itemMenuObject.transform.SetParent(closetMenuObject.transform);
                var itemMenu = itemMenuObject.AddComponent<ModularAvatarMenuItem>();
                itemMenu.Control = new VRCExpressionsMenu.Control()
                {
                    name = item.ItemName,
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    value = _isUnique ? _itemIndexes[item] : 1,
                    icon = item.CustomIcon,
                    parameter = new VRCExpressionsMenu.Control.Parameter()
                    {
                        name = _isUnique ? _id : ItemId(item),
                    },
                };
            }

            var closetMenu = closetMenuObject.AddComponent<ModularAvatarMenuItem>();
            closetMenu.Control = new VRCExpressionsMenu.Control()
            {
                name = _closetName,
                icon = _customIcon,
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
            };
            closetMenu.MenuSource = SubmenuSource.Children;
        }

        private void CreateMAParameters()
        {
            if (_isUnique)
            {
                var maParameters = ClosetUtil.GetOrAddComponent<ModularAvatarParameters>(gameObject);
                maParameters.parameters ??= new List<ParameterConfig>();
                maParameters.parameters.Add(new()
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
                foreach (var item in Items)
                {
                    var maParameters = ClosetUtil.GetOrAddComponent<ModularAvatarParameters>(item.gameObject);
                    maParameters.parameters ??= new List<ParameterConfig>();
                    maParameters.parameters.Add(new()
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
                var maMergeAnimator = gameObject.AddComponent<ModularAvatarMergeAnimator>();
                maMergeAnimator.animator = controller;
                maMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                maMergeAnimator.deleteAttachedAnimator = true;
                maMergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
                maMergeAnimator.matchAvatarWriteDefaults = true;
            }
        }

        private IEnumerable<AnimatorController> GenerateAnimation(VRCAvatarDescriptor avatar)
        {
            // Generate clips
            List<AnimatorController> controllers;
            if (_isUnique)
            {
                var (clips, disableAllClip) = GenerateUniqueClips(avatar);
                controllers = new() { GenerateUniqueAnimatorController(clips, disableAllClip) };
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
            _itemIndexes = new();
            int index = 1;
            foreach (var item in Items)
            {
                if (_isUnique && item.Default)
                {
                    _itemIndexes[item] = 0;
                }
                else
                {
                    _itemIndexes[item] = index++;
                }
            }
        }

        public void Setup(VRCAvatarDescriptor avatar, Transform menuParent)
        {
            _id = $"{_closetName}_{GUID.Generate()}";
            SetupItemIndexes();
            var controllers = GenerateAnimation(avatar);
            CreateMAMergeAnimator(controllers);
            CreateMAParameters();
            CreateMAMenu(menuParent);
        }

        private static VRCAvatarDescriptor FindAvatar(Transform t)
        {
            if (t == null) return null;
            if (t.TryGetComponent<VRCAvatarDescriptor>(out var descriptor)) return descriptor;
            return FindAvatar(t.parent);
        }

        public VRCAvatarDescriptor FindAvatar()
        {
            return FindAvatar(transform.parent);
        }
    }
}