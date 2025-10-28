using UnityEngine;
using System.Collections;

public class MonitorPlatform : MonoBehaviour
{
    [Header("Monitor Components")]
    public GameObject offStateSpriteObject; // 꺼진 상태를 나타내는 일반 Sprite GameObject
    public GameObject onStateSpineObject;   // 켜지는 애니메이션을 가진 Spine GameObject

    [Header("Mecanim Parameter Settings")]
    public string playerTag = "Player";
    public string turnOnTrigger = "TurnOn";

    private Animator monitorAnimator;
    private int triggerHash;
    private bool isRunning = false; // 애니메이션이 이미 실행되었는지 확인

    void Awake()
    {
        if (onStateSpineObject != null)
        {
            monitorAnimator = onStateSpineObject.GetComponent<Animator>();
        }
        if (monitorAnimator != null && !string.IsNullOrEmpty(turnOnTrigger))
        {
            triggerHash = Animator.StringToHash(turnOnTrigger);
        }
    }

    void Start()
    {
        // 시작 시 꺼진 상태로 설정 (Sprite 켜고 Spine 끄기)
        if (offStateSpriteObject != null) offStateSpriteObject.SetActive(true);
        if (onStateSpineObject != null) onStateSpineObject.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag) && !isRunning)
        {
            // 충돌한 Player GameObject를 코루틴에 넘겨줍니다.
            StartCoroutine(CoTurnOnMonitor(collision.gameObject));
            isRunning = true;
        }
    }

    IEnumerator CoTurnOnMonitor(GameObject player)
    {
        if (onStateSpineObject.activeSelf) yield break;

        // MovingPlatform2D 활성화/비활성화 로직 제거 - 움직임이 계속 유지되도록 함

        // 1. Rigidbody 간섭 및 낙하 방지를 위한 짧은 대기 (안정화)
        yield return null;

        // 2. ★★★ 핵심: 부모 관계를 On State Spine Object로 이전합니다. ★★★
        // 이 로직은 플레이어가 'offStateSpriteObject'의 자식이라고 가정합니다.
        if (player.transform.parent == offStateSpriteObject.transform)
        {
            // 부모를 새로운 Spine 오브젝트로 이전 (월드 위치 유지)
            player.transform.SetParent(onStateSpineObject.transform, true);
        }

        // 3. GameObject 교체: On Spine을 켜고 Off Sprite를 끕니다. (순서 중요)
        if (onStateSpineObject != null) onStateSpineObject.SetActive(true);
        if (offStateSpriteObject != null) offStateSpriteObject.SetActive(false);

        // 4. 애니메이션 실행
        if (monitorAnimator != null && triggerHash != 0)
        {
            monitorAnimator.SetTrigger(triggerHash);
        }

        // 애니메이션 재생 시간만큼 대기 (선택 사항)
        // yield return new WaitForSeconds(애니메이션 길이);
    }
}