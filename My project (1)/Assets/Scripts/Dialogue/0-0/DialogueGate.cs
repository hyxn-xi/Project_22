using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueGate : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanelRoot; // 전체 대화 UI 패널 (활성화/비활성화 대상)
    public TMP_Text textDisplay;        // 대사가 출력될 TMP_Text 컴포넌트

    [Header("Core Dialogue System")]
    // 이 필드는 이제 사용되지 않지만, 오류 방지를 위해 임시로 유지하거나 제거해야 합니다.
    // 이전 요청대로 'TypingDialogueSimple'에 의존하지 않는 단독 시스템으로 가정합니다.

    [Header("Dialogue Data")]
    public List<DialogueLine> currentLines;
    private int currentLineIndex = 0;

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;
    private Coroutine typingCo;
    private bool isTyping = false;

    private bool dialogueEnded = false;
    private NPCInteraction interactionController;
    private bool ignoreInputFrame = false;


    // NPCInteraction에서 호출되어 대화를 시작합니다.
    public void StartDialogue(List<DialogueLine> lines, NPCInteraction controller)
    {
        // 1. 상태 초기화 및 데이터 로드
        currentLines = lines;
        interactionController = controller;
        currentLineIndex = 0;
        dialogueEnded = false;

        // 2. UI 활성화
        if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(true);

        // 3. 첫 대화 시작
        ignoreInputFrame = true; // F키 입력 충돌 방지
        ShowNextLine();

        // (참고: 초상화 설정 및 초기화 로직은 이 함수에 추가되어야 합니다.)
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
                // 타이핑 중이면 스킵
                SkipTyping();
            }
            else if (dialogueEnded)
            {
                // 대화가 끝났으면 UI 닫기
                EndDialogue();
            }
            else
            {
                // 다음 대사로 이동
                ShowNextLine();
            }
        }
    }

    public void ShowNextLine()
    {
        if (currentLineIndex >= currentLines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentLines[currentLineIndex];

        // (여기서 화자 이름 표시/초상화 교체 로직이 실행되어야 합니다.)

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeSentence(textDisplay, line.text));

        currentLineIndex++;
    }

    IEnumerator TypeSentence(TMP_Text targetText, string sentence)
    {
        isTyping = true;
        targetText.text = "";

        // (화자 이름 및 텍스트 설정 로직)

        foreach (char letter in sentence.ToCharArray())
        {
            targetText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    void SkipTyping()
    {
        if (currentLines.Count == 0 || currentLineIndex == 0) return;

        // 현재 출력 중인 대사 (index - 1)의 전체 텍스트를 즉시 표시
        textDisplay.text = currentLines[currentLineIndex - 1].text;

        if (typingCo != null) StopCoroutine(typingCo);
        isTyping = false;
    }

    // 대화 종료 및 UI 비활성화
    public void EndDialogue()
    {
        dialogueEnded = true;

        // 1. UI 비활성화 (DialogueGate가 책임짐)
        if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(false);

        // 2. NPCInteraction 상태 초기화
        if (interactionController != null)
        {
            interactionController.EndInteraction();
        }
    }
}