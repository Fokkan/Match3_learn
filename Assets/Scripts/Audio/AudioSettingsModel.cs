using System;
using UnityEngine;

public static class AudioSettingsModel
{
    public const string KEY_BGM = "BGMVolume";
    public const string KEY_SFX = "SFXVolume";

    public static event Action<float> OnBgmChanged;
    public static event Action<float> OnSfxChanged;

    public static float GetBgm(float defaultValue = 1f)
        => Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, defaultValue));

    public static float GetSfx(float defaultValue = 1f)
        => Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, defaultValue));

    public static void SetBgm(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();
        OnBgmChanged?.Invoke(value);
    }

    public static void SetSfx(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();
        OnSfxChanged?.Invoke(value);
    }

    /// <summary>씬 진입 시 저장값을 리시버들에게 한 번 밀어줌</summary>
    public static void BroadcastCurrent()
    {
        OnBgmChanged?.Invoke(GetBgm());
        OnSfxChanged?.Invoke(GetSfx());
    }
}
