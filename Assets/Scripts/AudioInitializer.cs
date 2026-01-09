using UnityEngine;

public class AudioInitializer : MonoBehaviour
{
    [Header("BGM_VOL 또는 SFX_VOL 중 하나를 입력하세요")]
    public string saveKey = "BGM_VOL";

    void Start()
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            // 설정창이 꺼져 있어도, 소리 오브젝트는 시작하자마자 저장된 값을 읽습니다.
            float savedVol = PlayerPrefs.GetFloat(saveKey, 1f);
            audio.volume = savedVol;
            Debug.Log($"{gameObject.name} 볼륨이 {savedVol}로 자동 설정되었습니다.");
        }
    }
}