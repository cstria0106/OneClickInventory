using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using dog.miruku.inventory.runtime;
using PlasticPipe.PlasticProtocol.Messages;
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

        private static void AddTransitionsToDisable(InventoryNode node, Func<AnimatorStateTransition> addTransition, Action<string, AnimatorControllerParameterType> addParameter)
        {
            if (node.IsItem)
            {
                var transition = addTransition();
                SetupTransition(transition);
                if (node.ParentIsUnique)
                {
                    transition.AddCondition(AnimatorConditionMode.NotEqual, node.Index, node.ParameterName);
                    addParameter(node.ParameterName, AnimatorControllerParameterType.Int);
                }
                else
                {
                    transition.AddCondition(AnimatorConditionMode.IfNot, 0, node.ParameterName);
                    addParameter(node.ParameterName, AnimatorControllerParameterType.Bool);
                }
            }
            if (node.Parent != null)
            {
                AddTransitionsToDisable(node.Parent, addTransition, addParameter);
            }
        }

        private static void SetupTransitionConditionsToEnable(InventoryNode node, AnimatorStateTransition transition, Action<string, AnimatorControllerParameterType> addParameter)
        {
            if (node.IsItem)
            {
                if (node.ParentIsUnique)
                {
                    transition.AddCondition(AnimatorConditionMode.Equals, node.Index, node.ParameterName);
                    addParameter(node.ParameterName, AnimatorControllerParameterType.Int);
                }
                else
                {
                    transition.AddCondition(AnimatorConditionMode.If, 0, node.ParameterName);
                    addParameter(node.ParameterName, AnimatorControllerParameterType.Bool);
                }
            }

            if (node.Parent != null)
            {
                SetupTransitionConditionsToEnable(node.Parent, transition, addParameter);
            }
        }

        private static AnimatorStateTransition AddTransitionToEnable(InventoryNode node, Func<AnimatorStateTransition> addTransition, Action<string, AnimatorControllerParameterType> addParameter)
        {
            var transition = addTransition();
            SetupTransition(transition);
            SetupTransitionConditionsToEnable(node, transition, addParameter);
            return transition;
        }

        // generate animations for node itself
        private static AnimatorController GenerateNonUniqueAnimatorController(InventoryNode node, AnimationClip enabledClip, AnimationClip disabledClip)
        {
            var path = AssetUtil.GetPath($"Controllers/{node.Key}.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.RemoveLayer(0);
            controller.AddLayer(node.Key);
            var layer = controller.layers[0];
            layer.stateMachine.defaultState = layer.stateMachine.AddState("Idle", new Vector3(0, 0));
            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

            var enabledPosition = new Vector3(0, 50);
            var disabledPosition = new Vector3(0, 100);

            var enabledState = layer.stateMachine.AddState("Enabled", enabledPosition);
            var disabledState = layer.stateMachine.AddState("Disabled", disabledPosition);

            var parameters = new Dictionary<string, AnimatorControllerParameterType>();

            {
                enabledState.motion = enabledClip;
                AddTransitionToEnable(node,
                                        () => layer.stateMachine.AddAnyStateTransition(enabledState),
                                        (name, type) => parameters[name] = type);
                SetupParameterDrivers(enabledState, node);
            }
            {
                disabledState.motion = disabledClip;
                AddTransitionsToDisable(node,
                                        () => layer.stateMachine.AddAnyStateTransition(disabledState),
                                        (name, type) => parameters[name] = type);
            }

            foreach (var parameter in parameters)
            {
                controller.AddParameter(parameter.Key, parameter.Value);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static List<AnimatorController> GenerateNonUniqueAnimatorControllers(
            Dictionary<InventoryNode, AnimationClip> enabledClips,
            Dictionary<InventoryNode, AnimationClip> disabledClips
        ) => enabledClips.Keys
                    .Where(node => node.IsItem)
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
            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

            var idleState = layer.stateMachine.AddState("Idle", new Vector3(0, 0));
            layer.stateMachine.defaultState = idleState;

            var parameters = new Dictionary<string, AnimatorControllerParameterType>();

            // default or disable animation
            {
                var defaultNode = node.Children.FirstOrDefault(e => e.Value.Default);
                var defaultState = layer.stateMachine.AddState("Default", new Vector3(0, 50));
                if (defaultNode != null)
                {
                    defaultState.name = defaultNode.Value.Name;
                    defaultState.motion = clips[defaultNode];
                    SetupParameterDrivers(defaultState, defaultNode);
                }
                else
                {
                    defaultState.name = "Disabled";
                    defaultState.motion = disableAllClip;
                }

                parameters[node.IndexKey] = AnimatorControllerParameterType.Int;

                // add enable transition with condition to enable the item's parent node
                var enableTransition = AddTransitionToEnable(node,
                                        () => layer.stateMachine.AddAnyStateTransition(defaultState),
                                        (name, type) => parameters[name] = type);
                // and it self
                enableTransition.AddCondition(AnimatorConditionMode.Equals, 0, node.IndexKey);

                // add disable transitions with condition to disable the item's parent node
                AddTransitionsToDisable(node,
                                        () => defaultState.AddExitTransition(),
                                        (name, type) => parameters[name] = type);
                // and it self
                var exitTransition = defaultState.AddExitTransition();
                exitTransition.AddCondition(AnimatorConditionMode.NotEqual, 0, node.IndexKey);
            }

            var position = new Vector3(0, 100);
            var gab = new Vector3(0, 50);

            // non default item animations
            foreach (var child in node.Children.Where(e => e.IsItem).Where(e => !e.Value.Default))
            {
                var enabledClip = clips[child];
                var state = layer.stateMachine.AddState(child.Value.Name, position);
                state.motion = enabledClip;

                AddTransitionToEnable(child,
                                        () => layer.stateMachine.AddAnyStateTransition(state),
                                        (name, type) => parameters[name] = type);
                AddTransitionsToDisable(child,
                                        () => state.AddExitTransition(),
                                        (name, type) => parameters[name] = type);

                SetupParameterDrivers(state, child);
                position += gab;
            }

            layer.stateMachine.exitPosition = new Vector3(400, 0);

            foreach (var parameter in parameters)
            {
                controller.AddParameter(parameter.Key, parameter.Value);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}