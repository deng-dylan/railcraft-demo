using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RailCraft.CameraSystem
{
    [Serializable]
    public sealed class FactoryCameraShot
    {
        public string shotId;
        public Transform focusAnchor;
        public float distance = 9f;
        public float yaw = 35f;
        public float pitch = 35f;
    }

    [DisallowMultipleComponent]
    public sealed class CameraShotDirector : MonoBehaviour
    {
        [SerializeField] private FactoryCameraController cameraController;
        [SerializeField] private List<FactoryCameraShot> shots = new List<FactoryCameraShot>();
        [SerializeField] private float transitionDuration = 0.8f;

        private Coroutine transition;

        public string CurrentShotId { get; private set; }
        public bool IsTransitioning => transition != null;

        public void Configure(FactoryCameraController controller,
            IEnumerable<FactoryCameraShot> configuredShots, float duration = 0.8f)
        {
            cameraController = controller;
            shots = configuredShots == null
                ? new List<FactoryCameraShot>()
                : new List<FactoryCameraShot>(configuredShots);
            transitionDuration = Mathf.Max(0f, duration);
        }

        public bool Focus(string shotId)
        {
            if (cameraController == null || string.IsNullOrWhiteSpace(shotId))
                return false;

            var shot = shots.Find(candidate => candidate != null && candidate.shotId == shotId);
            if (shot == null || shot.focusAnchor == null)
                return false;

            if (transition != null)
                StopCoroutine(transition);
            transition = StartCoroutine(TransitionTo(shot));
            CurrentShotId = shotId;
            return true;
        }

        public void AddOrReplaceShot(FactoryCameraShot shot)
        {
            if (shot == null || string.IsNullOrWhiteSpace(shot.shotId))
                throw new ArgumentException("A camera shot requires an id.", nameof(shot));

            var index = shots.FindIndex(candidate => candidate != null && candidate.shotId == shot.shotId);
            if (index >= 0)
                shots[index] = shot;
            else
                shots.Add(shot);
        }

        private IEnumerator TransitionTo(FactoryCameraShot shot)
        {
            var startFocus = cameraController.FocusPosition;
            var startDistance = cameraController.Distance;
            var startYaw = cameraController.Yaw;
            var startPitch = cameraController.Pitch;

            if (transitionDuration <= 0f)
            {
                cameraController.SetView(shot.focusAnchor.position,
                    shot.distance, shot.yaw, shot.pitch);
                transition = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                cameraController.SetView(
                    Vector3.Lerp(startFocus, shot.focusAnchor.position, t),
                    Mathf.Lerp(startDistance, shot.distance, t),
                    Mathf.LerpAngle(startYaw, shot.yaw, t),
                    Mathf.Lerp(startPitch, shot.pitch, t));
                yield return null;
            }

            cameraController.SetView(shot.focusAnchor.position,
                shot.distance, shot.yaw, shot.pitch);
            transition = null;
        }
    }
}
