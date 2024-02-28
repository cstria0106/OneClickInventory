using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using dog.miruku.inventory.runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace dog.miruku.inventory
{
    public class AnimationGenerator
    {
        public static IEnumerable<AnimatorController> GenerateControllers(InventoryNode node)
        {
            List<AnimatorController> controllers;
            if (node.Value.IsUnique)
            {
                var (clips, disableAllClip) = GenerateUniqueClips(node);
                controllers = new List<AnimatorController>() { GenerateUniqueAnimatorController(node, clips, disableAllClip) };
            }
            else
            {
                var (enabledClips, disabledClips) = GenerateNonUniqueClips(node);
                controllers = GenerateNonUniqueAnimatorControllers(enabledClips, disabledClips);
            }

            foreach (var child in node.Children)
            {
                controllers.AddRange(GenerateControllers(child));
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
            if (o == null) throw new Exception("Invalid ancestor");
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
            IEnumerable<GameObject> enabledObjects = null,
            IEnumerable<GameObject> disabledObjects = null,
            IEnumerable<AnimationClip> additionalAnimations = null,
            IEnumerable<SetBlendShapeBinding> setBlendShapes = null,
            IEnumerable<ReplaceMaterialBinding> setMaterials = null
        )
        {
            var clip = new AnimationClip();
            if (enabledObjects != null)
            {
                var enabledKeys = new Keyframe[1] { new Keyframe(0.0f, 1f) };
                foreach (var e in enabledObjects.Where(e => Util.IsInAvatar(avatar, e.transform)))
                {
                    var curve = new AnimationCurve(enabledKeys);
                    clip.SetCurve(GetRelativePath(e.transform, avatar.transform), typeof(GameObject), "m_IsActive", curve);
                }
            }

            if (disabledObjects != null)
            {
                var disabledKeys = new Keyframe[1] { new Keyframe(0.0f, 0f) };
                foreach (var e in disabledObjects.Where(e => Util.IsInAvatar(avatar, e.transform)))
                {
                    var curve = new AnimationCurve(disabledKeys);
                    clip.SetCurve(GetRelativePath(e.transform, avatar.transform), typeof(GameObject), "m_IsActive", curve);
                }
            }

            if (setBlendShapes != null)
                foreach (var e in setBlendShapes.Where(e => Util.IsInAvatar(avatar, e.renderer.transform)))
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(0, e.value);
                    clip.SetCurve(GetRelativePath(e.renderer.transform, avatar.transform), typeof(SkinnedMeshRenderer), $"blendShape.{e.name}", curve);
                }

            if (setMaterials != null)
                foreach (var e in setMaterials.Where(e => Util.IsInAvatar(avatar, e.renderer.transform)))
                {
                    var objectPath = GetRelativePath(e.renderer.transform, avatar.transform);
                    var indexes = e.renderer.sharedMaterials.Where(m => m == e.from).Select((m, i) => i).ToList();
                    var rendererType = e.renderer is SkinnedMeshRenderer ? typeof(SkinnedMeshRenderer)
                                                : e.renderer is MeshRenderer ? typeof(MeshRenderer) : null;
                    foreach (var i in indexes)
                    {
                        var keyframe = new ObjectReferenceKeyframe()
                        {
                            time = 0,
                            value = e.to
                        };
                        var property = $"m_Materials.Array.data[{i}]";
                        var curve = EditorCurveBinding.PPtrCurve(objectPath, rendererType, property);
                        AnimationUtility.SetObjectReferenceCurve(clip, curve, new ObjectReferenceKeyframe[] { keyframe });
                    }
                }

            if (additionalAnimations != null)
                foreach (var e in additionalAnimations)
                {
                    CopyAnimationClip(e, clip);
                }

            var path = AssetUtil.GetPath($"Animations/{key}.anim");
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }


        private static (Dictionary<InventoryNode, AnimationClip>, Dictionary<InventoryNode, AnimationClip>) GenerateNonUniqueClips(InventoryNode node)
        {
            var enabledClips = new Dictionary<InventoryNode, AnimationClip>();
            var disabledClips = new Dictionary<InventoryNode, AnimationClip>();

            foreach (var child in node.Children)
            {
                enabledClips.Add(child, GenerateAnimationClip($"{child.Key}_enabled", child.Avatar, child.Value.GameObjects, child.Value.ObjectsToDisable, child.Value.AdditionalAnimations, child.Value.BlendShapesToChange, child.Value.MaterialsToReplace));
                disabledClips.Add(child, GenerateAnimationClip($"{child.Key}_disabled", child.Avatar, new GameObject[] { }, child.Value.GameObjects));
            }

            return (enabledClips, disabledClips);
        }


        private static (Dictionary<InventoryNode, AnimationClip>, AnimationClip) GenerateUniqueClips(InventoryNode node)
        {
            var allObjects = node.Children.SelectMany(child => child.Value.GameObjects).ToImmutableHashSet();
            var groups = node.Children.Select(
                child => (
                    child,
                    enabled: child.Value.GameObjects.ToList(),
                    disabled: allObjects.Where(o => !child.Value.GameObjects.Contains(o)).Concat(child.Value.ObjectsToDisable).ToList()
                )
            ).ToDictionary(
                e => e.child,
                e => (e.enabled, e.disabled)
            );

            // generate enabled clips
            var clips = groups.ToDictionary(
                e => e.Key,
                e => GenerateAnimationClip(e.Key.Key, e.Key.Avatar, e.Value.enabled, e.Value.disabled, e.Key.Value.AdditionalAnimations, e.Key.Value.BlendShapesToChange, e.Key.Value.MaterialsToReplace)
            );

            // generate disable all clips
            var disableAllClip = GenerateAnimationClip(
                $"{node.Key}_disable_all",
                node.Avatar,
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

        private static void SetupParameterDrivers(AnimatorState state, InventoryNode node)
        {
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
            driver.parameters.AddRange(node.Value.ParameterDriverBindings.Select(e => e.parameter));
        }

        // generate animations for node itself
        private static AnimatorController GenerateNonUniqueAnimatorController(InventoryNode node, AnimationClip enabledClip, AnimationClip disabledClip)
        {
            var path = AssetUtil.GetPath($"Controllers/{node.Key}.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.RemoveLayer(0);
            controller.AddLayer(node.Key);
            var layer = controller.layers[0];
            controller.AddParameter(node.ParameterName, AnimatorControllerParameterType.Bool);

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
                transition.AddCondition(AnimatorConditionMode.If, 0, node.ParameterName);
                SetupParameterDrivers(enabledState, node);
            }
            {
                disabledState.motion = disabledClip;
                var transition = layer.stateMachine.AddAnyStateTransition(disabledState);
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.IfNot, 0, node.ParameterName);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static List<AnimatorController> GenerateNonUniqueAnimatorControllers(
            Dictionary<InventoryNode, AnimationClip> enabledClips,
            Dictionary<InventoryNode, AnimationClip> disabledClips
        ) => enabledClips.Keys
                    .Select(node => GenerateNonUniqueAnimatorController(node, enabledClips[node], disabledClips[node]))
                    .ToList();

        // generate animations for node.Children
        private static AnimatorController GenerateUniqueAnimatorController(InventoryNode node, Dictionary<InventoryNode, AnimationClip> clips, AnimationClip disableAllClip)
        {
            var path = AssetUtil.GetPath($"Controllers/{node.Key}_select.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.RemoveLayer(0);
            controller.AddLayer(node.Key);
            var layer = controller.layers[0];
            controller.AddParameter(node.IndexKey, AnimatorControllerParameterType.Int);

            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

            // default or disable animation
            {
                var defaultNode = node.Children.FirstOrDefault(e => e.Value.Default);
                var defaultState = layer.stateMachine.AddState("Default", new Vector3(0, 0));
                defaultState.motion = disableAllClip;
                if (defaultNode != null)
                {
                    defaultState.name = defaultNode.Value.Name;
                    defaultState.motion = clips[defaultNode];
                    SetupParameterDrivers(defaultState, defaultNode);
                }

                layer.stateMachine.defaultState = defaultState;
            }

            var position = new Vector3(0, 50);
            var gab = new Vector3(0, 50);

            // non default animations
            foreach (var child in node.Children.Where(e => !e.Value.Default))
            {
                var enabledClip = clips[child];
                var state = layer.stateMachine.AddState(child.Value.Name, position);
                state.motion = enabledClip;

                var enabledTransition = layer.stateMachine.AddAnyStateTransition(state);
                SetupTransition(enabledTransition);
                enabledTransition.AddCondition(AnimatorConditionMode.Equals, child.Index, child.ParameterName);

                var exitTransition = state.AddExitTransition();
                SetupTransition(exitTransition);
                exitTransition.AddCondition(AnimatorConditionMode.NotEqual, child.Index, child.ParameterName);

                SetupParameterDrivers(state, child);
                position += gab;
            }

            layer.stateMachine.exitPosition = new Vector3(400, 0);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}