using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;

    void Awake()
    {
        // 1. 만약 이미 음악을 재생 중인 매니저가 있다면
        if (instance != null)
        {
            // 새로 만들어진 매니저는 파괴합니다 (중복 방지)
            Destroy(gameObject);
            return;
        }

        // 2. 처음 실행되는 매니저라면 자신을 instance에 저장하고
        instance = this;

        // 3. 씬이 바뀌어도 이 오브젝트를 파괴하지 않도록 설정합니다.
        DontDestroyOnLoad(gameObject);
    }
}