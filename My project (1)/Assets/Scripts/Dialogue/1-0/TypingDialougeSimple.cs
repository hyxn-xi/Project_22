using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    void Start()
    {
        StartDialogue();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (dialogueEnded && waitingForClose)
            {
                // 대사 UI 닫기
                girlUI.SetActive(false);
                dadUI.SetActive(false);
                waitingForClose = false;
                Debug.Log("Dialogue UI closed.");
            }
            else if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    public void StartDialogue()
    {
        currentLineIndex = 0;
        ShowNextLine();
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
        // 현재 대사 전체 출력하고 타이핑 종료
        DialogueLine line = lines[currentLineIndex - 1]; // 현재 출력 중인 대사

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
        Debug.Log("Dialogue ended. Press F to close.");
    }
}
