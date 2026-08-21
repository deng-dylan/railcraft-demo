using System;
using System.Collections;
using System.IO;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Opt-in built-player smoke path. It stays inert during normal play and is enabled only
    /// by the -whitebox-smoke command-line switch used during delivery verification.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiteboxAutomatedSmokeRunner : MonoBehaviour
    {
        public const string SmokeArgument = "-whitebox-smoke";
        public const string VariantArgumentPrefix = "-whitebox-smoke-variant=";
        public const string ScreenshotArgumentPrefix = "-whitebox-smoke-screenshot=";
        public const string BogieScreenshotArgumentPrefix = "-whitebox-smoke-bogie-screenshot=";
        public const string LandingScreenshotArgumentPrefix = "-whitebox-smoke-landing-screenshot=";
        public const string SuccessLogMarker = "RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED";
        public const string FailureLogMarker = "RAILCRAFT_WHITEBOX_SMOKE_FAILED";

        public static bool TryGetRequestedVariant(
            string[] arguments,
            out AssemblyVariantId variant)
        {
            variant = AssemblyVariantId.FuxingDemo;
            if (arguments == null)
                return false;

            var argument = arguments.FirstOrDefault(value =>
                value.StartsWith(VariantArgumentPrefix, StringComparison.OrdinalIgnoreCase));
            if (argument == null)
                return false;

            var key = argument.Substring(VariantArgumentPrefix.Length);
            return AssemblyVariantCatalog.TryParse(key, out variant);
        }

        private IEnumerator Start()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (!arguments.Any(argument =>
                string.Equals(argument, SmokeArgument, StringComparison.OrdinalIgnoreCase)))
                yield break;

            yield return null;

            string failure = null;
            try
            {
                ValidateEscapeMenu();
                DriveToCompletion(arguments);
                ValidateCompletionState();
            }
            catch (Exception exception)
            {
                failure = exception.ToString();
            }

            if (failure == null)
            {
                var screenshotPath = FindScreenshotPath(arguments);
                if (!string.IsNullOrWhiteSpace(screenshotPath))
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(screenshotPath);
                        if (!string.IsNullOrWhiteSpace(directory))
                            Directory.CreateDirectory(directory);
                        CaptureCompletionPreview(screenshotPath);
                    }
                    catch (Exception exception)
                    {
                        failure = exception.ToString();
                    }

                    if (failure == null)
                        yield return null;
                }

                if (failure == null)
                {
                    try
                    {
                        InvokeResetButton();
                    }
                    catch (Exception exception)
                    {
                        failure = exception.ToString();
                    }
                }
            }

            yield return null;

            if (failure == null)
            {
                try
                {
                    ValidateResetState();
                }
                catch (Exception exception)
                {
                    failure = exception.ToString();
                }
            }

            if (failure == null)
            {
                Debug.Log(SuccessLogMarker);
                Application.Quit(0);
            }
            else
            {
                Debug.LogError($"{FailureLogMarker}\n{failure}");
                Application.Quit(1);
            }
        }

        private static void ValidateEscapeMenu()
        {
            var menu = FindSingle<WhiteboxMainMenuController>();
            var inputLock = FindSingle<ThirdPersonInputLock>();
            Ensure(menu.HasActiveGame, "Smoke session was not active before ESC menu validation.");
            Ensure(!menu.IsMenuVisible, "Main menu stayed visible after smoke session startup.");

            Ensure(menu.HandleEscapePressed(), "ESC menu did not open during active gameplay.");
            Ensure(menu.IsMenuVisible, "ESC menu root did not become visible.");
            Ensure(inputLock.InputLocked, "ESC menu did not lock player input.");

            Ensure(menu.HandleEscapePressed(), "ESC menu did not resume the active game.");
            Ensure(!menu.IsMenuVisible, "ESC menu stayed visible after resume.");
            Ensure(!inputLock.InputLocked, "ESC menu kept player input locked after resume.");
        }

        private void DriveToCompletion(string[] arguments)
        {
            var host = FindSingle<WhiteboxGameSessionHost>();
            if (TryGetRequestedVariant(arguments, out var requestedVariant))
            {
                Ensure(
                    host.SelectedAssemblyVariant == requestedVariant,
                    $"Smoke requested {requestedVariant}, but active plan is " +
                    $"{host.SelectedAssemblyVariant}.");
            }
            var quizPanel = FindSingle<WhiteboxQuizPanel>();
            var inputLock = FindSingle<ThirdPersonInputLock>();
            var scanner = FindSingle<PlayerInteractionScanner>();
            ValidatePlayerSystems();
            var quizStations = FindAll<QuizPartStation>()
                .OrderBy(station => station.RewardPart)
                .ToArray();
            Ensure(quizStations.Length == 14, $"Expected 14 quiz stations, found {quizStations.Length}.");

            foreach (var station in quizStations)
            {
                FocusStation(station, scanner);
                Ensure(scanner.TryInteract(), $"Scanner could not open quiz for {station.RewardPart}.");
                Ensure(station.IsQuizOpen, $"Quiz did not open for {station.RewardPart}.");
                Ensure(inputLock.InputLocked, "Input was not locked while the quiz was open.");

                var optionButtons = quizPanel.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("QuizOption", StringComparison.Ordinal))
                    .OrderBy(button => button.name)
                    .ToArray();
                Ensure(optionButtons.Length == 4, $"Expected 4 quiz option buttons, found {optionButtons.Length}.");
                var activeOptionButtons = optionButtons
                    .Where(button => button.gameObject.activeInHierarchy)
                    .ToArray();
                Ensure(activeOptionButtons.Length == station.CurrentQuestion.Options.Count,
                    $"Quiz option count mismatch for {station.RewardPart}.");
                foreach (var button in activeOptionButtons)
                {
                    button.onClick.Invoke();
                    if (station.RewardUnlocked)
                        break;
                }

                Ensure(station.RewardUnlocked, $"No answer unlocked {station.RewardPart}.");
                Ensure(!inputLock.InputLocked, "Input stayed locked after a correct answer.");
                scanner.ScanNow();
                Ensure(scanner.TryInteract(), $"Scanner could not collect {station.RewardPart}.");
                Ensure(station.IsCollected, $"Reward {station.RewardPart} was not collected.");
            }

            Ensure(host.Session.InventoryParts.Count == 14, "Inventory did not contain all 14 parts.");

            var moduleStations = FindAll<ModuleAssemblyStation>()
                .OrderBy(station => station.ModuleId)
                .ToArray();
            Ensure(moduleStations.Length == 4, $"Expected 4 part assembly stations, found {moduleStations.Length}.");
            foreach (var station in moduleStations)
            {
                FocusStation(station, scanner);
                for (var index = 0; index < station.RequiredPartCount; index++)
                    Ensure(scanner.TryInteract(), $"Scanner could not install part {index + 1} for {station.ModuleId}.");
                Ensure(station.IsComplete, $"Module {station.ModuleId} did not complete.");
                Ensure(station.InstalledPartCount == station.RequiredPartCount,
                    $"Module {station.ModuleId} did not install every required part.");
            }

            Ensure(host.Session.InventoryParts.Count == 2,
                "Only carbody and central traction device should remain before landing.");

            var compositeStation = FindSingle<CompositeAssemblyStation>();
            FocusStation(compositeStation, scanner);
            for (var index = 0; index < compositeStation.RequiredModuleCount; index++)
                Ensure(scanner.TryInteract(), $"Scanner could not install bogie child module {index + 1}.");
            Ensure(compositeStation.IsComplete, "Bogie structure did not complete.");

            var bogieScreenshotPath = FindArgumentPath(arguments, BogieScreenshotArgumentPrefix);
            if (!string.IsNullOrWhiteSpace(bogieScreenshotPath))
            {
                var directory = Path.GetDirectoryName(bogieScreenshotPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                CaptureBogiePreview(bogieScreenshotPath, compositeStation.transform);
            }

            var finalStation = FindSingle<FinalAssemblyStation>();
            FocusStation(finalStation, scanner);
            for (var index = 0; index < finalStation.RequiredInputCount; index++)
                Ensure(scanner.TryInteract(), $"Scanner could not install landing input {index + 1}.");
            Ensure(finalStation.IsLandingComplete, "Landing assembly did not complete.");
            Ensure(!finalStation.IsVehicleComplete, "Landing incorrectly skipped commissioning.");
            Ensure(finalStation.InstalledInputCount == finalStation.RequiredInputCount,
                "Landing did not install all four inputs.");
            Ensure(host.Session.InventoryParts.Count == 0, "Inventory was not consumed by landing.");

            var landingScreenshotPath = FindArgumentPath(arguments, LandingScreenshotArgumentPrefix);
            if (!string.IsNullOrWhiteSpace(landingScreenshotPath))
            {
                var directory = Path.GetDirectoryName(landingScreenshotPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                CaptureLandingPreview(landingScreenshotPath, finalStation.transform);
            }

            var commissioningStations = FindAll<CommissioningStation>();
            Ensure(commissioningStations.Length == 3,
                $"Expected 3 commissioning stations, found {commissioningStations.Length}.");
            var testStation = commissioningStations.Single(station => station.Action == CommissioningAction.Test);
            var retuneStation = commissioningStations.Single(station => station.Action == CommissioningAction.Retune);
            var inspectionStation = commissioningStations.Single(station => station.Action == CommissioningAction.Inspect);

            FocusStation(testStation, scanner);
            Ensure(scanner.TryInteract(), "Initial commissioning interaction failed.");
            Ensure(host.Session.CommissioningPhase == CommissioningPhase.NeedsRetuning,
                "Initial commissioning did not exercise the failure branch.");

            FocusStation(retuneStation, scanner);
            Ensure(scanner.TryInteract(), "Retuning interaction failed.");
            Ensure(host.Session.CommissioningPhase == CommissioningPhase.ReadyForInspection,
                "Retuning did not unlock inspection.");

            FocusStation(inspectionStation, scanner);
            Ensure(scanner.TryInteract(), "Inspection interaction failed.");
            Ensure(host.Session.CommissioningPhase == CommissioningPhase.ReadyForRetest,
                "Inspection did not return to commissioning.");

            FocusStation(testStation, scanner);
            Ensure(scanner.TryInteract(), "Commissioning retest interaction failed.");
            Ensure(finalStation.IsVehicleComplete, "Vehicle did not enter service after the retest.");
        }

        private static void ValidatePlayerSystems()
        {
            var motor = FindSingle<ThirdPersonMotor>();
            var startingPosition = motor.transform.position;
            motor.TickMovement(Vector2.up, false, 0.12f);
            var planarDelta = Vector3.ProjectOnPlane(
                motor.transform.position - startingPosition,
                Vector3.up);
            Ensure(planarDelta.sqrMagnitude > 0.001f,
                "Third-person motor did not move the CharacterController.");

            var orbitCamera = FindSingle<ThirdPersonOrbitCamera>();
            var startingYaw = orbitCamera.Yaw;
            orbitCamera.ApplyLook(new Vector2(20f, 0f));
            Ensure(Mathf.Abs(Mathf.DeltaAngle(startingYaw, orbitCamera.Yaw)) > 0.01f,
                "Third-person orbit camera did not respond to look input.");
        }

        private static void FocusStation(
            Component station,
            PlayerInteractionScanner scanner)
        {
            var box = station.GetComponent<BoxCollider>();
            Ensure(box != null && box.isTrigger, $"{station.name} is missing its interaction trigger.");
            var approachZ = box.center.z - box.size.z * 0.5f - 0.85f;
            var playerTransform = scanner.transform;
            var controller = playerTransform.GetComponent<CharacterController>();
            Ensure(controller != null, "Player CharacterController was not found.");

            controller.enabled = false;
            var position = station.transform.TransformPoint(new Vector3(0f, 0f, approachZ));
            position.y = 0.05f;
            playerTransform.position = position;
            var direction = station.transform.position - position;
            direction.y = 0f;
            playerTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            controller.enabled = true;
            Physics.SyncTransforms();
            scanner.ScanNow();
            Ensure(ReferenceEquals(scanner.CurrentTarget, station),
                $"Interaction scanner did not select {station.name}.");
        }

        private static void ValidateCompletionState()
        {
            var host = FindSingle<WhiteboxGameSessionHost>();
            Ensure(host.Session.IsVehicleComplete,
                "Session did not report vehicle completion.");
            Ensure(host.Session.FlowStatus == AssemblyFlowStatus.Completed,
                "Assembly state machine did not reach Completed.");
            var progress = FindSingle<WhiteboxAssemblyProgressPresenter>();
            Ensure(progress.TotalSteps == 23 && progress.CompletedSteps == 23 &&
                progress.CompletionPercent == 100,
                "Assembly progress presenter did not reach 23/23 and 100 percent.");
            Ensure(FindSingle<WhiteboxKnowledgePresenter>().IsCatalogUnlocked,
                "Engineering knowledge compendium did not unlock.");
            var save = FindSingle<WhiteboxSaveController>();
            Ensure(save.HasActiveSession && save.HasSave,
                "Completed session was not saved.");
            Ensure(!FindSingle<WhiteboxMainMenuController>().IsMenuVisible,
                "Main menu remained visible during automated play.");
            Ensure(FindSingle<WhiteboxHudPresenter>().IsCompletionVisible,
                "Completion UI was not visible.");
            Ensure(FindSingle<ThirdPersonInputLock>().InputLocked,
                "Completion UI did not lock player input.");
        }

        private static void InvokeResetButton()
        {
            var resetButton = FindAll<Button>()
                .SingleOrDefault(button => button.name == "ResetWhiteboxButton");
            Ensure(resetButton != null, "Reset button was not found.");
            resetButton.onClick.Invoke();
        }

        private static void ValidateResetState()
        {
            var host = FindSingle<WhiteboxGameSessionHost>();
            Ensure(!host.Session.IsVehicleComplete, "Vehicle stayed complete after reset.");
            Ensure(!host.Session.IsLandingComplete, "Landing stayed complete after reset.");
            Ensure(host.Session.CommissioningPhase == CommissioningPhase.Locked,
                "Commissioning state did not reset to locked.");
            Ensure(host.Session.FlowStatus == AssemblyFlowStatus.Pending,
                "Assembly state machine did not reset to Pending.");
            Ensure(host.Session.InventoryParts.Count == 0, "Inventory was not empty after reset.");
            var progress = FindSingle<WhiteboxAssemblyProgressPresenter>();
            Ensure(progress.CompletedSteps == 0 && progress.CompletionPercent == 0,
                "Assembly progress did not reset to zero.");
            Ensure(!FindSingle<WhiteboxKnowledgePresenter>().IsCatalogUnlocked,
                "Engineering knowledge compendium stayed unlocked after reset.");
            Ensure(!FindSingle<WhiteboxHudPresenter>().IsCompletionVisible,
                "Completion UI stayed visible after reset.");
            Ensure(!FindSingle<ThirdPersonInputLock>().InputLocked,
                "Input stayed locked after reset.");
            Ensure(FindAll<QuizPartStation>().All(station =>
                    !station.RewardUnlocked && !station.IsCollected && !station.IsQuizOpen),
                "A quiz station did not reset.");
            Ensure(FindAll<ModuleAssemblyStation>().All(station =>
                    !station.IsComplete && station.InstalledPartCount == 0),
                "A module station did not reset.");
            var compositeStation = FindSingle<CompositeAssemblyStation>();
            Ensure(!compositeStation.IsComplete && compositeStation.InstalledModuleCount == 0,
                "Composite assembly station did not reset.");
            var finalStation = FindSingle<FinalAssemblyStation>();
            Ensure(!finalStation.IsVehicleComplete
                && !finalStation.IsLandingComplete
                && finalStation.InstalledInputCount == 0,
                "Landing station did not reset.");
        }

        private static T FindSingle<T>() where T : UnityEngine.Object
        {
            var results = FindAll<T>();
            Ensure(results.Length == 1, $"Expected one {typeof(T).Name}, found {results.Length}.");
            return results[0];
        }

        private static T[] FindAll<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        private static string FindScreenshotPath(string[] arguments)
        {
            return FindArgumentPath(arguments, ScreenshotArgumentPrefix);
        }

        private static string FindArgumentPath(string[] arguments, string prefix)
        {
            var argument = arguments.FirstOrDefault(value =>
                value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            return argument == null
                ? string.Empty
                : argument.Substring(prefix.Length).Trim('"');
        }

        private static void CaptureBogiePreview(string path, Transform station)
        {
            CaptureStationPreview(
                path,
                station,
                new Vector3(5.8f, 3.8f, -5.6f),
                new Vector3(0f, 1.35f, 0f),
                34f);
        }

        private static void CaptureLandingPreview(string path, Transform station)
        {
            CaptureStationPreview(
                path,
                station,
                // The landing station now sits on the east side of the hall.
                // Shoot from the open south-west side so the right factory
                // wall cannot occlude the 25.7 m coach and both bogies.
                new Vector3(-12f, 8f, -27f),
                new Vector3(0f, 2.6f, 0f),
                40f);
        }

        private static void CaptureCompletionPreview(string path)
        {
            // The completion UI is the evidence for this shot.  The dropped
            // vehicle can sit very close to the camera after the expanded
            // landing lane was laid out, so hide it only for this one render
            // and restore the scene immediately afterwards.
            var droppedVehicle = FindAll<Transform>()
                .SingleOrDefault(item => string.Equals(item.name, "DroppedVehicle", StringComparison.Ordinal));
            var wasActive = droppedVehicle != null && droppedVehicle.gameObject.activeSelf;

            try
            {
                if (droppedVehicle != null)
                    droppedVehicle.gameObject.SetActive(false);
                CaptureRenderedPreview(path);
            }
            finally
            {
                if (droppedVehicle != null)
                    droppedVehicle.gameObject.SetActive(wasActive);
            }
        }

        private static void CaptureStationPreview(
            string path,
            Transform station,
            Vector3 localCameraPosition,
            Vector3 localTarget,
            float fieldOfView)
        {
            var camera = UnityEngine.Camera.main ?? FindSingle<UnityEngine.Camera>();
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;
            var previousFieldOfView = camera.fieldOfView;
            var canvases = FindAll<Canvas>();
            var canvasStates = canvases.Select(canvas => canvas.enabled).ToArray();
            var feedback = station.GetComponent<InteractableVisualFeedback>();
            var wasHighlighted = feedback != null && feedback.IsHighlighted;
            var worldLabels = FindAll<TextMesh>()
                .Select(label => label.gameObject)
                .Distinct()
                .ToArray();
            var worldLabelStates = worldLabels.Select(item => item.activeSelf).ToArray();
            var roofBeams = FindAll<Transform>()
                .Where(item => string.Equals(item.name, "RoofBeams", StringComparison.Ordinal))
                .Select(item => item.gameObject)
                .Distinct()
                .ToArray();
            var roofBeamStates = roofBeams.Select(item => item.activeSelf).ToArray();
            var playerRenderers = FindAll<ThirdPersonMotor>()
                .SelectMany(motor => motor.GetComponentsInChildren<Renderer>(true))
                .Distinct()
                .ToArray();
            var playerRendererStates = playerRenderers.Select(renderer => renderer.enabled).ToArray();

            try
            {
                if (feedback != null)
                {
                    feedback.ClearFeedback();
                    feedback.SetHighlighted(false);
                }
                foreach (var canvas in canvases)
                    canvas.enabled = false;
                foreach (var label in worldLabels)
                    label.SetActive(false);
                foreach (var roofBeam in roofBeams)
                    roofBeam.SetActive(false);
                foreach (var renderer in playerRenderers)
                    renderer.enabled = false;

                var target = station.TransformPoint(localTarget);
                camera.transform.position = station.TransformPoint(localCameraPosition);
                camera.transform.rotation = Quaternion.LookRotation(
                    target - camera.transform.position,
                    Vector3.up);
                camera.fieldOfView = fieldOfView;
                CaptureRenderedPreview(path);
            }
            finally
            {
                if (feedback != null)
                    feedback.SetHighlighted(wasHighlighted);
                camera.transform.position = previousPosition;
                camera.transform.rotation = previousRotation;
                camera.fieldOfView = previousFieldOfView;
                for (var index = 0; index < canvases.Length; index++)
                    canvases[index].enabled = canvasStates[index];
                for (var index = 0; index < worldLabels.Length; index++)
                    worldLabels[index].SetActive(worldLabelStates[index]);
                for (var index = 0; index < roofBeams.Length; index++)
                    roofBeams[index].SetActive(roofBeamStates[index]);
                for (var index = 0; index < playerRenderers.Length; index++)
                    playerRenderers[index].enabled = playerRendererStates[index];
            }
        }

        private static void CaptureRenderedPreview(string path)
        {
            const int width = 1600;
            const int height = 900;
            var camera = UnityEngine.Camera.main ?? FindSingle<UnityEngine.Camera>();
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var canvases = FindAll<Canvas>();
            var previousModes = new RenderMode[canvases.Length];
            var previousCameras = new UnityEngine.Camera[canvases.Length];
            var previousPlaneDistances = new float[canvases.Length];

            try
            {
                for (var index = 0; index < canvases.Length; index++)
                {
                    previousModes[index] = canvases[index].renderMode;
                    previousCameras[index] = canvases[index].worldCamera;
                    previousPlaneDistances[index] = canvases[index].planeDistance;
                    if (canvases[index].renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        canvases[index].renderMode = RenderMode.ScreenSpaceCamera;
                        canvases[index].worldCamera = camera;
                        canvases[index].planeDistance = 1f;
                    }
                }

                Canvas.ForceUpdateCanvases();
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                for (var index = 0; index < canvases.Length; index++)
                {
                    canvases[index].renderMode = previousModes[index];
                    canvases[index].worldCamera = previousCameras[index];
                    canvases[index].planeDistance = previousPlaneDistances[index];
                }
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                Destroy(texture);
                Destroy(renderTexture);
            }
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
