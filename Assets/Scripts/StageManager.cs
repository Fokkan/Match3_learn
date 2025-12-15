using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 선택된 스테이지를 관리하고,
/// 선택된 스테이지 데이터를 BoardManager에 넘겨서 보드를 생성하는 매니저.
/// - 버튼에서 SelectStage(index)를 호출해 스테이지 변경
/// - 다음 스테이지로 이동(GoToNextStage)
/// - 스테이지 셀렉트 BGM / 인게임 BGM 관리
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Database")]
    public StageDatabase stageDB;      // StageDatabase ScriptableObject 연결

    [Header("Current Stage")]
    [Tooltip("0 기반 인덱스 (Stage 1 = 0, Stage 2 = 1 ...)")]
    public int currentStageIndex = 0;

    [Header("Board Reference")]
    public BoardManager board;

    [Header("BGM")]
    [Tooltip("공용 BGM 오디오 소스 (BGMPlayer의 AudioSource)")]
    public AudioSource bgmSource;

    [Tooltip("스테이지 셀렉트 화면에서 재생될 BGM")]
    public AudioClip stageSelectBgmClip;

    [Tooltip("실제 인게임(퍼즐 플레이)에서 재생될 BGM")]
    public AudioClip inGameBgmClip;

    [Range(0f, 1f)]
    [Tooltip("BGM 기본 볼륨")]
    public float bgmDefaultVolume = 0.6f;

    private Coroutine bgmFadeCoroutine;

    /// <summary>
    /// 현재 인덱스에 해당하는 StageData 반환.
    /// 스테이지 범위를 벗어나면 null.
    /// </summary>
    public StageData CurrentStage
    {
        get
        {
            if (stageDB == null || stageDB.stages == null || stageDB.stages.Length == 0)
                return null;

            currentStageIndex = Mathf.Clamp(currentStageIndex, 0, stageDB.stages.Length - 1);
            return stageDB.stages[currentStageIndex];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 씬 시작 시: 스테이지 셀렉트 BGM 재생
        PlayStageSelectBgm();
    }

    #region BGM 제어

    /// <summary>
    /// 스테이지 셀렉트 화면용 BGM을 즉시 재생.
    /// (페이드인 없이 바로 재생)
    /// </summary>
    private void PlayStageSelectBgm()
    {
        if (bgmSource == null || stageSelectBgmClip == null)
            return;

        // 진행 중인 페이드 코루틴이 있으면 정지
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        bgmSource.Stop();
        bgmSource.clip = stageSelectBgmClip;
        bgmSource.volume = bgmDefaultVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// 인게임 BGM을 페이드인하며 재생.
    /// </summary>
    private void PlayInGameBgmWithFade(float fadeDuration = 1.5f)
    {
        if (bgmSource == null || inGameBgmClip == null)
            return;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(FadeInBgm(inGameBgmClip, fadeDuration));
    }

    /// <summary>
    /// targetClip으로 BGM을 페이드인하며 교체 재생.
    /// - duration: 페이드인에 걸리는 시간(초)
    /// </summary>
    private IEnumerator FadeInBgm(AudioClip targetClip, float duration)
    {
        if (bgmSource == null || targetClip == null)
            yield break;

        bgmSource.Stop();
        bgmSource.clip = targetClip;
        bgmSource.volume = 0f;
        bgmSource.loop = true;
        bgmSource.Play();

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            bgmSource.volume = Mathf.Lerp(0f, bgmDefaultVolume, lerp);
            yield return null;
        }

        bgmSource.volume = bgmDefaultVolume;
        bgmFadeCoroutine = null;
    }

    #endregion

    /// <summary>
    /// 현재 스테이지 데이터를 BoardManager에 전달해서 보드를 생성한다.
    /// (실제 게임 시작 시점)
    /// </summary>
    public void ApplyCurrentStageToBoard()
    {
        if (board == null) return;

        StageData s = CurrentStage;
        if (s == null) return;

        // 보드 생성
        board.LoadStage(
            s.boardWidth,
            s.boardHeight,
            s.targetScore,
            s.maxMoves
        );

        // 스테이지 진입 시 인게임 BGM 페이드인
        PlayInGameBgmWithFade(1.5f);
    }

    /// <summary>
    /// 외부(UI 버튼)에서 호출되는 스테이지 선택 함수.
    /// SelectStage 버튼에서 이 함수를 호출해야 한다.
    /// </summary>
    public void SelectStage(int stageIndex)
    {
        if (stageDB == null || stageDB.stages == null || stageDB.stages.Length == 0)
            return;

        stageIndex = Mathf.Clamp(stageIndex, 0, stageDB.stages.Length - 1);
        currentStageIndex = stageIndex;

        ApplyCurrentStageToBoard();
    }

    /// <summary>
    /// 다음 스테이지가 존재하는지 여부.
    /// </summary>
    public bool HasNextStage()
    {
        if (stageDB == null || stageDB.stages == null) return false;
        return currentStageIndex < stageDB.stages.Length - 1;
    }

    /// <summary>
    /// 다음 스테이지로 이동하고 보드 재생성.
    /// (클리어 후 "다음" 버튼 등에서 호출)
    /// </summary>
    public void GoToNextStage()
    {
        if (!HasNextStage()) return;

        currentStageIndex++;
        ApplyCurrentStageToBoard();
    }
}
