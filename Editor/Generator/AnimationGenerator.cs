using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public static IEnumerable<AnimatorController> GenerateControllers(ClosetNode node)
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
                foreach (var e in enabledObjects)
                {
                    var curve = new AnimationCurve(enabledKeys);
                    clip.SetCurve(GetRelativePath(e.transform, avatar.transform), typeof(GameObject), "m_IsActive", curve);
                }
            }

            if (disabledObjects != null)
            {
                var disabledKeys = new Keyframe[1] { new Keyframe(0.0f, 0f) };
                foreach (var e in disabledObjects)
                {
                    var curve = new AnimationCurve(disabledKeys);
                    clip.SetCurve(GetRelativePath(e.transform, avatar.transform), typeof(GameObject), "m_IsActive", curve);
                }
            }

            if (setBlendShapes != null)
                foreach (var e in setBlendShapes)
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(0, e.value);
                    clip.SetCurve(GetRelativePath(e.renderer.transform, avatar.transform), typeof(SkinnedMeshRenderer), $"blendShape.{e.name}", curve);
                }

            if (setMaterials != null)
                foreach (var e in setMaterials)
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


        private static (Dictionary<ClosetNode, AnimationClip>, Dictionary<ClosetNode, AnimationClip>) GenerateNonUniqueClips(ClosetNode node)
        {
            var enabledClips = new Dictionary<ClosetNode, AnimationClip>();
            var disabledClips = new Dictionary<ClosetNode, AnimationClip>();

            foreach (var child in node.Children)
            {
                enabledClips.Add(child, GenerateAnimationClip($"{child.Key}_enabled", child.Avatar, child.Value.GameObjects, child.Value.ObjectsToDisable, child.Value.AdditionalAnimations));
                disabledClips.Add(child, GenerateAnimationClip($"{child.Key}_disabled", child.Avatar, new GameObject[] { }, child.Value.GameObjects));
            }

            return (enabledClips, disabledClips);
        }


        private static (Dictionary<ClosetNode, AnimationClip>, AnimationClip) GenerateUniqueClips(ClosetNode node)
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

        private static AnimatorController GenerateNonUniqueAnimatorController(ClosetNode node, AnimationClip enabledClip, AnimationClip disabledClip)
        {
            var controller = new AnimatorController();
            var layer = new AnimatorControllerLayer
            {
                stateMachine = new AnimatorStateMachine(),
                name = node.Value.Name
            };

            controller.AddLayer(layer);
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
            }
            {
                disabledState.motion = disabledClip;
                var transition = layer.stateMachine.AddAnyStateTransition(disabledState);
                SetupTransition(transition);
                transition.AddCondition(AnimatorConditionMode.IfNot, 0, node.ParameterName);
            }
            var path = AssetUtil.GetPath($"Controllers/{node.Key}.controller");
            AssetDatabase.CreateAsset(controller, path);
            return controller;
        }

        private static List<AnimatorController> GenerateNonUniqueAnimatorControllers(
            Dictionary<ClosetNode, AnimationClip> enabledClips,
            Dictionary<ClosetNode, AnimationClip> disabledClips
        ) => enabledClips.Keys
                    .Select(node => GenerateNonUniqueAnimatorController(node, enabledClips[node], disabledClips[node]))
                    .ToList();

        private static AnimatorController GenerateUniqueAnimatorController(ClosetNode node, Dictionary<ClosetNode, AnimationClip> clips, AnimationClip disableAllClip)
        {
            var defaultNode = node.Children.FirstOrDefault(e => e.Value.Default);
            var controller = new AnimatorController();
            var layer = new AnimatorControllerLayer
            {
                stateMachine = new AnimatorStateMachine(),
                name = node.Key
            };

            controller.AddLayer(layer);
            controller.AddParameter(node.UniqueKey, AnimatorControllerParameterType.Int);

            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

            // default or disable animation
            {
                var defaultState = layer.stateMachine.AddState("Default", new Vector3(0, 0));
                defaultState.motion = defaultNode != null ? clips[defaultNode] : disableAllClip;
                layer.stateMachine.defaultState = defaultState;
            }

            var position = new Vector3(0, 50);
            var gab = new Vector3(0, 50);

            // enabled animations
            foreach (var child in node.Children)
            {
                var enabledClip = clips[child];
                var state = layer.stateMachine.AddState(child.Value.Name, position);
                state.motion = enabledClip;

                var enabledTransition = layer.stateMachine.AddAnyStateTransition(state);
                SetupTransition(enabledTransition);
                Debug.Log(child.ParameterName);
                enabledTransition.AddCondition(AnimatorConditionMode.Equals, child.Index, child.ParameterName);

                var exitTransition = state.AddExitTransition();
                SetupTransition(exitTransition);
                exitTransition.AddCondition(AnimatorConditionMode.NotEqual, child.Index, child.ParameterName);

                position += gab;
            }

            layer.stateMachine.exitPosition = new Vector3(400, 0);
            var path = AssetUtil.GetPath($"Controllers/{node.Key}.controller");
            AssetDatabase.CreateAsset(controller, path);
            return controller;
        }
    }
}