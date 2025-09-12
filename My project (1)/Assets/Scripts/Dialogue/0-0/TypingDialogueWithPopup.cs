using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TypingDialogueWithPopup : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    [Header("Dialogue Data")]
    [TextArea(2, 6)]
    public string[] lines;

    [Header("Typing Settings")]
    public KeyCode nextKey = KeyCode.Space;
    public float typingSpeed = 0.05f;
    public float lineBreakDelay = 0.3f;

    [Header("Popup Settings")]
    public GameObject popupObject;
    public GameObject dimBackground;
    public int popupLineIndex = 3;
    public int popupCloseLineIndex = 4;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Transform cameraTarget;
    public float cameraMoveDuration = 2f;
    public int cameraMoveLineIndex = 2;

    [Header("Scene Transition")]
    public string nextSceneName = "1-1";

    // -------- Portrait (내장 크로스페이드) --------
    [Header("Portrait (optional, 두 장을 같은 위치로 겹치기)")]
    public Image portraitFront;        // 현재 보이는 Image
    public Image portraitBack;         // 다음 그림을 띄울 Image
    public Sprite[] linePortraits;     // 줄 번호에 맞춰 넣기(부족해도 OK)
    public float portraitFade = 0.12f; // 교체 속도
    public bool portraitUseNativeSize = false; // false면 오브젝트 크기 유지(추천)
    public bool portraitPreserveAspect = true; // 비율 유지

    int currentLine = 0;
    bool isTyping = false;
    Coroutine typingCoroutine;
    bool cameraMoved = false;
    bool isEnding = false;

    // portrait 내부 상태
    Coroutine portraitCo;
    bool portraitReady = false;

    void Start()
    {
        EnsureSceneTransitionManager();

        if (popupObject) popupObject.SetActive(false);
        if (dimBackground) dimBackground.SetActive(false);
        if (dialoguePanel) dialoguePanel.SetActive(true);

        InitPortrait();                 // 초상화 준비
        ApplyPortraitForLine(0, true);  // 첫 줄은 즉시 표시

        if (lines != null && lines.Length > 0)
            typingCoroutine = StartCoroutine(TypeLine(lines[0]));
    }

    void Update()
    {
        if (Input.GetKeyDown(nextKey))
        {
            if (isTyping)
            {
                StopCoroutine(typingCoroutine);
                dialogueText.text = lines[currentLine];
                isTyping = false;
            }
            else
            {
                if (currentLine == lines.Length - 1 && !isEnding)
                {
                    StartCoroutine(EndSequence());
                    isEnding = true;
                }
                else
                {
                    AdvanceLine();
                }
            }
        }
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            if (c == '\n') yield return new WaitForSeconds(lineBreakDelay);
            else yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    void AdvanceLine()
    {
        currentLine++;

        if (popupObject && currentLine == popupLineIndex)
        {
            popupObject.SetActive(true);
            if (dimBackground) dimBackground.SetActive(true);
        }
        if (popupObject && currentLine == popupCloseLineIndex)
        {
            popupObject.SetActive(false);
            if (dimBackground) dimBackground.SetActive(false);
        }

        if (!cameraMoved && currentLine == cameraMoveLineIndex && cameraTarget)
        {
            StartCoroutine(MoveCameraTo(cameraTransform, cameraTarget, cameraMoveDuration));
            cameraMoved = true;
        }

        // ★ 줄 바뀔 때 초상화 부드럽게 교체
        ApplyPortraitForLine(currentLine, false);

        if (currentLine < lines.Length)
            typingCoroutine = StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator MoveCameraTo(Transform mover, Transform target, float duration)
    {
        Vector3 startPos = mover.position;
        Quaternion startRot = mover.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            mover.position = Vector3.Lerp(startPos, target.position, smoothT);
            mover.rotation = Quaternion.Slerp(startRot, target.rotation, smoothT);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mover.position = target.position;
        mover.rotation = target.rotation;

        yield return new WaitForSeconds(0.1f);
        StartCoroutine(EndSequence());
    }

    IEnumerator EndSequence()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        yield return new WaitForSeconds(0.1f);

        EnsureSceneTransitionManager();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartSceneTransition(nextSceneName);
        }
        else
        {
            Debug.LogError("❌ SceneTransitionManager 인스턴스를 찾지 못함 — 씬 전환 실패");
        }
    }

    void EnsureSceneTransitionManager()
    {
        if (SceneTransitionManager.Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("SceneTransitionManager");
            if (prefab != null)
            {
                Instantiate(prefab);
                Debug.Log("✅ SceneTransitionManager 프리팹 인스턴스화 완료");
            }
            else
            {
                Debug.LogError("❌ Resources 폴더에서 SceneTransitionManager 프리팹을 찾지 못함");
            }
        }
    }

    // ------------- Portrait helpers (내장) -------------
    void InitPortrait()
    {
        if (portraitReady) return;
        if (!portraitFront || !portraitBack) return;

        portraitFront.preserveAspect = portraitPreserveAspect;
        portraitBack.preserveAspect = portraitPreserveAspect;

        // 항상 활성 상태 유지(깜빡임 방지), 알파로만 제어
        portraitFront.gameObject.SetActive(true);
        portraitBack.gameObject.SetActive(true);

        var cf = portraitFront.color; cf.a = 1f; portraitFront.color = cf;
        var cb = portraitBack.color; cb.a = 0f; portraitBack.color = cb;

        portraitReady = true;
    }

    void ApplyPortraitForLine(int lineIndex, bool instant)
    {
        if (!portraitReady) return;
        if (linePortraits == null) return;
        if (lineIndex < 0 || lineIndex >= linePortraits.Length) return;

        Sprite s = linePortraits[lineIndex];
        if (!s) return;

        if (instant) SetPortraitInstant(s);
        else CrossfadeToPortrait(s, portraitFade);
    }

    void SetPortraitInstant(Sprite sprite)
    {
        if (!portraitReady || !sprite) return;
        // front에 즉시 세팅, back은 투명 유지
        portraitFront.sprite = sprite;
        if (portraitUseNativeSize) portraitFront.SetNativeSize();

        var cb = portraitBack.color; cb.a = 0f; portraitBack.color = cb;
        var cf = portraitFront.color; cf.a = 1f; portraitFront.color = cf;
    }

    void CrossfadeToPortrait(Sprite sprite, float duration)
    {
        if (!portraitReady || !sprite) return;
        if (portraitCo != null) StopCoroutine(portraitCo);
        portraitCo = StartCoroutine(CoPortrait(sprite, duration));
    }

    IEnumerator CoPortrait(Sprite next, float dur)
    {
        // 다음 그림을 back에 준비(투명)
        portraitBack.sprite = next;
        if (portraitUseNativeSize) portraitBack.SetNativeSize();

        float t = 0f;
        Color cf = portraitFront.color;
        Color cb = portraitBack.color;
        cb.a = 0f; portraitBack.color = cb;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;          // 일시정지 중에도 자연스럽게
            float a = Mathf.Clamp01(t / dur);
            cf.a = 1f - a;
            cb.a = a;
            portraitFront.color = cf;
            portraitBack.color = cb;
            yield return null;
        }

        // 스왑
        var tmp = portraitFront; portraitFront = portraitBack; portraitBack = tmp;

        // 버퍼는 투명 초기화
        cb = portraitBack.color; cb.a = 0f; portraitBack.color = cb;
        portraitCo = null;
    }
}
