using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BgmVolumeReceiver : MonoBehaviour
{
    private AudioSource _src;

    private void Awake()
    {
        _src = GetComponent<AudioSource>();
        Apply(AudioSettingsModel.GetBgm());
    }

    private void OnEnable()
    {
        AudioSettingsModel.OnBgmChanged += Apply;
        Apply(AudioSettingsModel.GetBgm());
    }

    private void OnDisable()
    {
        AudioSettingsModel.OnBgmChanged -= Apply;
    }

    private void Apply(float v)
    {
        if (_src) _src.volume = Mathf.Clamp01(v);
    }
}
