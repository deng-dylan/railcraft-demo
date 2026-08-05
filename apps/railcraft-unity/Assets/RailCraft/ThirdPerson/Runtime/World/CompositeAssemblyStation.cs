using System;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    [DisallowMultipleComponent]
    public sealed class CompositeAssemblyStation : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private ModuleId targetModuleId;
        [SerializeField] private string stationDisplayName = "总成装配台";
        [SerializeField] private ModuleId[] requiredModules = Array.Empty<ModuleId>();
        [SerializeField] private Transform[] snapSlots = Array.Empty<Transform>();
        [SerializeField] private GameObject[] moduleVisuals = Array.Empty<GameObject>();
        [SerializeField] private GameObject completedVisual;
        [SerializeField, TextArea] private string afterCompletionObjective = "前往下一道工序";

        private WhiteboxGameSessionHost subscribedHost;

        public ModuleId TargetModuleId => targetModuleId;
        public int InstalledModuleCount => CountInstalledModules();
        public int RequiredModuleCount => requiredModules == null ? 0 : requiredModules.Length;
        public bool IsComplete => sessionHost != null && sessionHost.Session.IsModuleComplete(targetModuleId);

        public string InteractionPrompt
        {
            get
            {
                if (sessionHost == null || IsComplete)
                    return string.Empty;

                var nextIndex = FindNextModuleIndex();
                if (nextIndex < 0)
                    return string.Empty;
                var child = requiredModules[nextIndex];
                return sessionHost.Session.IsModuleComplete(child)
                    ? $"按 E 将{WhiteboxDisplayNames.Module(child)}安装到{stationDisplayName}"
                    : $"请先完成{WhiteboxDisplayNames.Module(child)}";
            }
        }

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            ModuleId configuredTargetModuleId,
            string configuredStationDisplayName,
            ModuleId[] configuredRequiredModules,
            Transform[] configuredSnapSlots,
            GameObject[] configuredModuleVisuals,
            GameObject configuredCompletedVisual,
            string configuredAfterCompletionObjective)
        {
            ValidateConfiguration(configuredRequiredModules, configuredSnapSlots, configuredModuleVisuals);
            Unsubscribe();
            sessionHost = configuredSessionHost;
            targetModuleId = configuredTargetModuleId;
            stationDisplayName = string.IsNullOrWhiteSpace(configuredStationDisplayName)
                ? "总成装配台"
                : configuredStationDisplayName;
            requiredModules = (ModuleId[])configuredRequiredModules.Clone();
            snapSlots = (Transform[])configuredSnapSlots.Clone();
            moduleVisuals = (GameObject[])configuredModuleVisuals.Clone();
            completedVisual = configuredCompletedVisual;
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

            var nextIndex = FindNextModuleIndex();
            if (nextIndex < 0)
                return;
            var child = requiredModules[nextIndex];
            if (!sessionHost.Session.IsModuleComplete(child))
            {
                sessionHost.NotifyFeedback($"{WhiteboxDisplayNames.Module(child)}尚未组装完成");
                return;
            }

            var result = sessionHost.InstallModule(targetModuleId, child);
            if (!result.Accepted)
            {
                sessionHost.NotifyFeedback($"无法安装{WhiteboxDisplayNames.Module(child)}（{result.Status}）");
                return;
            }

            RefreshVisuals();
            if (result.IsTargetModuleComplete || IsComplete)
            {
                sessionHost.NotifyFeedback($"{WhiteboxDisplayNames.Module(targetModuleId)}组装完成");
                sessionHost.SetObjective(afterCompletionObjective);
            }
            else
            {
                sessionHost.NotifyFeedback($"已安装{WhiteboxDisplayNames.Module(child)}");
                sessionHost.SetObjective(
                    $"继续组装{WhiteboxDisplayNames.Module(targetModuleId)}（{InstalledModuleCount}/{RequiredModuleCount}）");
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

        private int FindNextModuleIndex()
        {
            if (sessionHost == null || requiredModules == null)
                return -1;
            for (var index = 0; index < requiredModules.Length; index++)
            {
                if (!sessionHost.Session.IsModuleInstalled(targetModuleId, requiredModules[index]))
                    return index;
            }
            return -1;
        }

        private int CountInstalledModules()
        {
            if (sessionHost == null || requiredModules == null)
                return 0;
            var count = 0;
            for (var index = 0; index < requiredModules.Length; index++)
            {
                if (sessionHost.Session.IsModuleInstalled(targetModuleId, requiredModules[index]))
                    count++;
            }
            return count;
        }

        private void RefreshVisuals()
        {
            if (requiredModules == null)
                return;
            for (var index = 0; index < requiredModules.Length; index++)
            {
                var installed = sessionHost != null
                    && sessionHost.Session.IsModuleInstalled(targetModuleId, requiredModules[index]);
                if (moduleVisuals != null && index < moduleVisuals.Length && moduleVisuals[index] != null)
                {
                    if (installed)
                        SnapVisual(moduleVisuals[index].transform, snapSlots[index]);
                    moduleVisuals[index].SetActive(installed);
                }
            }
            if (completedVisual != null)
                completedVisual.SetActive(IsComplete);
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
            ModuleId[] configuredModules,
            Transform[] configuredSlots,
            GameObject[] configuredVisuals)
        {
            if (configuredModules == null || configuredModules.Length == 0)
                throw new ArgumentException("A composite station requires at least one child module.", nameof(configuredModules));
            if (configuredSlots == null || configuredSlots.Length != configuredModules.Length)
                throw new ArgumentException("Each child module needs one snap slot.", nameof(configuredSlots));
            if (configuredVisuals == null || configuredVisuals.Length != configuredModules.Length)
                throw new ArgumentException("Each child module needs one visual.", nameof(configuredVisuals));
        }
    }
}
