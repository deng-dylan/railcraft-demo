using System;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Mixed-input station for the diagram's landing operation. Completing this station
    /// unlocks commissioning; it does not finish the game by itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FinalAssemblyStation : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private ModuleId targetModuleId = ModuleId.Landing;
        [SerializeField] private string stationDisplayName = "落车工位";
        [SerializeField] private ModuleId[] requiredModules = Array.Empty<ModuleId>();
        [SerializeField] private PartId[] requiredParts = Array.Empty<PartId>();
        [SerializeField] private Transform[] moduleSnapSlots = Array.Empty<Transform>();
        [SerializeField] private Transform[] partSnapSlots = Array.Empty<Transform>();
        [SerializeField] private GameObject[] moduleVisuals = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] partVisuals = Array.Empty<GameObject>();
        [SerializeField] private GameObject completedLandingVisual;

        private WhiteboxGameSessionHost subscribedHost;

        public ModuleId TargetModuleId => targetModuleId;
        public int InstalledInputCount => CountInstalledModules() + CountInstalledParts();
        public int RequiredInputCount => (requiredModules?.Length ?? 0) + (requiredParts?.Length ?? 0);
        public bool IsLandingComplete => sessionHost != null && sessionHost.Session.IsLandingComplete;
        public bool IsVehicleComplete => sessionHost != null && sessionHost.Session.IsVehicleComplete;

        public string InteractionPrompt
        {
            get
            {
                if (sessionHost == null || IsLandingComplete)
                    return string.Empty;

                var moduleIndex = FindNextModuleIndex();
                if (moduleIndex >= 0)
                {
                    var child = requiredModules[moduleIndex];
                    return sessionHost.Session.IsModuleComplete(child)
                        ? $"按 E 将{WhiteboxDisplayNames.Module(child)}送入{stationDisplayName}"
                        : $"请先完成{WhiteboxDisplayNames.Module(child)}";
                }

                var partIndex = FindNextPartIndex();
                if (partIndex < 0)
                    return string.Empty;
                var part = requiredParts[partIndex];
                return sessionHost.Session.InventoryContains(part)
                    ? $"按 E 将{WhiteboxDisplayNames.Part(part)}送入{stationDisplayName}"
                    : $"缺少{WhiteboxDisplayNames.Part(part)}，请先答题拾取";
            }
        }

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            ModuleId configuredTargetModuleId,
            string configuredStationDisplayName,
            ModuleId[] configuredRequiredModules,
            PartId[] configuredRequiredParts,
            Transform[] configuredModuleSnapSlots,
            Transform[] configuredPartSnapSlots,
            GameObject[] configuredModuleVisuals,
            GameObject[] configuredPartVisuals,
            GameObject configuredCompletedLandingVisual)
        {
            ValidateConfiguration(
                configuredRequiredModules,
                configuredRequiredParts,
                configuredModuleSnapSlots,
                configuredPartSnapSlots,
                configuredModuleVisuals,
                configuredPartVisuals);
            Unsubscribe();
            sessionHost = configuredSessionHost;
            targetModuleId = configuredTargetModuleId;
            stationDisplayName = string.IsNullOrWhiteSpace(configuredStationDisplayName)
                ? "落车工位"
                : configuredStationDisplayName;
            requiredModules = (ModuleId[])configuredRequiredModules.Clone();
            requiredParts = (PartId[])configuredRequiredParts.Clone();
            moduleSnapSlots = (Transform[])configuredModuleSnapSlots.Clone();
            partSnapSlots = (Transform[])configuredPartSnapSlots.Clone();
            moduleVisuals = (GameObject[])configuredModuleVisuals.Clone();
            partVisuals = (GameObject[])configuredPartVisuals.Clone();
            completedLandingVisual = configuredCompletedLandingVisual;
            Subscribe();
            RefreshVisuals();
        }

        public bool CanInteract(InteractionContext context)
        {
            return sessionHost != null && !IsLandingComplete;
        }

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
                return;

            var moduleIndex = FindNextModuleIndex();
            if (moduleIndex >= 0)
            {
                InstallModule(moduleIndex);
                return;
            }

            var partIndex = FindNextPartIndex();
            if (partIndex >= 0)
                InstallPart(partIndex);
        }

        private void InstallModule(int moduleIndex)
        {
            var child = requiredModules[moduleIndex];
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
            AfterInputInstalled(WhiteboxDisplayNames.Module(child));
        }

        private void InstallPart(int partIndex)
        {
            var part = requiredParts[partIndex];
            if (!sessionHost.Session.InventoryContains(part))
            {
                sessionHost.NotifyFeedback($"库存中没有{WhiteboxDisplayNames.Part(part)}");
                return;
            }

            var result = sessionHost.InstallPart(targetModuleId, part);
            if (!result.Accepted)
            {
                sessionHost.NotifyFeedback($"无法安装{WhiteboxDisplayNames.Part(part)}（{result.Status}）");
                return;
            }
            AfterInputInstalled(WhiteboxDisplayNames.Part(part));
        }

        private void AfterInputInstalled(string inputName)
        {
            RefreshVisuals();
            if (IsLandingComplete)
            {
                sessionHost.NotifyFeedback("落车完成，车辆进入调试阶段");
                sessionHost.SetObjective("前往调试判定工位进行首次调试");
            }
            else
            {
                sessionHost.NotifyFeedback($"已安装{inputName}");
                sessionHost.SetObjective($"继续落车装配（{InstalledInputCount}/{RequiredInputCount}）");
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

        private int FindNextPartIndex()
        {
            if (sessionHost == null || requiredParts == null)
                return -1;
            for (var index = 0; index < requiredParts.Length; index++)
            {
                if (!sessionHost.Session.IsPartInstalled(targetModuleId, requiredParts[index]))
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

        private int CountInstalledParts()
        {
            if (sessionHost == null || requiredParts == null)
                return 0;
            var count = 0;
            for (var index = 0; index < requiredParts.Length; index++)
            {
                if (sessionHost.Session.IsPartInstalled(targetModuleId, requiredParts[index]))
                    count++;
            }
            return count;
        }

        private void RefreshVisuals()
        {
            if (requiredModules != null)
            {
                for (var index = 0; index < requiredModules.Length; index++)
                {
                    var installed = sessionHost != null
                        && sessionHost.Session.IsModuleInstalled(targetModuleId, requiredModules[index]);
                    if (moduleVisuals != null && index < moduleVisuals.Length && moduleVisuals[index] != null)
                    {
                        if (installed)
                            SnapVisual(moduleVisuals[index].transform, moduleSnapSlots[index]);
                        // Once the dropped vehicle is shown, hide the four staging
                        // tokens so their geometry cannot overlap the completed body.
                        moduleVisuals[index].SetActive(installed && !IsLandingComplete);
                    }
                }
            }

            if (requiredParts != null)
            {
                for (var index = 0; index < requiredParts.Length; index++)
                {
                    var installed = sessionHost != null
                        && sessionHost.Session.IsPartInstalled(targetModuleId, requiredParts[index]);
                    if (partVisuals != null && index < partVisuals.Length && partVisuals[index] != null)
                    {
                        if (installed)
                            SnapVisual(partVisuals[index].transform, partSnapSlots[index]);
                        partVisuals[index].SetActive(installed && !IsLandingComplete);
                    }
                }
            }

            if (completedLandingVisual != null)
                completedLandingVisual.SetActive(IsLandingComplete);
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
            ModuleId[] modules,
            PartId[] parts,
            Transform[] moduleSlots,
            Transform[] partSlots,
            GameObject[] configuredModuleVisuals,
            GameObject[] configuredPartVisuals)
        {
            if (modules == null)
                throw new ArgumentNullException(nameof(modules));
            if (parts == null)
                throw new ArgumentNullException(nameof(parts));
            if (modules.Length + parts.Length == 0)
                throw new ArgumentException("Landing requires at least one input.");
            if (moduleSlots == null || moduleSlots.Length != modules.Length)
                throw new ArgumentException("Each required module needs one snap slot.", nameof(moduleSlots));
            if (partSlots == null || partSlots.Length != parts.Length)
                throw new ArgumentException("Each required part needs one snap slot.", nameof(partSlots));
            if (configuredModuleVisuals == null || configuredModuleVisuals.Length != modules.Length)
                throw new ArgumentException("Each required module needs one visual.", nameof(configuredModuleVisuals));
            if (configuredPartVisuals == null || configuredPartVisuals.Length != parts.Length)
                throw new ArgumentException("Each required part needs one visual.", nameof(configuredPartVisuals));
        }
    }
}
