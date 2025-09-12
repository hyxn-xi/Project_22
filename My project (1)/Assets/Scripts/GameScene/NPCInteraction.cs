using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("플레이어 Transform")]
    public Transform player;   // 씬의 Player 오브젝트 Transform

    [System.Serializable]
    public class InteractionZone
    {
        public string zoneName;             // 편의상 “Father”, “Mother”, “Son”
        public float minX;                  // 진입 최소 X 좌표
        public float maxX;                  // 진입 최대 X 좌표
        public GameObject interactionIcon;  // 머리 위에 띄울 아이콘
        public string dialogueSceneName;    // F키 눌렀을 때 전환할 씬 이름

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
    }

    private void Update()
    {
        float px = player.position.x;

        foreach (var z in zones)
        {
            bool nowInside = px >= z.minX && px <= z.maxX;

            // 진입
            if (!z.isPlayerInside && nowInside)
            {
                z.isPlayerInside = true;
                if (z.interactionIcon != null)
                    z.interactionIcon.SetActive(true);
            }
            // 이탈
            else if (z.isPlayerInside && !nowInside)
            {
                z.isPlayerInside = false;
                if (z.interactionIcon != null)
                    z.interactionIcon.SetActive(false);
            }

            // F 키 눌렀을 때 씬 전환 (페이드 포함)
            if (z.isPlayerInside && Input.GetKeyDown(KeyCode.F))
            {
                if (!string.IsNullOrEmpty(z.dialogueSceneName))
                {
                    TrySceneTransition(z.dialogueSceneName);
                }
            }
        }
    }

    private void TrySceneTransition(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartSceneTransition(sceneName);
        }
        else
        {
            Debug.LogWarning("⚠ SceneTransitionManager 인스턴스를 찾지 못함 — 페이드 없이 씬 전환");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
