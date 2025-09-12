using System.Collections;
using UnityEngine;

/// <summary>
/// RadialReveal�� �÷��̾� ��ġ/���൵�� �����ϴ� ����̹�.
/// - LateUpdate���� �� ������ �÷��̾� ��ġ�� �߽����� ����
/// - StartReveal() ȣ�� �� duration ���� Progress 0��1 �ִϸ��̼�
/// </summary>
public class PickupRevealDriver : MonoBehaviour
{
    [Header("References")]
    public RadialReveal radial;   // RadialReveal ������Ʈ
    public Transform player;      // �÷��̾� Transform
    public Camera worldCam;       // ����θ� Camera.main ���

    [Header("Animation")]
    public bool playOnEnable = false;
    public float duration = 0.8f;
    public AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Behavior")]
    public bool followPlayerEveryFrame = true; // �� ������ �߽��� �÷��̾�� ����

    Coroutine _co;

    Camera Cam => worldCam != null ? worldCam : Camera.main;

    void OnEnable()
    {
        if (playOnEnable) StartReveal();
    }

    void LateUpdate()
    {
        if (!radial || !player) return;

        // �� ������ �÷��̾� ��ġ �������� �߽� ����
        if (followPlayerEveryFrame)
        {
            radial.ConfigureForPlayer(Cam, player.position);
        }
    }

    /// <summary>
    /// ���൵ 0��1 ���� ����
    /// </summary>
    public void StartReveal()
    {
        if (!radial || !player) return;
        if (_co != null) StopCoroutine(_co);

        // ���� �������� �� �� �÷��̾� �������� ����
        radial.ConfigureForPlayer(Cam, player.position);
        radial.SetProgress(0f);

        _co = StartCoroutine(CoReveal());
    }

    IEnumerator CoReveal()
    {
        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            radial.SetProgress(progressCurve.Evaluate(p));

            // followPlayerEveryFrame=false �� �ּ��� �ִϸ��̼� �߿��� ����
            if (!followPlayerEveryFrame && player)
                radial.ConfigureForPlayer(Cam, player.position);

            yield return null;
        }

        radial.SetProgress(1f);
        _co = null;
    }
}
