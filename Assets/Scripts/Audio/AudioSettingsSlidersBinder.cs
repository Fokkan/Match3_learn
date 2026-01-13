using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsSlidersBinder : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Defaults")]
    [SerializeField] private float defaultBgm = 1f;
    [SerializeField] private float defaultSfx = 1f;

    private bool _isBinding;

    private void OnEnable()
    {
        // UI -> 저장값으로 초기화
        _isBinding = true;
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(AudioSettingsModel.GetBgm(defaultBgm));
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(AudioSettingsModel.GetSfx(defaultSfx));
        _isBinding = false;

        // UI 이벤트 연결
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(OnBgmChangedByUi);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSfxChangedByUi);

        // 현재값 브로드캐스트(열자마자 바로 적용)
        AudioSettingsModel.BroadcastCurrent();
    }

    private void OnDisable()
    {
        if (bgmSlider) bgmSlider.onValueChanged.RemoveListener(OnBgmChangedByUi);
        if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(OnSfxChangedByUi);
    }

    private void OnBgmChangedByUi(float v)
    {
        if (_isBinding) return;
        AudioSettingsModel.SetBgm(v);
    }

    private void OnSfxChangedByUi(float v)
    {
        if (_isBinding) return;
        AudioSettingsModel.SetSfx(v);
    }
}
