using UnityEngine;

/// <summary>
/// �÷��̾��� Dead2 �ִϸ��̼� ������ �����ӿ���
/// �ִϸ��̼� �̺�Ʈ�� ȣ��Ǵ� ��.
/// </summary>
public class PlayerDeathHook : MonoBehaviour
{
    [Tooltip("���� �ִ� GameOverController ����")]
    public GameOverController gameOver;

    void Reset()
    {
        // �����Ϳ��� ������Ʈ �߰� �� �ڵ� ���� �õ�(��� ����)
        if (!gameOver) gameOver = FindObjectOfType<GameOverController>();
    }

    /// <summary>
    /// Dead2 �ִϸ��̼� ������ �������� Animation Event�� ȣ���� �Լ���
    /// </summary>
    public void AE_OnDead2Finished()
    {
        if (gameOver) gameOver.TriggerGameOver();
        else Debug.LogWarning("[PlayerDeathHook] GameOverController�� ������� �ʾҽ��ϴ�.");
    }

    // Ȥ�� ���� �̸��� �̺�Ʈ�� �־��� ��� ȣȯ
    public void AE_OnDie2Finished() => AE_OnDead2Finished();
}
