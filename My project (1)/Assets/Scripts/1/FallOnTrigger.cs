using UnityEngine;

public class FallOnTrigger : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("실제 라바콘의 Spine Mecanim Animator 컴포넌트")]
    public Animator coneAnimator; // ★★★ 라바콘 본체(Cone_Obstacle)의 Animator를 연결

    [Header("Settings")]
    [Tooltip("Animator에서 설정한 '넘어짐' Trigger 파라미터 이름")]
    public string fallTriggerName = "Fall";

    [Tooltip("감지할 플레이어의 Tag")]
    public string playerTag = "Player";

    // 내부 상태 변수
    private int fallTriggerHash;
    private bool hasFallen = false; // 중복 실행 방지
    private Collider2D triggerCollider;

    void Awake()
    {
        // 1. Animator가 연결되었는지 확인
        if (coneAnimator == null)
        {
            Debug.LogError("Animator 컴포넌트가 연결되지 않았습니다!");
            enabled = false;
            return;
        }

        // 2. Trigger 이름을 Hash로 변환
        fallTriggerHash = Animator.StringToHash(fallTriggerName);

        // 3. 이 오브젝트의 Collider2D 확인
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            Debug.LogError("Collider2D가 없거나 Is Trigger가 체크되지 않았습니다.");
            enabled = false;
        }
    }

    // ★★★ 플레이어가 '감지 영역'에 들어왔을 때 ★★★
    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 이미 넘어졌거나, 들어온 대상이 플레이어가 아니면 무시
        if (hasFallen || !other.CompareTag(playerTag))
        {
            return;
        }

        // 2. Animator의 Trigger를 발동시켜 '넘어짐' 애니메이션 실행
        if (coneAnimator != null)
        {
            coneAnimator.SetTrigger(fallTriggerHash);
        }

        // 3. 중복 실행 방지
        hasFallen = true;

        // 4. 감지 영역(Trigger) 비활성화
        triggerCollider.enabled = false;
    }
}