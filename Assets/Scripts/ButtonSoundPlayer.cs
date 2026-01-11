using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundPlayer : MonoBehaviour
{
    public AudioSource audioSource; // 소리를 재생할 스피커
    public AudioClip clickSound;    // 재생할 효과음 파일

    void Start()
    {
        // 이 스크립트가 붙어있는 오브젝트의 버튼 컴포넌트를 가져옵니다.
        Button btn = GetComponent<Button>();

        if (btn != null)
        {
            // 버튼을 누를 때 PlaySound 함수가 실행되도록 연결합니다.
            btn.onClick.AddListener(PlaySound);
        }
    }

    void PlaySound()
    {
        if (audioSource != null && clickSound != null)
        {
            // 효과음을 한 번 재생합니다.
            audioSource.PlayOneShot(clickSound);
        }
    }
}