using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Play On Awake가 꺼져 있어도 자동 재생되게 보장
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log($"[BGMManager] Auto Play: {audioSource.clip.name}");
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
