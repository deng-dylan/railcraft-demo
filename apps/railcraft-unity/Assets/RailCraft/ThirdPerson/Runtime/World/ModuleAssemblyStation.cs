using System;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    [DisallowMultipleComponent]
    public sealed class ModuleAssemblyStation : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private ModuleId moduleId;
        [SerializeField] private string stationDisplayName = "模块装配台";
        [SerializeField] private PartId[] requiredParts = Array.Empty<PartId>();
        [SerializeField] private Transform[] snapSlots = Array.Empty<Transform>();
        [SerializeField] private GameObject[] partVisuals = Array.Empty<GameObject>();
        [SerializeField] private GameObject completedModuleVisual;
        [SerializeField, TextArea] private string afterCompletionObjective = "继续收集其他模块零件";

        private WhiteboxGameSessionHost subscribedHost;

        public ModuleId ModuleId => moduleId;
        public bool IsComplete => sessionHost != null && sessionHost.Session.IsModuleComplete(moduleId);
        public int InstalledPartCount => CountInstalledParts();
        public int RequiredPartCount => requiredParts == null ? 0 : requiredParts.Length;

        public string InteractionPrompt
        {
            get
            {
                if (sessionHost == null || IsComplete)
                    return string.Empty;

                var installableIndex = FindInstallablePartIndex();
                if (installableIndex >= 0)
                    return $"按 E 将{WhiteboxDisplayNames.Part(requiredParts[installableIndex])}安装到{stationDisplayName}";

                var missingIndex = FindFirstUninstalledPartIndex();
                return missingIndex >= 0
                    ? $"缺少{WhiteboxDisplayNames.Part(requiredParts[missingIndex])}，请先答题拾取"
                    : string.Empty;
            }
        }

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            ModuleId configuredModuleId,
            string configuredStationDisplayName,
            PartId[] configuredRequiredParts,
            Transform[] configuredSnapSlots,
            GameObject[] configuredPartVisuals,
            GameObject configuredCompletedModuleVisual,
            string configuredAfterCompletionObjective)
        {
            ValidateConfiguration(configuredRequiredParts, configuredSnapSlots, configuredPartVisuals);
            Unsubscribe();
            sessionHost = configuredSessionHost;
            moduleId = configuredModuleId;
            stationDisplayName = string.IsNullOrWhiteSpace(configuredStationDisplayName)
                ? "模块装配台"
                : configuredStationDisplayName;
            requiredParts = (PartId[])configuredRequiredParts.Clone();
            snapSlots = (Transform[])configuredSnapSlots.Clone();
            partVisuals = (GameObject[])configuredPartVisuals.Clone();
            completedModuleVisual = configuredCompletedModuleVisual;
            afterCompletionObjective = configuredAfterCompletionObjective ?? string.Empty;
            Subscribe();
            RefreshVisuals();
        }

        public bool CanInteract(InteractionContext context)
        {
            return sessionHost != null && !IsComplete;
        }

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
                return;

            var partIndex = FindInstallablePartIndex();
            if (partIndex < 0)
            {
                var missingIndex = FindFirstUninstalledPartIndex();
                if (missingIndex >= 0)
                    sessionHost.NotifyFeedback($"库存中没有{WhiteboxDisplayNames.Part(requiredParts[missingIndex])}");
                return;
            }

            var partId = requiredParts[partIndex];
            var result = sessionHost.InstallPart(moduleId, partId);
            if (!result.Accepted)
            {
                sessionHost.NotifyFeedback($"无法安装{WhiteboxDisplayNames.Part(partId)}（{result.Status}）");
                return;
            }

            RefreshVisuals();
            sessionHost.NotifyFeedback($"已安装{WhiteboxDisplayNames.Part(partId)}");

            if (result.IsModuleComplete || IsComplete)
            {
                sessionHost.NotifyFeedback($"{WhiteboxDisplayNames.Module(moduleId)}组装完成");
                sessionHost.SetObjective(afterCompletionObjective);
            }
            else
            {
                sessionHost.SetObjective($"继续组装{WhiteboxDisplayNames.Module(moduleId)}（{InstalledPartCount}/{RequiredPartCount}）");
            }
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshVisuals();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || sessionHost == null || subscribedHost == sessionHost)
                return;

            subscribedHost = sessionHost;
            subscribedHost.StateChanged += RefreshVisuals;
            subscribedHost.SessionReset += RefreshVisuals;
        }

        private void Unsubscribe()
        {
            if (subscribedHost == null)
                return;

            subscribedHost.StateChanged -= RefreshVisuals;
            subscribedHost.SessionReset -= RefreshVisuals;
            subscribedHost = null;
        }

        private int FindInstallablePartIndex()
        {
            if (sessionHost == null || requiredParts == null)
                return -1;

            for (var index = 0; index < requiredParts.Length; index++)
            {
                var partId = requiredParts[index];
                if (!sessionHost.Session.IsPartInstalled(moduleId, partId)
                    && sessionHost.Session.InventoryContains(partId))
                    return index;
            }

            return -1;
        }

        private int FindFirstUninstalledPartIndex()
        {
            if (sessionHost == null || requiredParts == null)
                return -1;

            for (var index = 0; index < requiredParts.Length; index++)
            {
                if (!sessionHost.Session.IsPartInstalled(moduleId, requiredParts[index]))
                    return index;
            }

            return -1;
        }

        private int CountInstalledParts()
        {
            if (sessionHost == null || requiredParts == null)
                return 0;

            var count = 0;
            for (var index = 0; index < requiredParts.Length; index++)
            {
                if (sessionHost.Session.IsPartInstalled(moduleId, requiredParts[index]))
                    count++;
            }

            return count;
        }

        private void RefreshVisuals()
        {
            if (requiredParts == null)
                return;

            for (var index = 0; index < requiredParts.Length; index++)
            {
                var installed = sessionHost != null
                    && sessionHost.Session.IsPartInstalled(moduleId, requiredParts[index]);
                if (partVisuals != null && index < partVisuals.Length && partVisuals[index] != null)
                {
                    if (installed)
                        SnapVisual(partVisuals[index].transform, snapSlots[index]);
                    partVisuals[index].SetActive(installed);
                }
            }

            if (completedModuleVisual != null)
                completedModuleVisual.SetActive(IsComplete);
        }

        private static void SnapVisual(Transform visual, Transform slot)
        {
            if (visual == null || slot == null)
                return;

            visual.SetParent(slot, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
        }

        private static void ValidateConfiguration(
            PartId[] configuredParts,
            Transform[] configuredSlots,
            GameObject[] configuredVisuals)
        {
            if (configuredParts == null || configuredParts.Length == 0)
                throw new ArgumentException("A part assembly station requires at least one part.", nameof(configuredParts));
            if (configuredSlots == null || configuredSlots.Length != configuredParts.Length)
                throw new ArgumentException("Each required part needs one snap slot.", nameof(configuredSlots));
            if (configuredVisuals == null || configuredVisuals.Length != configuredParts.Length)
                throw new ArgumentException("Each required part needs one visual.", nameof(configuredVisuals));
        }
    }
}
