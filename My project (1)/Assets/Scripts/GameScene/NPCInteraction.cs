using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NPCInteraction : MonoBehaviour
{
    [Header("플레이어 Transform")]
    public Transform player;   // 씬의 Player 오브젝트 Transform

    [Header("Dialogue System Reference")]
    public TypingDialougeSimple dialogueManager; // 대사 관리 스크립트 (필수 연결)
    public GameObject dialoguePanel;             // 대화 UI 전체 패널 오브젝트 (필수 연결)

    private bool isDialogueRunning = false; // 대화 중인지 상태를 추적 (F키 오동작 방지)

    [System.Serializable]
    public class InteractionZone
    {
        public string zoneName;
        public float minX;
        public float maxX;
        public GameObject interactionIcon;  // 머리 위에 띄울 아이콘

        [Header("Dialogue Setup")]
        public string initialSceneName; // 1-0, 2-0 등으로 초기 씬 전환 (클리어 안 했을 때)
        public List<DialogueLine> clearedDialogueLines; // 클리어 후 재생할 대화 목록
        public Sprite dialogueStartPortrait; // NPC가 활성화시킬 소녀의 초상화 Sprite

        [Header("Status")]
        public bool isCleared = false; // ★★★ NPC 클리어 상태 (상호작용 분기 기준) ★★★

        [HideInInspector]
        public bool isPlayerInside = false; // 내부 여부 체크
    }

    [Header("Interaction Zones 설정")]
    public InteractionZone[] zones;

    private void Start()
    {
        // 시작 시 모든 아이콘 숨기기
        foreach (var z in zones)
        {
            if (z.interactionIcon != null)
                z.interactionIcon.SetActive(false);
        }

        // 시작 시 대사 UI 숨기기
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        // 대화 중일 때는 다른 상호작용을 막고, F키로 대화 종료를 처리합니다.
        if (isDialogueRunning)
        {
            // ★★★ 수정: F 키를 통한 UI 닫기 로직 제거 ★★★
            // 이제 TypingDialougeSimple에서 EndDialogue()가 호출될 때 직접 EndInteraction()을 호출합니다.

            // IsDialogueFinished()가 true라면, 이는 이미 EndInteraction이 호출되었거나 호출될 예정임을 의미합니다.
            // 다른 상호작용을 막기 위해 return만 유지합니다.
            return;
        }

        float px = player.position.x;

        foreach (var z in zones)
        {
            bool nowInside = px >= z.minX && px <= z.maxX;

            // 진입/이탈 로직 유지
            if (!z.isPlayerInside && nowInside)
            {
                z.isPlayerInside = true;
                if (z.interactionIcon != null)
                    z.interactionIcon.SetActive(true);
            }
            else if (z.isPlayerInside && !nowInside)
            {
                z.isPlayerInside = false;
                if (z.interactionIcon != null)
                    z.interactionIcon.SetActive(false);
            }

            // F 키 눌렀을 때 상호작용 분기
            if (z.isPlayerInside && Input.GetKeyDown(KeyCode.F))
            {
                if (z.isCleared)
                {
                    // 클리어 상태일 때: NPC 고유 대화 목록 재생
                    StartDialogueInteraction(z);
                }
                else
                {
                    // 미클리어 상태일 때: 씬 전환
                    TrySceneTransition(z.initialSceneName);
                }
                break;
            }
        }
    }

    private void StartDialogueInteraction(InteractionZone z)
    {
        if (dialogueManager == null || dialoguePanel == null)
        {
            Debug.LogError("❌ Dialogue Manager 또는 Panel이 연결되지 않았습니다.");
            return;
        }

        // 1. 대화 상태 플래그 설정
        isDialogueRunning = true;

        // 2. 대화 UI 활성화
        dialoguePanel.SetActive(true);

        // 3. 대화 시작 (새로운 대화 목록 및 속도 설정)
        if (z.clearedDialogueLines != null && z.clearedDialogueLines.Count > 0)
        {
            // ★★★ TypingDialougeSimple에 'this' (자신)를 전달합니다. ★★★
            dialogueManager.StartNewDialogue(z.clearedDialogueLines, z.dialogueStartPortrait, this);

            // TypingDialougeSimple의 현재 설정된 속도를 가져와서 다시 설정합니다.
            if (dialogueManager.typingSpeed > 0)
            {
                dialogueManager.SetTypingSpeed(dialogueManager.typingSpeed);
            }
        }
        else
        {
            Debug.LogWarning(z.zoneName + " NPC는 클리어 후 대화 리스트가 비어 있어 상호작용을 종료합니다.");
            EndInteraction();
        }

        // 4. 상호작용 아이콘 숨기기
        if (z.interactionIcon != null) z.interactionIcon.SetActive(false);
    }

    // 대화 종료 함수 (UI 끄기)
    public void EndInteraction()
    {
        // 1. 대화 UI 비활성화
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // 2. 대화 상태 플래그 해제
        isDialogueRunning = false;

        Debug.Log("대화 종료 및 UI 닫힘.");
    }

    // 씬 전환 함수
    private void TrySceneTransition(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartSceneTransition(sceneName);
        }
        else
        {
            Debug.LogWarning("⚠ SceneTransitionManager 인스턴스를 찾지 못함 — 페이드 없이 씬 전환");
            SceneManager.LoadScene(sceneName);
        }
    }
}