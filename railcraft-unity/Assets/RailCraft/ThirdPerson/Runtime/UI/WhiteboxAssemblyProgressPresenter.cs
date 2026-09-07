using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Presents the recipe-driven assembly progress and finite-state-machine status.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiteboxAssemblyProgressPresenter : MonoBehaviour
    {
        public const int CommissioningStepCount = 4;

        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text stepText;
        [SerializeField] private Text percentText;
        [SerializeField] private Text statusText;

        private WhiteboxGameCatalog catalog;
        private bool subscribed;

        public int CompletedSteps { get; private set; }
        public int TotalSteps { get; private set; }
        public float Completion01 => TotalSteps <= 0 ? 0f : (float)CompletedSteps / TotalSteps;
        public int CompletionPercent => Mathf.RoundToInt(Completion01 * 100f);

        public void Configure(
            WhiteboxGameSessionHost host,
            Slider slider,
            Text configuredStepText,
            Text configuredPercentText,
            Text configuredStatusText,
            WhiteboxGameCatalog configuredCatalog = null)
        {
            Unsubscribe();
            sessionHost = host ?? throw new ArgumentNullException(nameof(host));
            progressSlider = slider;
            stepText = configuredStepText;
            percentText = configuredPercentText;
            statusText = configuredStatusText;
            catalog = configuredCatalog ?? WhiteboxGameCatalog.CreateDefault();
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            if (sessionHost == null)
                return;

            catalog ??= WhiteboxGameCatalog.CreateDefault();
            TotalSteps = CalculateTotalSteps(catalog);
            CompletedSteps = CalculateCompletedSteps(sessionHost.Session, catalog);

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.wholeNumbers = false;
                progressSlider.value = Completion01;
            }

            if (stepText != null)
                stepText.text = BuildStepLabel(CompletedSteps, TotalSteps);
            if (percentText != null)
                percentText.text = $"完成度 {CompletionPercent}%";
            if (statusText != null)
                statusText.text = $"状态：{GetStatusDisplayName(sessionHost.Session.FlowStatus)}";
        }

        public static int CalculateTotalSteps(WhiteboxGameCatalog configuredCatalog)
        {
            if (configuredCatalog == null)
                throw new ArgumentNullException(nameof(configuredCatalog));

            var total = CommissioningStepCount;
            foreach (var module in configuredCatalog.Modules)
            {
                total += WorkPackageStepCount(module);
                total += module.RequiredModules.Count;
            }
            return total;
        }

        public static int CalculateCompletedSteps(
            IWorldGameSession session,
            WhiteboxGameCatalog configuredCatalog)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (configuredCatalog == null)
                throw new ArgumentNullException(nameof(configuredCatalog));

            var completed = 0;
            foreach (var module in configuredCatalog.Modules)
            {
                if (TryGetMatchingPackage(module, out var package))
                {
                    if (IsPackageInstalled(session, module.Id, package))
                        completed++;
                }
                else
                {
                    foreach (var partId in module.RequiredParts)
                    {
                        if (session.IsPartInstalled(module.Id, partId))
                            completed++;
                    }
                }

                foreach (var childModuleId in module.RequiredModules)
                {
                    if (session.IsModuleInstalled(module.Id, childModuleId))
                        completed++;
                }
            }

            return completed + GetCompletedCommissioningStepCount(session.CommissioningPhase);
        }

        public static int CalculateCompletedSteps(
            WhiteboxGameSessionSnapshot snapshot,
            WhiteboxGameCatalog configuredCatalog)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (configuredCatalog == null)
                throw new ArgumentNullException(nameof(configuredCatalog));

            var completed = 0;
            foreach (var module in snapshot.Modules ?? Array.Empty<ModuleAssemblySnapshot>())
            {
                if (!configuredCatalog.TryGetModule(module.ModuleId, out var moduleDefinition))
                    continue;

                if (TryGetMatchingPackage(moduleDefinition, out var package))
                {
                    var installed = new HashSet<PartId>(
                        module.InstalledParts ?? Array.Empty<PartId>());
                    var packageComplete = package.RequiredParts.Count > 0 &&
                        package.RequiredParts.All(installed.Contains);
                    if (packageComplete)
                        completed++;
                }
                else
                {
                    completed += (module.InstalledParts ?? Array.Empty<PartId>()).Length;
                }
                completed += (module.InstalledModules ?? Array.Empty<ModuleId>()).Length;
            }

            return Mathf.Clamp(
                completed + GetCompletedCommissioningStepCount(snapshot.CommissioningPhase),
                0,
                CalculateTotalSteps(configuredCatalog));
        }

        public static int GetCompletedCommissioningStepCount(CommissioningPhase phase)
        {
            return phase switch
            {
                CommissioningPhase.NeedsRetuning => 1,
                CommissioningPhase.ReadyForInspection => 2,
                CommissioningPhase.ReadyForRetest => 3,
                CommissioningPhase.InService => 4,
                _ => 0
            };
        }

        public static string BuildStepLabel(int completedSteps, int totalSteps)
        {
            if (totalSteps <= 0)
                return "第0步/共0步";
            var currentStep = Mathf.Clamp(completedSteps + 1, 1, totalSteps);
            return $"第{currentStep}步/共{totalSteps}步";
        }

        public static string GetStatusDisplayName(AssemblyFlowStatus status)
        {
            return status switch
            {
                AssemblyFlowStatus.Pending => "待装配",
                AssemblyFlowStatus.InProgress => "进行中",
                AssemblyFlowStatus.Completed => "完成",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed || sessionHost == null || !isActiveAndEnabled)
                return;
            sessionHost.StateChanged += Refresh;
            sessionHost.SessionReset += Refresh;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || sessionHost == null)
                return;
            sessionHost.StateChanged -= Refresh;
            sessionHost.SessionReset -= Refresh;
            subscribed = false;
        }

        private static int WorkPackageStepCount(ModuleDefinition module)
        {
            if (module.RequiredParts.Count == 0)
                return 0;

            return TryGetMatchingPackage(module, out _)
                ? 1
                : module.RequiredParts.Count;
        }

        private static bool TryGetMatchingPackage(
            ModuleDefinition module,
            out WorkPackageDefinition package)
        {
            package = null;
            if (module == null ||
                !WhiteboxWorkPackageCatalog.TryGetForAssemblyModule(module.Id, out var candidate) ||
                !candidate.IsMaterialPackage ||
                candidate.RequiredParts.Count != module.RequiredParts.Count)
                return false;

            var required = new HashSet<PartId>(module.RequiredParts);
            foreach (var partId in candidate.RequiredParts)
            {
                if (!required.Contains(partId))
                    return false;
            }

            package = candidate;
            return true;
        }

        private static bool IsPackageInstalled(
            IWorldGameSession session,
            ModuleId moduleId,
            WorkPackageDefinition package)
        {
            foreach (var partId in package.RequiredParts)
            {
                if (!session.IsPartInstalled(moduleId, partId))
                    return false;
            }

            return package.RequiredParts.Count > 0;
        }
    }
}
