using System;
using UnityEngine;

namespace RailCraft.ThirdPerson.FinalShowcase
{
    public enum FinalShowcaseCameraPreset
    {
        Overview = 0,
        Head = 1,
        Side = 2,
        Departure = 3
    }

    /// <summary>
    /// Pure presentation state shared by runtime input, UI and edit-mode tests.
    /// It deliberately owns no scene references.
    /// </summary>
    public sealed class FinalShowcasePresentationState
    {
        private const float ProgressEpsilon = 0.0001f;

        public int CameraCount { get; private set; }
        public int CarCount { get; private set; }
        public int CameraIndex { get; private set; } = -1;
        public int SelectedCarIndex { get; private set; } = -1;
        public bool ExplodedTarget { get; private set; }
        public float ExplodeProgress { get; private set; }

        public FinalShowcaseCameraPreset CameraPreset =>
            CameraIndex >= 0 && CameraIndex <= (int)FinalShowcaseCameraPreset.Departure
                ? (FinalShowcaseCameraPreset)CameraIndex
                : FinalShowcaseCameraPreset.Overview;

        public bool IsExplosionAnimating =>
            Mathf.Abs(ExplodeProgress - (ExplodedTarget ? 1f : 0f)) > ProgressEpsilon;

        public bool ConfigureCounts(int cameraCount, int carCount)
        {
            if (cameraCount < 0)
                throw new ArgumentOutOfRangeException(nameof(cameraCount));
            if (carCount < 0)
                throw new ArgumentOutOfRangeException(nameof(carCount));

            var previousCamera = CameraIndex;
            var previousCar = SelectedCarIndex;
            CameraCount = cameraCount;
            CarCount = carCount;
            CameraIndex = cameraCount == 0
                ? -1
                : Mathf.Clamp(CameraIndex < 0 ? 0 : CameraIndex, 0, cameraCount - 1);
            SelectedCarIndex = carCount == 0
                ? -1
                : Mathf.Clamp(SelectedCarIndex < 0 ? 0 : SelectedCarIndex, 0, carCount - 1);
            return previousCamera != CameraIndex || previousCar != SelectedCarIndex;
        }

        public bool SelectCamera(FinalShowcaseCameraPreset preset)
        {
            return SelectCamera((int)preset);
        }

        public bool SelectCamera(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= CameraCount || cameraIndex == CameraIndex)
                return false;

            CameraIndex = cameraIndex;
            return true;
        }

        public bool StepCamera(int delta)
        {
            if (CameraCount == 0 || delta == 0)
                return false;

            return SelectCamera(WrapIndex(CameraIndex + delta, CameraCount));
        }

        public bool SelectCar(int carIndex)
        {
            if (carIndex < 0 || carIndex >= CarCount || carIndex == SelectedCarIndex)
                return false;

            SelectedCarIndex = carIndex;
            return true;
        }

        public bool StepCar(int delta)
        {
            if (CarCount == 0 || delta == 0)
                return false;

            return SelectCar(WrapIndex(SelectedCarIndex + delta, CarCount));
        }

        public bool SetExploded(bool exploded)
        {
            if (ExplodedTarget == exploded)
                return false;

            ExplodedTarget = exploded;
            return true;
        }

        public bool ToggleExploded()
        {
            ExplodedTarget = !ExplodedTarget;
            return true;
        }

        public bool AdvanceExplosion(float deltaTime, float duration)
        {
            var target = ExplodedTarget ? 1f : 0f;
            var previous = ExplodeProgress;
            if (duration <= ProgressEpsilon)
            {
                ExplodeProgress = target;
            }
            else
            {
                var step = Mathf.Max(0f, deltaTime) / duration;
                ExplodeProgress = Mathf.MoveTowards(ExplodeProgress, target, step);
            }

            return Mathf.Abs(previous - ExplodeProgress) > ProgressEpsilon;
        }

        public bool Reset(bool immediate)
        {
            var previousCamera = CameraIndex;
            var previousCar = SelectedCarIndex;
            var previousTarget = ExplodedTarget;
            var previousProgress = ExplodeProgress;

            CameraIndex = CameraCount == 0 ? -1 : 0;
            SelectedCarIndex = CarCount == 0 ? -1 : 0;
            ExplodedTarget = false;
            if (immediate)
                ExplodeProgress = 0f;

            return previousCamera != CameraIndex ||
                previousCar != SelectedCarIndex ||
                previousTarget != ExplodedTarget ||
                Mathf.Abs(previousProgress - ExplodeProgress) > ProgressEpsilon;
        }

        public static int WrapIndex(int value, int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        public static Vector3 CalculateExplodedOffset(
            int carIndex,
            int carCount,
            float longitudinalGap,
            float lateralOffset,
            float verticalLift)
        {
            if (carCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(carCount));
            if (carIndex < 0 || carIndex >= carCount)
                throw new ArgumentOutOfRangeException(nameof(carIndex));

            var distanceFromPositiveEnd = (carCount - 1) * 0.5f - carIndex;
            var lateralDirection = carIndex % 2 == 0 ? -1f : 1f;
            return new Vector3(
                lateralDirection * Mathf.Max(0f, lateralOffset),
                Mathf.Max(0f, verticalLift),
                distanceFromPositiveEnd * Mathf.Max(0f, longitudinalGap));
        }

        public static float SmoothProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return progress * progress * (3f - 2f * progress);
        }

        public static float CalculateExponentialBlend(float sharpness, float deltaTime)
        {
            if (sharpness <= 0f || deltaTime <= 0f)
                return 0f;

            return 1f - Mathf.Exp(-sharpness * deltaTime);
        }
    }
}

