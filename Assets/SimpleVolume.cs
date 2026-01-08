using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 씬 관리를 위해 추가
using System;
using System.Collections.Generic;

public class SimpleVolume : MonoBehaviour
{
    private Slider volumeSlider;
    private float savedVolume = 1f;

    [Header("분리 설정")]
    public string saveKey = "BGM_VOL";
    public string targetTag = "BGM";

    [Header("Icon Settings")]
    public Image iconImage;
    public Sprite normalIcon;
    public Sprite muteIcon;

    private List<AudioSource> targetSources = new List<AudioSource>();
    private static event Action<string, float> OnVolumeGlobalChanged;

    void Awake() => volumeSlider = GetComponent<Slider>();

    void OnEnable()
    {
        OnVolumeGlobalChanged += SyncVolume;
        // ⭐ 씬이 로드될 때 실행될 함수 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        OnVolumeGlobalChanged -= SyncVolume;
        // ⭐ 등록 해제 (메모리 관리)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ⭐ 씬이 바뀔 때마다 실행되는 함수
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedVolume();
    }

    void Start()
    {
        ApplySavedVolume();
        volumeSlider.onValueChanged.AddListener(HandleSliderChange);
    }

    // 저장된 값을 불러와서 현재 씬의 모든 대상에게 적용하는 핵심 함수
    void ApplySavedVolume()
    {
        float lastVolume = PlayerPrefs.GetFloat(saveKey, 1f);

        // UI 슬라이더 위치 맞추기
        if (volumeSlider != null) volumeSlider.value = lastVolume;

        // 아이콘 업데이트
        UpdateIcon(lastVolume);

        // 현재 씬의 태그 대상을 다 찾아서 볼륨 조절
        FindAllTargets();
        foreach (var source in targetSources)
        {
            if (source != null) source.volume = lastVolume;
        }
    }

    void FindAllTargets()
    {
        targetSources.Clear();
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        foreach (GameObject obj in targets)
        {
            AudioSource source = obj.GetComponent<AudioSource>();
            if (source != null) targetSources.Add(source);
        }
    }

    void HandleSliderChange(float value)
    {
        PlayerPrefs.SetFloat(saveKey, value);
        PlayerPrefs.Save();
        OnVolumeGlobalChanged?.Invoke(saveKey, value);
    }

    void SyncVolume(string key, float value)
    {
        if (key != saveKey) return;
        if (volumeSlider != null) volumeSlider.value = value;

        FindAllTargets();
        foreach (var source in targetSources)
        {
            if (source != null) source.volume = value;
        }

        UpdateIcon(value);
    }

    public void ToggleMute()
    {
        if (volumeSlider.value > 0)
        {
            savedVolume = volumeSlider.value;
            HandleSliderChange(0);
        }
        else
        {
            HandleSliderChange(savedVolume > 0 ? savedVolume : 1f);
        }
    }

    void UpdateIcon(float vol)
    {
        if (iconImage == null) return;
        iconImage.sprite = (vol <= 0) ? muteIcon : normalIcon;
    }
}