using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public class Scene001aIntroDirector_UsingTypingWithPopup : MonoBehaviour
{
    [Header("Dialogue (existing TypingDialogueWithPopup)")]
    public TypingDialogueWithPopup dialogue;  // 기존 스크립트
    [TextArea] public string line1 = "이제 저 아저씨는 괜찮은 것 같아. 색이 다시 돌아왔어.";
    [TextArea] public string line2 = "..이제는 저 아줌마를 도와줘야 될 것 같아. 마음이 너무 아파 보여.";
    public KeyCode advanceKey = KeyCode.F;    // 진행 키

    [Header("Scene Transition")]
    public string nextSceneName = "GameScene-1a";

    [Header("Camera Focus")]
    public Camera cam;                 // 비우면 Camera.main
    public Transform dadFocus;         // 아빠 클로즈업 포인트(빈 오브젝트)
    public Transform momFocus;         // 엄마 클로즈업 포인트(빈 오브젝트)
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

        // ★ TypingDialogueWithPopup이 Start에서 lines를 바로 읽으므로,
        //    Awake에서 먼저 주입해 두면 순서 문제 없이 동작합니다.
        dialogue.lines = new string[] { line1, line2 };
        dialogue.nextKey = advanceKey;
        dialogue.nextSceneName = nextSceneName;

        // 이 씬에서는 팝업/카메라기능을 쓰지 않도록(내부 MoveCameraTo는 EndSequence를 호출함)
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

    void OnDisable()
    {
        if (cameraFollowToDisable)
            cameraFollowToDisable.SetActive(followWasActive);
    }

    void Start()
    {
        // TypingDialogueWithPopup은 Start에서 자동으로 1번째 줄을 출력 시작합니다.
        // 우리는 텍스트가 "완전히" 표시되는 시점을 감지해 카메라 연출을 수행합니다.
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // 1) 첫 번째 줄이 완전히 표시될 때까지 대기
        yield return StartCoroutine(WaitUntilTextFullyEquals(dialogue.dialogueText, line1));
        // 아빠 클로즈업(도중에 사용자가 다음 줄로 넘기면 즉시 중단)
        if (dadFocus) yield return StartCoroutine(ZoomWithInterrupt(line1, dadFocus));

        // 2) 두 번째 줄이 완전히 표시될 때까지 대기
        yield return StartCoroutine(WaitUntilTextFullyEquals(dialogue.dialogueText, line2));
        // 엄마 클로즈업(마찬가지로 도중에 줄이 바뀌면 중단)
        if (momFocus) yield return StartCoroutine(ZoomWithInterrupt(line2, momFocus));

        // 이후 흐름: 사용자가 F로 마지막 줄을 넘기면
        // TypingDialogueWithPopup이 EndSequence()를 호출하고
        // nextSceneName("GameScene-1a")로 전환됩니다.
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
