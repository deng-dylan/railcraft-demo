using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    public static class ArtAlphaAudioFactory
    {
        public const string AudioRootPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Audio/UI";
        public const string ButtonClipPath = AudioRootPath + "/Buttons/button_press_1.wav";
        public const string SuccessClipPath = AudioRootPath + "/Ok/ok_1.wav";
        public const string FailureClipPath = AudioRootPath + "/Warning_Popup/warning_1.wav";
        public const string RepairClipPath = AudioRootPath + "/Repair/repair_1.wav";

        public static bool AreCoreClipsAvailable =>
            Load(ButtonClipPath) != null &&
            Load(SuccessClipPath) != null &&
            Load(FailureClipPath) != null &&
            Load(RepairClipPath) != null;

        public static bool TryAttach(
            GameObject audioHost,
            WhiteboxGameSessionHost sessionHost,
            Transform buttonRoot,
            out WhiteboxAudioPresenter presenter)
        {
            presenter = null;
            if (audioHost == null || sessionHost == null || buttonRoot == null || !AreCoreClipsAvailable)
                return false;

            presenter = audioHost.GetComponent<WhiteboxAudioPresenter>();
            if (presenter == null)
                presenter = audioHost.AddComponent<WhiteboxAudioPresenter>();
            presenter.Configure(
                sessionHost,
                buttonRoot,
                Load(ButtonClipPath),
                Load(SuccessClipPath),
                Load(FailureClipPath),
                Load(RepairClipPath));
            return true;
        }

        private static AudioClip Load(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }
    }
}
