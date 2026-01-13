using UnityEngine;

public class SfxVolumeReceiver : MonoBehaviour
{
    [Tooltip("BoardManager의 SFX AudioSource. 비워두면 BoardManager에서 자동 탐색합니다.")]
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (!sfxSource)
        {
            // 같은 오브젝트 또는 자식에서 찾아봄
            sfxSource = GetComponent<AudioSource>();
            if (!sfxSource) sfxSource = GetComponentInChildren<AudioSource>(true);
        }

        Apply(AudioSettingsModel.GetSfx());
    }

    private void OnEnable()
    {
        AudioSettingsModel.OnSfxChanged += Apply;
        Apply(AudioSettingsModel.GetSfx());
    }

    private void OnDisable()
    {
        AudioSettingsModel.OnSfxChanged -= Apply;
    }

    private void Apply(float v)
    {
        if (sfxSource) sfxSource.volume = Mathf.Clamp01(v);
    }
}
