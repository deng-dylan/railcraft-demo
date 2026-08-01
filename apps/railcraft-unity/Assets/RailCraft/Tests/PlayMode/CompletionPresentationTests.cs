using System.Collections;
using NUnit.Framework;
using RailCraft.CameraSystem;
using RailCraft.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailCraft.Tests.PlayMode
{
    public sealed class CompletionPresentationTests
    {
        [UnityTest]
        public IEnumerator CompletionFocusesHeroAndMovesReleasedVehicleToTrackDestination()
        {
            var root = new GameObject("completion.presentation.fixture");
            var cameraObject = new GameObject("camera");
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            var focus = new GameObject("focus").transform;
            focus.SetParent(root.transform, false);
            var hero = new GameObject("hero").transform;
            hero.SetParent(root.transform, false);
            hero.position = new Vector3(4f, 2f, 8f);
            var vehicle = new GameObject("released.vehicle").transform;
            vehicle.SetParent(root.transform, false);
            var destination = new GameObject("track.destination").transform;
            destination.SetParent(root.transform, false);
            destination.position = new Vector3(0f, 0f, 9f);

            var cameraController = root.AddComponent<FactoryCameraController>();
            cameraController.Configure(camera, focus, null);
            var director = root.AddComponent<CameraShotDirector>();
            director.Configure(cameraController, new[]
            {
                new FactoryCameraShot
                {
                    shotId = "hero",
                    focusAnchor = hero,
                    distance = 12f,
                    yaw = 30f,
                    pitch = 25f
                }
            }, 0f);
            var completion = root.AddComponent<CompletionPresenter>();
            completion.ConfigureReleaseScene(vehicle, destination, director, 0f);

            completion.ShowCompleted();
            yield return null;

            Assert.That(completion.Message, Is.EqualTo(CompletionPresenter.CompletedMessage));
            Assert.That(director.CurrentShotId, Is.EqualTo("hero"));
            Assert.That(Vector3.Distance(vehicle.position, destination.position), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(cameraController.FocusPosition, hero.position), Is.LessThan(0.001f));
            Object.Destroy(root);
        }
    }
}
