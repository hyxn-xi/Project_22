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

    // ★★★ 추가: NPCInteraction 참조 필드 ★★★
    private NPCInteraction interactionController;

    // -------- Portrait (이식된 부분) --------
    [Header("Portrait (이식)")]
    public Image portraitFront;
    public Image portraitBack;
    public float portraitFade = 0.12f;
    public bool portraitPreserveAspect = true;

    Coroutine portraitCo;
    bool portraitReady = false;

    [SerializeField] private Sprite defaultPortrait;

    void Start()
    {
        InitPortrait();
        // NPCInteraction에서 StartNewDialogue를 호출할 때까지 대기합니다.
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            // ★★★ 대화가 이미 끝났고 닫히기를 기다리는 상태라면, 아무것도 하지 않고 NPCInteraction에 F를 넘깁니다. ★★★
            else if (dialogueEnded && waitingForClose)
            {
                return;
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    // ★★★ 1. 외부에서 새로운 대화 목록, 시작 초상화, 그리고 NPCInteraction 컨트롤러를 받습니다. ★★★
    public void StartNewDialogue(List<DialogueLine> newLines, Sprite startPortrait, NPCInteraction controller)
    {
        // 컨트롤러 참조 저장
        interactionController = controller;

        // 새로운 대화 목록으로 교체
        lines = newLines;

        // 상태 초기화
        currentLineIndex = 0;
        dialogueEnded = false;
        waitingForClose = false;

        // NPC에 따라 다른 초상화로 즉시 설정
        if (startPortrait != null)
        {
            SetPortraitInstant(startPortrait);
        }
        else if (defaultPortrait != null)
        {
            SetPortraitInstant(defaultPortrait);
        }

        // 대화 시작
        ShowNextLine();
    }

    // 외부에서 타이핑 속도를 강제로 설정할 수 있는 함수
    public void SetTypingSpeed(float speed)
    {
        if (speed > 0f)
        {
            typingSpeed = speed;
        }
    }


    public void ShowNextLine()
    {
        if (currentLineIndex >= lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = lines[currentLineIndex];

        // UI 초기화
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

    // ★★★ 2. 대화가 끝나면 직접 EndInteraction을 호출하여 UI를 닫습니다. ★★★
    void EndDialogue()
    {
        dialogueEnded = true;
        waitingForClose = true;

        if (interactionController != null)
        {
            // UI를 닫는 책임을 NPCInteraction에 넘깁니다.
            interactionController.EndInteraction();
        }

        Debug.Log("Dialogue ended. UI closed by TypingDialougeSimple.");
    }

    // 외부에서 대화가 완전히 끝났는지 확인할 수 있도록 상태를 반환합니다.
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
        if (!portraitReady || !sprite) return;
        portraitFront.sprite = sprite;

        var cb = portraitBack.color; cb.a = 0f; portraitBack.color = cb;
        var cf = portraitFront.color; cf.a = 1f; portraitFront.color = cf;
    }

    void CrossfadeToPortrait(Sprite sprite, float duration)
    {
        if (!portraitReady || !sprite) return;
        if (portraitFront.sprite == sprite) return;

        if (portraitCo != null) StopCoroutine(portraitCo);
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