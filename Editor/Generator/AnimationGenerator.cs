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
    public abstract class AnimationGenerator
    {
        public static Dictionary<AnimatorController, int> GenerateControllers(InventoryNode node)
        {
            var controllers = new Dictionary<AnimatorController, int>();
            if (node.IsInventory)
            {
                if (node.Value.IsUnique)
                {
                    var (clips, disableAllClip) = GenerateUniqueClips(node);
                    controllers[GenerateUniqueAnimatorController(node, clips, disableAllClip)] =
                        node.Value.LayerPriority;
                }
                else
                {
                    var clips = GenerateNonUniqueClips(node);
                    foreach (var child in node.ChildItems)
                    {
                        controllers[
                                GenerateNonUniqueAnimatorController(child, clips[child].Item1, clips[child].Item2)] =
                            child.Value.LayerPriority;
                    }
                }
            }

            foreach (var child in node.Children)
            {
                foreach (var entry in GenerateControllers(child))
                {
                    controllers[entry.Key] = entry.Value;
                }
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
                var enabledKeys = new Keyframe[] {new(0.0f, 1f)};
                foreach (var e in enabledObjects.Where(e => Util.IsInAvatar(avatar, e.transform)))
                {
                    var curve = new AnimationCurve(enabledKeys);
                    clip.SetCurve(GetRelativePath(e.transform, avatar.transform), typeof(GameObject), "m_IsActive",
                        curve);
                }
            }

            if (disabledObjects != null)
            {
                var disabledKeys = new Keyframe[] {new(0.0f, 0f)};
                foreach (var e in disabledObjects.Where(e => Util.IsInAvatar(avatar, e.transform)))
                {
                    var curve = new AnimationCurve(disabledKeys);
                    clip.SetCurve(GetRelativePath(e.transform, avatar.transform), typeof(GameObject), "m_IsActive",
                        curve);
                }
            }

            if (setBlendShapes != null)
                foreach (var e in setBlendShapes.Where(e => Util.IsInAvatar(avatar, e.renderer.transform)))
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(0, e.value);
                    clip.SetCurve(GetRelativePath(e.renderer.transform, avatar.transform), typeof(SkinnedMeshRenderer),
                        $"blendShape.{e.name}", curve);
                }

            if (setMaterials != null)
                foreach (var e in setMaterials.Where(e => Util.IsInAvatar(avatar, e.renderer.transform)))
                {
                    var objectPath = GetRelativePath(e.renderer.transform, avatar.transform);
                    var indexes = e.renderer.sharedMaterials.Select((m, i) => (m, i))
                        .Where(m => e.from == m.m)
                        .Select(m => m.i).ToList();
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
                        AnimationUtility.SetObjectReferenceCurve(clip, curve, new[] {keyframe});
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

        private static Dictionary<InventoryNode, (AnimationClip, AnimationClip)> GenerateNonUniqueClips(
            InventoryNode node)
        {
            var clips = new Dictionary<InventoryNode, (AnimationClip, AnimationClip)>();

            foreach (var child in node.ChildItems)
            {
                var enabledClip = GenerateAnimationClip($"{child.Key}_enabled", child.Avatar, child.Value.GameObjects,
                    child.Value.ObjectsToDisable, child.Value.AdditionalAnimations, child.Value.BlendShapesToChange,
                    child.Value.MaterialsToReplace);
                var disabledClip = GenerateAnimationClip($"{child.Key}_disabled", child.Avatar, new GameObject[] { },
                    child.Value.GameObjects);
                clips.Add(child, (enabledClip, disabledClip));
            }

            return clips;
        }

        private static (Dictionary<InventoryNode, AnimationClip>, AnimationClip) GenerateUniqueClips(InventoryNode node)
        {
            var allObjects = node.ChildItems.SelectMany(child => child.Value.GameObjects).ToImmutableHashSet();
            var groups = node.ChildItems.Select(
                child => (
                    child,
                    enabled: child.Value.GameObjects.ToList(),
                    disabled: allObjects.Where(o => !child.Value.GameObjects.Contains(o))
                        .Concat(child.Value.ObjectsToDisable).ToList()
                )
            ).ToDictionary(
                e => e.child,
                e => (e.enabled, e.disabled)
            );

            // generate enabled clips
            var clips = groups.ToDictionary(
                e => e.Key,
                e => GenerateAnimationClip(e.Key.Key, e.Key.Avatar, e.Value.enabled, e.Value.disabled,
                    e.Key.Value.AdditionalAnimations, e.Key.Value.BlendShapesToChange, e.Key.Value.MaterialsToReplace)
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
            if (node.Value.ParameterDriverBindings.Count() == 0) return;
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
            driver.parameters.AddRange(node.Value.ParameterDriverBindings.Select(e => e.parameter));
        }

        private static void AddEncodedEqualsConditions(AnimatorStateTransition transition, string parameterName,
            int value, int bits, Action<string, AnimatorControllerParameterType> addParameter)
        {
            foreach (var (name, bit) in Encode(parameterName, bits, value))
            {
                transition.AddCondition(bit == 1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, name);
                addParameter(name, AnimatorControllerParameterType.Bool);
            }
        }

        private static List<AnimatorStateTransition> AddTransitionsToDisable(InventoryNode node,
            Func<AnimatorStateTransition> addTransition, Action<string, AnimatorControllerParameterType> addParameter,
            bool recursive = true)
        {
            var transitions = new List<AnimatorStateTransition>();
            if (node.IsItem)
            {
                foreach (var (name, bit) in Encode(node.ParameterName, node.ParameterBits, node.ParameterValue))
                {
                    var transition = addTransition();
                    SetupTransition(transition);
                    transition.AddCondition(bit == 0 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, name);
                    addParameter(name, AnimatorControllerParameterType.Bool);
                    transitions.Add(transition);
                }
            }

            if (recursive && node.Parent != null)
            {
                transitions.AddRange(AddTransitionsToDisable(node.Parent, addTransition, addParameter));
            }

            return transitions;
        }

        private static void SetupTransitionConditionsToEnable(InventoryNode node, AnimatorStateTransition transition,
            Action<string, AnimatorControllerParameterType> addParameter, bool recursive = true)
        {
            if (node.IsItem)
            {
                AddEncodedEqualsConditions(transition, node.ParameterName, node.ParameterValue, node.ParameterBits,
                    addParameter);
            }

            if (recursive && node.Parent != null)
            {
                SetupTransitionConditionsToEnable(node.Parent, transition, addParameter);
            }
        }

        private static void AddTransitionToEnable(InventoryNode node, Func<AnimatorStateTransition> addTransition,
            Action<string, AnimatorControllerParameterType> addParameter, bool recursive = true)
        {
            var transition = addTransition();
            SetupTransition(transition);
            SetupTransitionConditionsToEnable(node, transition, addParameter, recursive);
        }

        // generate animations for node itself
        private static AnimatorController GenerateNonUniqueAnimatorController(InventoryNode node,
            AnimationClip enabledClip, AnimationClip disabledClip)
        {
            var path = AssetUtil.GetPath($"Controllers/{node.Key}/Toggle.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.RemoveLayer(0);
            controller.AddLayer(node.Key);
            var layer = controller.layers[0];
            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

            var parameters = new Dictionary<string, AnimatorControllerParameterType>
            {
                [node.ParameterName] = AnimatorControllerParameterType.Int,
                [GetSyncedParameterName(node.ParameterName)] = AnimatorControllerParameterType.Bool
            };

            {
                // setup idle
                var idleState = layer.stateMachine.AddState("Idle", new Vector3(0, 0));
                layer.stateMachine.defaultState = idleState;

                // transition to idle when parent is disabled
                AddTransitionsToDisable(node.Parent,
                    () => layer.stateMachine.AddAnyStateTransition(idleState),
                    (name, type) => parameters[name] = type);
            }

            var enabledState = layer.stateMachine.AddState($"Enabled ({node.EscapedName})", new Vector3(0, 50));
            var disabledState = layer.stateMachine.AddState($"Disabled ({node.EscapedName})", new Vector3(0, 100));

            {
                enabledState.motion = enabledClip;
                AddTransitionToEnable(node,
                    () => layer.stateMachine.AddAnyStateTransition(enabledState),
                    (name, type) => parameters[name] = type);
                SetupParameterDrivers(enabledState, node);
            }

            {
                disabledState.motion = disabledClip;
                var transitions = AddTransitionsToDisable(node,
                    () => layer.stateMachine.AddAnyStateTransition(disabledState),
                    (name, type) => parameters[name] = type,
                    recursive: false);
                // this state requires parent to be enabled
                foreach (var transition in transitions)
                {
                    SetupTransitionConditionsToEnable(node.Parent, transition, (name, type) => parameters[name] = type,
                        recursive: true);
                }
            }

            foreach (var parameter in parameters)
            {
                controller.AddParameter(parameter.Key, parameter.Value);
            }

            SetupEncoder(controller, node.ParameterName, 1, 1);
            SetupDecoder(controller, node.ParameterName, 1, 1);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController GenerateUniqueAnimatorController(InventoryNode node,
            Dictionary<InventoryNode, AnimationClip> clips, AnimationClip disableAllClip)
        {
            var path = AssetUtil.GetPath($"Controllers/{node.Key}.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.RemoveLayer(0);
            controller.AddLayer(node.Key);
            var layer = controller.layers[0];
            layer.stateMachine.entryPosition = new Vector3(-200, 0);
            layer.stateMachine.anyStatePosition = new Vector3(-200, 50);

            var parameters = new Dictionary<string, AnimatorControllerParameterType>
            {
                [node.Key] = AnimatorControllerParameterType.Int,
                [GetSyncedParameterName(node.Key)] = AnimatorControllerParameterType.Bool
            };
            {
                // setup idle
                var idleState = layer.stateMachine.AddState("Idle", new Vector3(0, 0));
                layer.stateMachine.defaultState = idleState;

                // transition to idle when parent is disabled
                AddTransitionsToDisable(node,
                    () => layer.stateMachine.AddAnyStateTransition(idleState),
                    (name, type) => parameters[name] = type);
            }

            // default or disabled
            {
                var defaultNode = node.ChildItems.FirstOrDefault(e => e.Value.Default);
                var defaultState = layer.stateMachine.AddState("Default", new Vector3(0, 50));
                if (defaultNode != null)
                {
                    defaultState.name = defaultNode.EscapedName;
                    defaultState.motion = clips[defaultNode];
                    SetupParameterDrivers(defaultState, defaultNode);
                }
                else
                {
                    defaultState.name = "Disabled";
                    defaultState.motion = disableAllClip;
                }

                var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
                SetupTransition(transition);
                // conditions of parents to be enabled
                SetupTransitionConditionsToEnable(node, transition, (name, type) => parameters[name] = type);
                // and itself
                AddEncodedEqualsConditions(transition, node.Key, 0, node.ChildrenBits,
                    (name, type) => parameters[name] = type);
            }

            var position = new Vector3(0, 100);
            var gab = new Vector3(0, 50);

            // non default item animations
            foreach (var child in node.ChildItems.Where(e => e.IsItem).Where(e => !e.Value.Default))
            {
                var enabledClip = clips[child];
                var state = layer.stateMachine.AddState(child.EscapedName, position);
                state.motion = enabledClip;

                AddTransitionToEnable(child,
                    () => layer.stateMachine.AddAnyStateTransition(state),
                    (name, type) => parameters[name] = type);
                SetupParameterDrivers(state, child);
                position += gab;
            }

            foreach (var parameter in parameters)
            {
                controller.AddParameter(parameter.Key, parameter.Value);
            }

            SetupEncoder(controller, node.Key, node.ChildrenBits, node.MaxChildrenIndex);
            SetupDecoder(controller, node.Key, node.ChildrenBits, node.MaxChildrenIndex);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static string GetSyncedParameterName(string parameterName) => $"{parameterName}/Synced";

        private static string GetEncodedParameterName(string parameterName, int bit) => $"{parameterName}/Bits/{bit}";

        public static List<(string, int)> Encode(string parameterName, int bits, int value)
        {
            var list = new List<(string, int)>();
            for (int i = 0; i < bits; i++)
            {
                int bit = (value >> (bits - 1 - i)) & 1;
                list.Add((GetEncodedParameterName(parameterName, i), bit));
            }

            return list;
        }

        private static void SetupEncoder(AnimatorController controller, string parameterName, int bits, int maxIndex)
        {
            controller.AddLayer($"{parameterName}/Encoder");
            var layer = controller.layers[controller.layers.Length - 1];


            layer.stateMachine.entryPosition = new Vector3(0, 0);
            layer.stateMachine.anyStatePosition = new Vector3(0, 50);

            var idleState = layer.stateMachine.AddState("Wait for sync");
            layer.stateMachine.defaultState = idleState;

            controller.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);

            for (int i = 0; i <= maxIndex; i++)
            {
                var state = layer.stateMachine.AddState(i.ToString(), new Vector3(200, 50 + i * 50));
                var transition = layer.stateMachine.AddAnyStateTransition(state);
                SetupTransition(transition);

                transition.AddCondition(AnimatorConditionMode.Equals, i, parameterName);
                transition.AddCondition(AnimatorConditionMode.If, 0, GetSyncedParameterName(parameterName));
                transition.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
                var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                foreach (var (name, value) in Encode(parameterName, bits, i))
                {
                    driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter()
                    {
                        name = name,
                        value = value
                    });
                }
            }
        }

        private static void SetupDecoder(AnimatorController controller, string parameterName, int bits, int maxIndex)
        {
            controller.AddLayer($"{parameterName}/Decoder");
            var layer = controller.layers[controller.layers.Length - 1];

            layer.stateMachine.entryPosition = new Vector3(0, 0);
            layer.stateMachine.anyStatePosition = new Vector3(0, 50);

            for (int i = 0; i <= maxIndex; i++)
            {
                var state = layer.stateMachine.AddState(i.ToString(), new Vector3(200, i * 50));
                if (i == 0) layer.stateMachine.defaultState = state;
                var transition = layer.stateMachine.AddAnyStateTransition(state);
                SetupTransition(transition);

                foreach (var (name, value) in Encode(parameterName, bits, i))
                {
                    transition.AddCondition(value == 1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0,
                        name);
                }

                var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>()
                {
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        name = parameterName,
                        value = i
                    },
                    new VRC_AvatarParameterDriver.Parameter()
                    {
                        name = GetSyncedParameterName(parameterName),
                        value = 1
                    }
                };
            }
        }
    }
}