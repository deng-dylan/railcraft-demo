using System.Linq;
using RailCraft.ThirdPerson.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Wraps the curated worker prefab in the stable player-root contract and
    /// creates a project-owned locomotion controller from the supplied clips.
    /// </summary>
    public static class ArtAlphaCharacterVisualFactory
    {
        public const string VisualRootName = "WorkerVisual";
        public const string CharacterRootPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Character/Bubba_The_Handyman/URP";
        public const string PlayerPrefabPath =
            CharacterRootPath + "/Prefabs/Handyman_ver_1.prefab";
        public const string IdleAnimationPath =
            CharacterRootPath + "/Animations/Handyman_idle_1_animation.fbx";
        public const string WalkAnimationPath =
            CharacterRootPath + "/Animations/Handyman_walk_animation.fbx";
        public const string RunAnimationPath =
            CharacterRootPath + "/Animations/Handyman_run_animation.fbx";
        public const string ControllerFolderPath =
            "Assets/RailCraft/ThirdPerson/Art/Animation";
        public const string ControllerPath =
            ControllerFolderPath + "/ArtAlphaWorkerLocomotion.controller";

        private const string IdleClipName = "Handyman_idle1_anim";
        private const string WalkClipName = "Handyman_walk_anim";
        private const string RunClipName = "Handyman_run_anim";
        private const float TargetCharacterHeight = 1.76f;

        public static bool IsCharacterAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null &&
            LoadClip(IdleAnimationPath, IdleClipName) != null &&
            LoadClip(WalkAnimationPath, WalkClipName) != null &&
            LoadClip(RunAnimationPath, RunClipName) != null;

        public static bool TryCreate(
            Transform parent,
            out GameObject visual,
            out Animator animator)
        {
            visual = null;
            animator = null;
            if (parent == null || !IsCharacterAvailable)
                return false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (visual == null)
                return false;

            visual.name = VisualRootName;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            StripGameplayComponents(visual);

            animator = visual.GetComponentInChildren<Animator>(true);
            var controller = EnsureLocomotionController();
            if (animator == null || controller == null || !NormalizeCharacter(visual))
            {
                Object.DestroyImmediate(visual);
                visual = null;
                animator = null;
                return false;
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return true;
        }

        public static AnimatorController EnsureLocomotionController()
        {
            EnsureControllerFolder();
            var idle = LoadClip(IdleAnimationPath, IdleClipName);
            var walk = LoadClip(WalkAnimationPath, WalkClipName);
            var run = LoadClip(RunAnimationPath, RunClipName);
            if (idle == null || walk == null || run == null)
                return null;

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            if (!controller.parameters.Any(parameter =>
                    parameter.name == ThirdPersonLocomotionAnimator.SpeedParameterName &&
                    parameter.type == AnimatorControllerParameterType.Float))
            {
                controller.AddParameter(
                    ThirdPersonLocomotionAnimator.SpeedParameterName,
                    AnimatorControllerParameterType.Float);
            }
            if (!controller.parameters.Any(parameter =>
                    parameter.name == ThirdPersonLocomotionAnimator.GroundedParameterName &&
                    parameter.type == AnimatorControllerParameterType.Bool))
            {
                controller.AddParameter(
                    ThirdPersonLocomotionAnimator.GroundedParameterName,
                    AnimatorControllerParameterType.Bool);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null && candidate.name == "Locomotion");
            if (state == null)
                state = stateMachine.AddState("Locomotion");

            var blendTree = state.motion as BlendTree;
            if (blendTree == null)
            {
                blendTree = new BlendTree
                {
                    name = "ArtAlphaWorkerLocomotionBlend",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = ThirdPersonLocomotionAnimator.SpeedParameterName,
                    useAutomaticThresholds = false
                };
                AssetDatabase.AddObjectToAsset(blendTree, controller);
                state.motion = blendTree;
            }

            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = ThirdPersonLocomotionAnimator.SpeedParameterName;
            blendTree.useAutomaticThresholds = false;
            blendTree.children = new[]
            {
                new ChildMotion { motion = idle, threshold = 0f, timeScale = 1f },
                new ChildMotion { motion = walk, threshold = 0.58f, timeScale = 1f },
                new ChildMotion { motion = run, threshold = 1f, timeScale = 1f }
            };
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadClip(string assetPath, string preferredName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name == preferredName) ??
                AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }

        private static bool NormalizeCharacter(GameObject visual)
        {
            if (!TryGetBounds(visual, out var bounds) || bounds.size.y <= 0.001f)
                return false;

            var scale = TargetCharacterHeight / bounds.size.y;
            visual.transform.localScale = Vector3.one * scale;
            if (!TryGetBounds(visual, out bounds))
                return false;

            visual.transform.position += Vector3.up *
                (visual.transform.parent.position.y - bounds.min.y);
            return true;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return true;
        }

        private static void StripGameplayComponents(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
                Object.DestroyImmediate(rigidbody);
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                Object.DestroyImmediate(camera);
            foreach (var light in root.GetComponentsInChildren<Light>(true))
                Object.DestroyImmediate(light);
            foreach (var audioSource in root.GetComponentsInChildren<AudioSource>(true))
                Object.DestroyImmediate(audioSource);
        }

        private static void EnsureControllerFolder()
        {
            const string artRoot = "Assets/RailCraft/ThirdPerson/Art";
            if (!AssetDatabase.IsValidFolder(ControllerFolderPath))
                AssetDatabase.CreateFolder(artRoot, "Animation");
        }
    }
}
