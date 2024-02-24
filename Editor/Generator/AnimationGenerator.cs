using System;
using System.Collections.Generic;
using System.Linq;
using dog.miruku.ndcloset.runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.ndcloset
{
    public class AnimationGenerator
    {
        private readonly GeneratorContext _context;
        private GeneratorContext Ctx => _context;
        public AnimationGenerator(GeneratorContext context)
        {
            _context = context;
        }

        public IEnumerable<AnimatorController> GenerateControllers()
        {
            List<AnimatorController> controllers;
            if (Ctx.Closet.IsUnique)
            {
                var (clips, disableAllClip) = GenerateUniqueClips();
                controllers = new List<AnimatorController>() { GenerateUniqueAnimatorController(clips, disableAllClip) };
            }
            else
            {
                var (enabledClips, disabledClips) = GenerateNonUniqueClips();
                controllers = GenerateNonUniqueAnimatorControllers(enabledClips, disabledClips);
            }
            return controllers;
        }

        private static void CopyAnimationClip(AnimationClip from, AnimationClip to)
        {
            var curves = AnimationUtility.GetCurveBindings(from).ToList();
            curves.AddRange(AnimationUtility.GetObjectReferenceCurveBindings(from));
            foreach (var curve in curves)
            {
                to.SetCurve(curve.path, curve.type, curve.propertyName, AnimationUtility.GetEditorCurve(from, curve));
            }
        }

        private static string GetRelativePath(Transform o, Transform ancestor, string path)
        {
            if (o == null) throw new System.Exception("Invalid ancestor");
            if (o == ancestor) return path;

            return GetRelativePath(o.parent, ancestor, $"{o.name}/{path}");
        }

        private static string GetRelativePath(Transform o, Transform ancestor)
        {
            return GetRelativePath(o.parent, ancestor, o.name);
        }

        private static AnimationClip GenerateAnimationClip(
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

            var path = AssetUtil.GetPath($"Animations/{key}.anim");
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }


        private (Dictionary<ClosetItem, AnimationClip>, Dictionary<ClosetItem, AnimationClip>) GenerateNonUniqueClips()
        {
            var enabledClips = new Dictionary<ClosetItem, AnimationClip>();
            var disabledClips = new Dictionary<ClosetItem, AnimationClip>();
            var items = Ctx.Closet.Items;

            foreach (var item in items)
            {
                enabledClips.Add(item, GenerateAnimationClip($"{Ctx.ClosetId}/{Ctx.ItemIndex(item)}_enabled", Ctx.Avatar, new GameObject[] { item.gameObject }, Enumerable.Empty<GameObject>(), item.EnabledAdditionalAnimations));
                disabledClips.Add(item, GenerateAnimationClip($"{Ctx.ClosetId}/{Ctx.ItemIndex(item)}_disabled", Ctx.Avatar, Enumerable.Empty<GameObject>(), new GameObject[] { item.gameObject }, Enumerable.Empty<AnimationClip>()));
            }

            return (enabledClips, disabledClips);
        }


        private (Dictionary<ClosetItem, AnimationClip>, AnimationClip) GenerateUniqueClips()
        {
            var clips = new Dictionary<ClosetItem, AnimationClip>();
            var items = Ctx.Closet.Items;

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
                var clip = GenerateAnimationClip($"{Ctx.ClosetId}/{Ctx.ItemIndex(item)}", Ctx.Avatar, enabled, disabled, item.EnabledAdditionalAnimations);
                clips[item] = clip;
            }

            var disableAllClip = GenerateAnimationClip(
                $"{Ctx.ClosetId}/disable_all",
                Ctx.Avatar,
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
            var parameterName = Ctx.NonUniqueParameterName(item);
            var controller = new AnimatorController();
            var layer = new AnimatorControllerLayer
            {
                stateMachine = new AnimatorStateMachine(),
                name = item.ItemName
            };

            controller.AddLayer(layer);
            controller.AddParameter(parameterName, AnimatorControllerParameterType.Bool);

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
                // var driver = enabledState.AddStateMachineBehaviour<VRCCtx.AvatarParameterDriver>();
                // driver.parameters = item.EnabledParameters.ToList();
                var transition = layer.stateMachine.AddAnyStateTransition(enabledState);
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.If, 0, parameterName);
            }
            {
                disabledState.motion = disabledClip;
                var transition = layer.stateMachine.AddAnyStateTransition(disabledState);
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.IfNot, 0, parameterName);
            }
            var path = AssetUtil.GetPath($"Controllers/{Ctx.ClosetId}/{Ctx.ItemIndex(item)}.controller");
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
            var defaultItem = Ctx.Closet.DefaultItem;
            var controller = new AnimatorController();
            var layer = new AnimatorControllerLayer
            {
                stateMachine = new AnimatorStateMachine(),
                name = Ctx.Closet.ClosetName
            };

            controller.AddLayer(layer);
            controller.AddParameter(Ctx.ClosetId, AnimatorControllerParameterType.Int);

            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

            // Default or disable animation
            var defaultState = layer.stateMachine.AddState("Default", new Vector3(0, 0));
            layer.stateMachine.defaultState = defaultState;

            defaultState.motion = defaultItem != null ? clips[defaultItem] : disableAllClip;
            {
                var transition = layer.stateMachine.AddEntryTransition(defaultState);
                transition.AddCondition(AnimatorConditionMode.Less, Ctx.MinIndex, Ctx.ClosetId);
            }
            {
                var transition = layer.stateMachine.AddEntryTransition(defaultState);
                transition.AddCondition(AnimatorConditionMode.Greater, Ctx.MaxIndex, Ctx.ClosetId);
            }
            {
                var transition = defaultState.AddExitTransition();
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.Greater, Ctx.MinIndex - 1, Ctx.ClosetId);
                transition.AddCondition(AnimatorConditionMode.Less, Ctx.MaxIndex + 1, Ctx.ClosetId);
            }

            var position = new Vector3(0, 50);
            var yGab = new Vector3(0, 50);

            // Enabled animations
            foreach (var item in clips.Keys)
            {
                var enabledClip = clips[item];
                var state = layer.stateMachine.AddState(item.ItemName, position);
                state.motion = enabledClip;

                // setup entry
                var entryTransition = layer.stateMachine.AddEntryTransition(state);
                entryTransition.AddCondition(AnimatorConditionMode.Equals, Ctx.ItemIndex(item), Ctx.ClosetId);
                // setup exit
                var exitTransition = state.AddExitTransition();
                SetupTransition(exitTransition);
                exitTransition.AddCondition(AnimatorConditionMode.NotEqual, Ctx.ItemIndex(item), Ctx.ClosetId);

                position += yGab;
            }

            layer.stateMachine.exitPosition = new Vector3(400, 0);

            var path = AssetUtil.GetPath($"Controllers/{Ctx.ClosetId}.controller");
            AssetDatabase.CreateAsset(controller, path);
            return controller;
        }
    }
}