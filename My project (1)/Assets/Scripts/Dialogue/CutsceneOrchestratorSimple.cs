using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class CutsceneOrchestratorSimple : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;
    public bool playOnStart = true;
    public bool pauseVideoOnDialogue = true;

    [Header("Dialogue Start")]
    [Tooltip("컷신 시작 후 이 시간이 지나면 대사를 시작합니다(초)")]
    public float delayBeforeDialogue = 5f;

    [Header("Dialogue (기존 스크립트 그대로 사용)")]
    public TypingDialougeSimple dialogue;   // ← 네가 이미 쓰는 컴포넌트

    [Header("Scene Transition")]
    public string nextSceneName = "";       // 비워두면 Build Settings의 다음 씬으로

    [Header("Optional UI")]
    [Tooltip("대사 시작 전까진 숨길 오브젝트(대사 패널 루트 등)")]
    public GameObject[] hideUntilDialogue;

    bool started;
    bool seenAnyDialogueUI;    // 한 번이라도 말풍선이 켜졌는지
    bool closingDetected;      // 말풍선이 다시 모두 꺼진 시점(=닫힘)

    void Awake()
    {
        // 컷신 시작 전엔 대사 스크립트를 잠시 비활성화 → 5초 뒤에 켜서 시작
        if (dialogue)
        {
            dialogue.enabled = false;
            if (dialogue.girlUI) dialogue.girlUI.SetActive(false);
            if (dialogue.dadUI) dialogue.dadUI.SetActive(false);
        }

        if (hideUntilDialogue != null)
            foreach (var go in hideUntilDialogue)
                if (go) go.SetActive(false);
    }

    void Start()
    {
        if (started) return;
        started = true;

        if (videoPlayer && playOnStart && !videoPlayer.isPlaying)
            videoPlayer.Play();

        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        // 1) 일정 시간 대기
        float t = 0f;
        while (t < delayBeforeDialogue) { t += Time.deltaTime; yield return null; }

        // 2) 필요하면 비디오 일시정지
        if (pauseVideoOnDialogue && videoPlayer) videoPlayer.Pause();

        // 3) 숨겨둔 UI 켜기
        if (hideUntilDialogue != null)
            foreach (var go in hideUntilDialogue)
                if (go) go.SetActive(true);

        // 4) 대사 시작(기존 스크립트 그대로)
        if (dialogue)
        {
            dialogue.enabled = true;  // Start()가 돌면서 자동 시작
            // 혹시 자동 시작이 꺼진 버전이면 직접 호출 가능:
            // dialogue.StartDialogue();
        }

        // 5) 대사가 “닫히는” 순간 감지: 한 번이라도 UI가 켜졌다가 둘 다 꺼지면 닫힘으로 판단
        while (!closingDetected)
        {
            if (dialogue)
            {
                bool girlOn = dialogue.girlUI && dialogue.girlUI.activeInHierarchy;
                bool dadOn = dialogue.dadUI && dialogue.dadUI.activeInHierarchy;

                if (girlOn || dadOn) seenAnyDialogueUI = true;
                if (seenAnyDialogueUI && !girlOn && !dadOn) closingDetected = true;
            }
            yield return null;
        }

        // 6) 씬 전환
        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartSceneTransition(nextSceneName);
            else
                SceneManager.LoadScene(nextSceneName);
            return;
        }

        // 비워져 있으면 Build Settings의 다음 인덱스로
        int cur = SceneManager.GetActiveScene().buildIndex;
        int count = SceneManager.sceneCountInBuildSettings;
        int next = Mathf.Clamp(cur + 1, 0, count - 1);
        string byName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(next));

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.StartSceneTransition(byName);
        else
            SceneManager.LoadScene(next);
    }
}
