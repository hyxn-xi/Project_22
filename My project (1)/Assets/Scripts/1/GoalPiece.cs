using UnityEngine;

public class GoalPiece : MonoBehaviour
{
    [Header("Refs")]
    public StageClearSequence stageClearSequence;   // �ν����Ϳ��� �Ҵ�(������ �ڵ� Ž��)
    public Transform player;                        // Player Transform(������ �ڵ� Ž��)

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
            // �� 3�� ���� �����ε� ���
            stageClearSequence.Begin(player, nextSceneName, holdAfterReveal);
        }
        else
        {
            Debug.LogError("[GoalPiece] StageClearSequence�� ���� �����ϴ�.");
        }
    }
}
