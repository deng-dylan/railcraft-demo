using System;
using UnityEngine;

namespace RailCraft.ThirdPerson.UI
{
    public readonly struct WhiteboxSettingsState
    {
        public WhiteboxSettingsState(float masterVolume, int qualityLevel)
        {
            MasterVolume = Mathf.Clamp01(masterVolume);
            QualityLevel = Mathf.Clamp(qualityLevel, 0, Math.Max(0, QualitySettings.names.Length - 1));
        }

        public float MasterVolume { get; }
        public int QualityLevel { get; }
    }

    public static class WhiteboxRuntimeSettings
    {
        private const string VolumeKey = "railcraft.whitebox.settings.master-volume";
        private const string QualityKey = "railcraft.whitebox.settings.quality-level";

        public static WhiteboxSettingsState Load()
        {
            var volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            var quality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
            return new WhiteboxSettingsState(volume, quality);
        }

        public static void Apply(WhiteboxSettingsState state)
        {
            AudioListener.volume = state.MasterVolume;
            if (QualitySettings.names.Length > 0 && QualitySettings.GetQualityLevel() != state.QualityLevel)
                QualitySettings.SetQualityLevel(state.QualityLevel, true);
        }

        public static void Save(float masterVolume, int qualityLevel)
        {
            var state = new WhiteboxSettingsState(masterVolume, qualityLevel);
            PlayerPrefs.SetFloat(VolumeKey, state.MasterVolume);
            PlayerPrefs.SetInt(QualityKey, state.QualityLevel);
            PlayerPrefs.Save();
            Apply(state);
        }
    }
}
