using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.Interaction
{
    public sealed class DragDropController : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private DraggableModule[] draggableModules = Array.Empty<DraggableModule>();
        [SerializeField] private DropTarget[] dropTargets = Array.Empty<DropTarget>();
        [SerializeField] private Behaviour cameraOrbitController;
        [SerializeField] private float rejectedReturnDuration = 0.25f;

        private readonly InputAction pointerPositionAction = new InputAction(
            "RailCraftPointerPosition", InputActionType.Value, "<Mouse>/position");
        private readonly InputAction pointerPressAction = new InputAction(
            "RailCraftPointerPress", InputActionType.Button, "<Mouse>/leftButton");
        private readonly HashSet<DraggableModule> returningModules = new HashSet<DraggableModule>();
        private readonly Dictionary<DraggableModule, DropTarget> snappingModules =
            new Dictionary<DraggableModule, DropTarget>();

        private IDragAuthorization authorization;
        private DraggableModule activeModule;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private bool orbitWasEnabled;
        private bool isDisabling;
        private bool isEndingDrag;

        public event Action<string> DropCompleted;
        public event Action<DragDropResult> DropRejected;
        public event Action<bool> PartDragStateChanged;

        public bool IsPartDragActive => activeModule != null;

        private void OnEnable()
        {
            isDisabling = false;
            pointerPositionAction.Enable();
            pointerPressAction.Enable();
        }

        private void OnDisable()
        {
            isDisabling = true;
            pointerPositionAction.Disable();
            pointerPressAction.Disable();
            StopAllCoroutines();

            var interruptedDrag = activeModule;
            activeModule = null;
            if (interruptedDrag != null)
            {
                interruptedDrag.CancelDragAndRestoreStart();
                PartDragStateChanged?.Invoke(false);
            }

            foreach (var module in returningModules)
                module?.FinishReturn();
            returningModules.Clear();

            var interruptedSnaps = new List<KeyValuePair<DraggableModule, DropTarget>>(snappingModules);
            snappingModules.Clear();
            foreach (var entry in interruptedSnaps)
            {
                if (entry.Key == null)
                    continue;

                if (entry.Value == null || entry.Value.SnapAnchor == null)
                {
                    entry.Key.CancelDragAndRestoreStart();
                    continue;
                }

                entry.Key.FinishSnap(entry.Value.SnapAnchor.position, entry.Value.SnapAnchor.rotation);
                DropCompleted?.Invoke(entry.Key.StepId);
            }

            RestoreCameraOrbit();
        }

        private void OnDestroy()
        {
            pointerPositionAction.Dispose();
            pointerPressAction.Dispose();
        }

        private void Update()
        {
            var pointerPosition = pointerPositionAction.ReadValue<Vector2>();
            if (pointerPressAction.WasPressedThisFrame())
                TryBeginDragAtScreenPosition(pointerPosition);

            if (activeModule == null)
                return;

            DragToScreenPosition(pointerPosition);
            if (pointerPressAction.WasReleasedThisFrame())
                ReleaseAt(activeModule.transform.position);
        }

        public void Configure(
            IDragAuthorization configuredAuthorization,
            DropTarget[] configuredTargets,
            Behaviour configuredCameraOrbitController)
        {
            authorization = configuredAuthorization;
            dropTargets = configuredTargets ?? Array.Empty<DropTarget>();
            cameraOrbitController = configuredCameraOrbitController;
            foreach (var target in dropTargets)
            {
                if (target != null)
                    target.SetAuthorization(authorization);
            }
        }

        public bool TryBeginDrag(DraggableModule module)
        {
            if (!isActiveAndEnabled || isDisabling || isEndingDrag
                || module == null || activeModule != null)
                return false;

            if (authorization == null || !authorization.CanDrag(module.StepId))
            {
                RaiseRejected(new DragDropResult(false, "step_locked", module.StepId, null));
                return false;
            }

            if (!module.BeginDrag())
                return false;

            activeModule = module;
            DisableCameraOrbit();
            PartDragStateChanged?.Invoke(true);
            return true;
        }

        public void DragTo(Vector3 worldPosition)
        {
            activeModule?.DragTo(worldPosition);
        }

        public DragDropResult ReleaseAt(Vector3 worldPosition)
        {
            if (activeModule == null)
                return new DragDropResult(false, "not_dragging", null, null);

            var module = activeModule;
            activeModule = null;
            RestoreCameraOrbit();
            isEndingDrag = true;
            try
            {
                if (authorization == null || !authorization.CanDrag(module.StepId))
                    return Reject(module, "step_locked", null);

                var target = FindTarget(worldPosition);
                if (target == null)
                    return Reject(module, "outside_target", null);

                if (!target.CanAccept(module.StepId))
                    return Reject(module, "wrong_target", target);

                module.BeginSnap();
                var accepted = new DragDropResult(true, "accepted", module.StepId, target);
                snappingModules[module] = target;
                PartDragStateChanged?.Invoke(false);
                if (isActiveAndEnabled && !isDisabling && snappingModules.ContainsKey(module))
                    StartCoroutine(SnapModule(module, target));
                return accepted;
            }
            finally
            {
                isEndingDrag = false;
            }
        }

        private bool TryBeginDragAtScreenPosition(Vector2 screenPosition)
        {
            var cameraForInteraction = interactionCamera == null ? Camera.main : interactionCamera;
            if (cameraForInteraction == null)
                return false;

            var ray = cameraForInteraction.ScreenPointToRay(screenPosition);
            foreach (var module in draggableModules)
            {
                if (module == null || module.InteractionCollider == null || !module.InteractionCollider.enabled)
                    continue;

                if (!module.InteractionCollider.Raycast(ray, out var hit, float.PositiveInfinity))
                    continue;

                if (!TryBeginDrag(module))
                    return false;

                dragPlane = new Plane(cameraForInteraction.transform.forward, module.StartPosition);
                dragOffset = dragPlane.Raycast(ray, out var distance)
                    ? module.StartPosition - ray.GetPoint(distance)
                    : Vector3.zero;
                return true;
            }

            return false;
        }

        private void DragToScreenPosition(Vector2 screenPosition)
        {
            var cameraForInteraction = interactionCamera == null ? Camera.main : interactionCamera;
            if (cameraForInteraction == null || !dragPlane.Raycast(cameraForInteraction.ScreenPointToRay(screenPosition), out var distance))
                return;

            DragTo(cameraForInteraction.ScreenPointToRay(screenPosition).GetPoint(distance) + dragOffset);
        }

        private DragDropResult Reject(DraggableModule module, string code, DropTarget target)
        {
            module.BeginReturn();
            returningModules.Add(module);
            var rejected = new DragDropResult(false, code, module.StepId, target);
            PartDragStateChanged?.Invoke(false);
            RaiseRejected(rejected);
            if (isActiveAndEnabled && !isDisabling && returningModules.Contains(module))
                StartCoroutine(ReturnModule(module));
            return rejected;
        }

        private IEnumerator SnapModule(DraggableModule module, DropTarget target)
        {
            var startPosition = module.transform.position;
            var startRotation = module.transform.rotation;
            var duration = target.SnapDuration;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var fraction = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                module.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, target.SnapAnchor.position, fraction),
                    Quaternion.Slerp(startRotation, target.SnapAnchor.rotation, fraction));
                yield return null;
            }

            if (snappingModules.Remove(module))
            {
                module.FinishSnap(target.SnapAnchor.position, target.SnapAnchor.rotation);
                DropCompleted?.Invoke(module.StepId);
            }
        }

        private IEnumerator ReturnModule(DraggableModule module)
        {
            var returnStart = module.transform.position;
            var startPosition = module.StartPosition;
            var lockedRotation = module.LockedRotation;
            var duration = Mathf.Max(0f, rejectedReturnDuration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var fraction = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                module.transform.SetPositionAndRotation(
                    Vector3.Lerp(returnStart, startPosition, fraction),
                    lockedRotation);
                yield return null;
            }

            if (returningModules.Remove(module))
                module.FinishReturn();
        }

        private DropTarget FindTarget(Vector3 worldPosition)
        {
            DropTarget closest = null;
            var closestDistance = float.PositiveInfinity;
            foreach (var target in dropTargets)
            {
                if (target == null || target.SnapAnchor == null)
                    continue;

                var distance = Vector3.Distance(worldPosition, target.SnapAnchor.position);
                if (distance > target.SnapRadius || distance >= closestDistance)
                    continue;

                closest = target;
                closestDistance = distance;
            }

            return closest;
        }

        private void DisableCameraOrbit()
        {
            if (cameraOrbitController == null)
                return;

            orbitWasEnabled = cameraOrbitController.enabled;
            cameraOrbitController.enabled = false;
        }

        private void RestoreCameraOrbit()
        {
            if (cameraOrbitController == null)
                return;

            cameraOrbitController.enabled = orbitWasEnabled;
        }

        private void RaiseRejected(DragDropResult result)
        {
            DropRejected?.Invoke(result);
        }
    }
}
