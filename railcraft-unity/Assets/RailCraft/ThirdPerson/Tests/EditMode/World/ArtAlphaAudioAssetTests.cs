using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class ArtAlphaAudioAssetTests
    {
        [Test]
        public void CoreUiAudioClipsAreAvailableAndAttachToOnePresenter()
        {
            var root = new GameObject("ArtAlphaAudioTestRoot");
            var buttonObject = new GameObject("TestButton", typeof(RectTransform), typeof(Button));
            buttonObject.transform.SetParent(root.transform, false);
            try
            {
                var sessionHost = root.AddComponent<WhiteboxGameSessionHost>();
                Assert.That(ArtAlphaAudioFactory.AreCoreClipsAvailable, Is.True);
                Assert.That(
                    ArtAlphaAudioFactory.TryAttach(
                        root,
                        sessionHost,
                        root.transform,
                        out var presenter),
                    Is.True);

                Assert.That(presenter, Is.Not.Null);
                Assert.That(presenter.ButtonClip, Is.Not.Null);
                Assert.That(presenter.SuccessClip, Is.Not.Null);
                Assert.That(presenter.FailureClip, Is.Not.Null);
                Assert.That(presenter.RepairClip, Is.Not.Null);
                Assert.That(root.GetComponents<WhiteboxAudioPresenter>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponents<AudioSource>(), Has.Length.EqualTo(1));
                Assert.That(root.GetComponent<AudioSource>().playOnAwake, Is.False);
                Assert.That(root.GetComponent<AudioSource>().spatialBlend, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
