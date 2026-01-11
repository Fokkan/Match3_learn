using UnityEngine;
using UnityEngine.Audio; // 오디오 믹서 조작을 위해 필요
using UnityEngine.UI;    // 슬라이더 조작을 위해 필요

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsWindow;
    public AudioMixer masterMixer; // 여기에 MainMixer를 넣을 겁니다.

    public void OpenSettings()
    {
        settingsWindow.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsWindow.SetActive(false);
    }

    // BGM 조절 함수
    public void SetBGM(float volume)
    {
        // 슬라이더의 0~1 값을 -80~0 데시벨로 변환
        masterMixer.SetFloat("BGM_Vol", Mathf.Log10(volume) * 20);
    }

    // SFX 조절 함수
    public void SetSFX(float volume)
    {
        masterMixer.SetFloat("SFX_Vol", Mathf.Log10(volume) * 20);
    }
}