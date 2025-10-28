using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 이 파일은 외부에서 정의된 DialogueLine 구조체를 사용한다고 가정합니다.

public class TypingDialougeSimple : MonoBehaviour
{
    [Header("UI References")]
    public GameObject girlUI;
    public TMP_Text girlText;

    public GameObject dadUI;
    public TMP_Text dadText;

    [Header("Dialogue Data")]
    public List<DialogueLine> lines;
    private int currentLineIndex = 0;

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private bool dialogueEnded = false;
    private bool waitingForClose = false;

    private NPCInteraction interactionController;
    private bool ignoreInputFrame = false;

    // -------- Portrait (이식된 부분) --------
    [Header("Portrait (이식)")]
    public Image portraitFront;
    public Image portraitBack;
    public float portraitFade = 0.12f;
    public bool portraitPreserveAspect = true;

    Coroutine portraitCo;
    bool portraitReady = false;

    [SerializeField] private Sprite defaultPortrait;

    // NPCInteraction 없이 UI를 닫기 위한 필드 추가
    [Header("Dialogue Panel (for Direct Close)")]
    [Tooltip("전체 대화 UI를 감싸는 최상위 패널 (NPCInteraction 없을 시 사용)")]
    public GameObject dialogueGroupContainer;


    void Start()
    {
        InitPortrait();

        // ★★★ Start() 시점에 NPCInteraction이 없다면 대화를 자동 시작합니다. ★★★
        if (FindObjectOfType<NPCInteraction>() == null)
        {
            // UI를 수동으로 켭니다.
            if (dialogueGroupContainer != null) dialogueGroupContainer.SetActive(true);

            // 첫 대사 자동 시작
            if (lines != null && lines.Count > 0)
            {
                ShowNextLine();
            }
            else
            {
                Debug.LogWarning("Dialogue lines are empty. Cannot start dialogue automatically.");
            }
        }
        // NPCInteraction이 있다면, NPCInteraction이 F키 입력 후 StartNewDialogue()를 호출할 때까지 대기합니다.
    }

    void Update()
    {
        if (ignoreInputFrame)
        {
            ignoreInputFrame = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else if (dialogueEnded && waitingForClose)
            {
                // UI 닫기 로직 (EndDialogue가 이미 CloseDialogueUI를 호출함)
                if (interactionController != null)
                {
                    EndDialogue();
                }
                else
                {
                    CloseDialogueUI();
                }
                return;
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    public void StartNewDialogue(List<DialogueLine> newLines, Sprite startPortrait, NPCInteraction controller)
    {
        // ★★★ 안전장치: 대화 목록이 비어있으면 즉시 종료하고 시작 방지 ★★★
        if (newLines == null || newLines.Count == 0)
        {
            Debug.LogWarning("Dialogue list is empty. Cannot start dialogue.");
            CloseDialogueUI();
            return;
        }

        interactionController = controller;
        lines = newLines;
        currentLineIndex = 0;
        dialogueEnded = false;
        waitingForClose = false;

        // 초상화 GameObject 강제 활성화 
        if (portraitFront != null) portraitFront.gameObject.SetActive(true);
        if (portraitBack != null) portraitBack.gameObject.SetActive(true);

        if (startPortrait != null)
        {
            SetPortraitInstant(startPortrait);
        }
        else if (defaultPortrait != null)
        {
            SetPortraitInstant(defaultPortrait);
        }

        ignoreInputFrame = true;
        ShowNextLine();
    }

    public void SetTypingSpeed(float speed)
    {
        if (speed > 0f)
        {
            typingSpeed = speed;
        }
    }

    public void ShowNextLine()
    {
        // ★★★ 핵심: 대화 목록 끝에 도달했는지 확인 ★★★
        if (currentLineIndex >= lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = lines[currentLineIndex];

        // UI 초기화 (활성화/비활성화)
        girlUI.SetActive(false);
        dadUI.SetActive(false);

        // 초상화 교체 로직
        if (line.portrait != null)
        {
            CrossfadeToPortrait(line.portrait, portraitFade);
        }

        // 텍스트 선택 및 출력
        if (line.speakerName == "Girl")
        {
            girlUI.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(girlText, line.text));
        }
        else if (line.speakerName == "Dad")
        {
            dadUI.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(dadText, line.text));
        }

        currentLineIndex++;
    }

    IEnumerator TypeSentence(TMP_Text targetText, string sentence)
    {
        isTyping = true;
        targetText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            targetText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    void SkipTyping()
    {
        DialogueLine line = lines[currentLineIndex - 1];

        if (line.speakerName == "Girl")
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            girlText.text = line.text;
        }
        else if (line.speakerName == "Dad")
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dadText.text = line.text;
        }

        isTyping = false;
    }

    void EndDialogue()
    {
        dialogueEnded = true;
        waitingForClose = true;

        // F 키 입력 버퍼 비우기 (NPCInteraction 충돌 방지)
        Input.ResetInputAxes();

        // UI 닫기
        CloseDialogueUI();

        if (interactionController != null)
        {
            // NPCInteraction에게 상태 초기화 책임을 위임
            interactionController.EndInteraction();
        }

        Debug.Log("Dialogue ended. UI closed.");
    }

    // UI를 완전히 닫는 함수 (잔상 제거 로직)
    public void CloseDialogueUI()
    {
        // 1. 전체 Container 비활성화 
        if (dialogueGroupContainer != null) dialogueGroupContainer.SetActive(false);

        // 2. 개별 UI 비활성화
        if (girlUI != null) girlUI.SetActive(false);
        if (dadUI != null) dadUI.SetActive(false);

        // 3. Portrait 잔상 제거
        if (portraitFront != null) portraitFront.gameObject.SetActive(false);
        if (portraitBack != null) portraitBack.gameObject.SetActive(false);

        // 상태 초기화
        dialogueEnded = false;
        waitingForClose = false;
    }


    public bool IsDialogueFinished()
    {
        return dialogueEnded && waitingForClose;
    }

    // ------------------- Portrait Helpers -------------------

    void InitPortrait()
    {
        if (portraitReady) return;
        if (!portraitFront || !portraitBack) return;

        portraitFront.preserveAspect = portraitPreserveAspect;
        portraitBack.preserveAspect = portraitPreserveAspect;

        portraitFront.gameObject.SetActive(true);
        portraitBack.gameObject.SetActive(true);

        if (defaultPortrait != null) portraitFront.sprite = defaultPortrait;

        var cf = portraitFront.color; cf.a = 1f; portraitFront.color = cf;
        var cb = portraitBack.color; cb.a = 0f; portraitBack.color = cb;

        portraitReady = true;
    }

    void SetPortraitInstant(Sprite sprite)
    {
        if (!portraitReady || sprite == null) return;
        portraitFront.sprite = sprite;

        // Portrait Image의 GameObject를 활성화합니다.
        if (portraitFront.gameObject != null) portraitFront.gameObject.SetActive(true);

        var cb = portraitBack.color; cb.a = 0f; portraitBack.color = cb;
        var cf = portraitFront.color; cf.a = 1f; portraitFront.color = cf;
    }

    void CrossfadeToPortrait(Sprite sprite, float duration)
    {
        if (!portraitReady || sprite == null) return;
        if (portraitFront.sprite == sprite) return;

        if (portraitCo != null) StopCoroutine(portraitCo);

        // Crossfade 시작 시 Portrait GameObject가 켜져 있어야 합니다.
        if (portraitFront.gameObject != null) portraitFront.gameObject.SetActive(true);
        if (portraitBack.gameObject != null) portraitBack.gameObject.SetActive(true);

        portraitCo = StartCoroutine(CoPortrait(sprite, duration));
    }

    IEnumerator CoPortrait(Sprite next, float dur)
    {
        portraitBack.sprite = next;

        float t = 0f;
        Color cf = portraitFront.color;
        Color cb = portraitBack.color;
        cb.a = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);

            cf.a = 1f - a;
            cb.a = a;

            portraitFront.color = cf;
            portraitBack.color = cb;
            yield return null;
        }

        cf.a = 1f;
        portraitFront.color = cf;

        var tmp = portraitFront; portraitFront = portraitBack; portraitBack = tmp;

        cb.a = 0f; portraitBack.color = cb;
        portraitCo = null;
    }
}