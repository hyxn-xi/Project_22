using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Video;
using System; // Action 사용을 위해 필요

[DisallowMultipleComponent]
public class Scene001aIntroDirector_UsingTypingWithPopup : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;
    public bool playOnStart = true;
    public bool pauseVideoOnDialogue = true;

    [Header("Dialogue Start")]
    [Tooltip("컷신 시작 후 이 시간이 지나면 대사를 시작합니다(초)")]
    public float delayBeforeDialogue = 5f;

    [Header("Dialogue (기존 스크립트 그대로 사용)")]
    public TypingDialogueWithPopup dialogue;    // ← 네가 이미 쓰는 컴포넌트

    [Header("Scene Transition")]
    public string nextSceneName = "GameScene-1a";

    // ★★★ 추가된 필드 ★★★
    [Header("Stage Clear Flag")]
    [Tooltip("이 씬이 STAGE 1 클리어 데이터를 저장하는 최종 씬인지")]
    public bool isFinalStageClearScene = false;
    // ★★★ 추가된 필드 끝 ★★★

    [Header("Optional UI")]
    [Tooltip("대사 시작 전까진 숨길 오브젝트(대사 패널 루트 등)")]
    public GameObject[] hideUntilDialogue;

    [Header("Camera Focus")]
    public Camera cam;
    public Transform dadFocus;
    public Transform momFocus;
    public float zoomInTime = 0.6f;
    public float holdTime = 0.6f;
    public float zoomOutTime = 0.6f;
    [Tooltip("직교 카메라: 작을수록 더 가까움")]
    public float orthoZoomSize = 2.5f;
    [Tooltip("원근 카메라: 작을수록 더 가까움")]
    public float perspectiveFOV = 30f;

    [Header("Optional")]
    [Tooltip("연출 중 비활성화할 카메라 팔로우/Cinemachine 오브젝트")]
    public GameObject cameraFollowToDisable;


    // internal
    Vector3 camPos0; float ortho0; float fov0;
    bool followWasActive;

    // ... (Dialogue lines) ...
    [TextArea] public string line1 = "이제 저 아저씨는 괜찮은 것 같아. 색이 다시 돌아왔어.";
    [TextArea] public string line2 = "..이제는 저 아줌마를 도와줘야 될 것 같아. 마음이 너무 아파 보여.";
    public KeyCode advanceKey = KeyCode.F;      // 진행 키


    bool started;
    bool seenAnyDialogueUI;
    bool closingDetected;


    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("[IntroDirector] Camera가 필요합니다."); enabled = false; return; }

        camPos0 = cam.transform.position;
        if (cam.orthographic) ortho0 = cam.orthographicSize; else fov0 = cam.fieldOfView;

        if (!dialogue)
        {
            Debug.LogError("[IntroDirector] TypingDialogueWithPopup 참조가 필요합니다.");
            enabled = false; return;
        }

        // 1. Dialogue 컴포넌트 설정
        dialogue.lines = new string[] { line1, line2 };
        dialogue.nextKey = advanceKey;

        // 2. ★★★ 핵심 수정: Dialogue의 종료 시 호출할 콜백 함수 주입 ★★★
        // Dialogue는 이제 이 함수를 호출할 책임만 가집니다.
        dialogue.onEndSequence = HandleStageClearAndTransition;

        // 3. Dialogue 컴포넌트가 씬 전환을 실행하도록 nextSceneName 설정 (Director의 값 사용)
        dialogue.nextSceneName = nextSceneName;

        // 이 씬에서는 팝업/카메라기능을 쓰지 않도록
        dialogue.popupObject = null;
        dialogue.dimBackground = null;
        dialogue.cameraTransform = null;
        dialogue.cameraTarget = null;

        if (cameraFollowToDisable)
        {
            followWasActive = cameraFollowToDisable.activeSelf;
            cameraFollowToDisable.SetActive(false);
        }
    }

    // ★★★ Stage Clear 및 최종 씬 전환 로직 (Director가 담당) ★★★
    void HandleStageClearAndTransition()
    {
        // 1. STAGE 1 클리어 데이터 저장 (isFinalStageClearScene이 True일 때만 실행)
        if (isFinalStageClearScene)
        {
            PlayerPrefs.SetInt("STAGE1_CLEARED", 1);
            PlayerPrefs.Save();
            Debug.Log("[Director] STAGE 1 CLEARED data saved. Initiating final transition.");
        }
        else
        {
            Debug.Log("[Director] Not final clear scene. Skipping data save.");
        }

        // 2. 최종 목적지로 씬 전환 (Dialogue의 EndSequence()에 의해 실행됨)
        // Dialogue.EndSequence()가 이 함수 실행 후 nextSceneName으로 전환을 시도합니다.
    }
    // ★★★ Stage Clear 및 최종 씬 전환 로직 끝 ★★★


    void OnDisable()
    {
        if (cameraFollowToDisable)
            cameraFollowToDisable.SetActive(followWasActive);
    }

    void Start()
    {
        if (started) return;
        started = true;

        if (videoPlayer && playOnStart && !videoPlayer.isPlaying)
            videoPlayer.Play();

        // RunSequence() 코루틴 시작
        StartCoroutine(CoRun());
    }

    // 이 코루틴은 카메라 연출을 위해 유지합니다.
    IEnumerator CoRun()
    {
        // 1) 일정 시간 대기 (영상 재생 등)
        float t = 0f;
        while (t < delayBeforeDialogue) { t += Time.deltaTime; yield return null; }

        // 2) 필요하면 비디오 일시정지
        if (pauseVideoOnDialogue && videoPlayer) videoPlayer.Pause();

        // 3) 숨겨둔 UI 켜기
        if (hideUntilDialogue != null)
            foreach (var go in hideUntilDialogue)
                if (go) go.SetActive(true);

        // 4) 대사 시작 (TypingDialogueWithPopup의 Start()가 돌면서 자동 시작)
        if (dialogue) dialogue.enabled = true;

        // 5) 카메라 연출 수행
        // 1번째 줄 대기
        yield return StartCoroutine(WaitUntilTextFullyEquals(dialogue.dialogueText, line1));
        // 아빠 클로즈업
        if (dadFocus) yield return StartCoroutine(ZoomWithInterrupt(line1, dadFocus));

        // 2번째 줄 대기
        yield return StartCoroutine(WaitUntilTextFullyEquals(dialogue.dialogueText, line2));
        // 엄마 클로즈업
        if (momFocus) yield return StartCoroutine(ZoomWithInterrupt(line2, momFocus));

        // 이후 흐름: 사용자가 F로 마지막 줄을 넘기면
        // TypingDialogueWithPopup이 EndSequence()를 호출하고
        // onEndSequence (HandleStageClearAndTransition)를 실행합니다.

        // 마지막 대기
        while (dialogue.enabled)
        {
            yield return null; // dialogue 스크립트가 스스로 씬 전환을 수행할 때까지 대기
        }
    }


    IEnumerator WaitUntilTextFullyEquals(TMP_Text label, string target)
    {
        if (!label) yield break;
        // 사용자가 F로 스킵하든 타이핑이 끝나든, "정확히 같은 문자열"이 된 시점까지 대기
        while (label.text != target)
            yield return null;
    }

    IEnumerator ZoomWithInterrupt(string watchingLine, Transform focus)
    {
        if (!cam) yield break;

        // 준비
        Vector3 startPos = cam.transform.position;
        Vector3 targetPos = new Vector3(focus.position.x, focus.position.y, startPos.z);
        float fromSize = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        float toSize = cam.orthographic ? orthoZoomSize : perspectiveFOV;

        // IN
        float t = 0f;
        while (t < zoomInTime)
        {
            // 줄이 바뀌면 즉시 중단
            if (dialogue.dialogueText && dialogue.dialogueText.text != watchingLine)
                yield break;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / zoomInTime);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, a);
            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(fromSize, toSize, a);
            else cam.fieldOfView = Mathf.Lerp(fromSize, toSize, a);
            yield return null;
        }
        cam.transform.position = targetPos;
        if (cam.orthographic) cam.orthographicSize = toSize; else cam.fieldOfView = toSize;

        // HOLD
        float h = 0f;
        while (h < holdTime)
        {
            if (dialogue.dialogueText && dialogue.dialogueText.text != watchingLine)
                yield break;
            h += Time.deltaTime;
            yield return null;
        }

        // OUT
        t = 0f;
        while (t < zoomOutTime)
        {
            if (dialogue.dialogueText && dialogue.dialogueText.text != watchingLine)
                yield break;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / zoomOutTime);
            cam.transform.position = Vector3.Lerp(targetPos, camPos0, a);
            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(toSize, ortho0, a);
            else cam.fieldOfView = Mathf.Lerp(toSize, fov0, a);
            yield return null;
        }
        RestoreCamera();
    }

    void RestoreCamera()
    {
        if (!cam) return;
        cam.transform.position = camPos0;
        if (cam.orthographic) cam.orthographicSize = ortho0;
        else cam.fieldOfView = fov0;
    }
}