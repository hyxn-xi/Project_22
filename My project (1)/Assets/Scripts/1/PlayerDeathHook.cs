using UnityEngine;

/// <summary>
/// 플레이어의 Dead2 애니메이션 마지막 프레임에서
/// 애니메이션 이벤트로 호출되는 훅.
/// </summary>
public class PlayerDeathHook : MonoBehaviour
{
    [Tooltip("씬에 있는 GameOverController 참조")]
    public GameOverController gameOver;

    void Reset()
    {
        // 에디터에서 컴포넌트 추가 시 자동 연결 시도(없어도 무방)
        if (!gameOver) gameOver = FindObjectOfType<GameOverController>();
    }

    /// <summary>
    /// Dead2 애니메이션 마지막 프레임의 Animation Event가 호출할 함수명
    /// </summary>
    public void AE_OnDead2Finished()
    {
        if (gameOver) gameOver.TriggerGameOver();
        else Debug.LogWarning("[PlayerDeathHook] GameOverController가 연결되지 않았습니다.");
    }

    // 혹시 기존 이름을 이벤트에 넣었을 경우 호환
    public void AE_OnDie2Finished() => AE_OnDead2Finished();
}
