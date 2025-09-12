using UnityEngine;

public class GoalPiece : MonoBehaviour
{
    [Header("Refs")]
    public StageClearSequence stageClearSequence;   // 인스펙터에서 할당(없으면 자동 탐색)
    public Transform player;                        // Player Transform(없으면 자동 탐색)

    [Header("Flow")]
    public string nextSceneName = "";
    public float holdAfterReveal = 3f;

    bool triggered;

    void Reset()
    {
        if (!stageClearSequence)
            stageClearSequence = FindFirstObjectByType<StageClearSequence>(FindObjectsInactive.Include);
        if (!player)
        {
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc) player = pc.transform;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (!player) player = other.transform;
        if (!stageClearSequence)
            stageClearSequence = FindFirstObjectByType<StageClearSequence>(FindObjectsInactive.Include);

        if (stageClearSequence)
        {
            // ★ 3개 인자 오버로드 사용
            stageClearSequence.Begin(player, nextSceneName, holdAfterReveal);
        }
        else
        {
            Debug.LogError("[GoalPiece] StageClearSequence가 씬에 없습니다.");
        }
    }
}
