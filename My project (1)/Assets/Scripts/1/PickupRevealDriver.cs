using System.Collections;
using UnityEngine;

/// <summary>
/// RadialReveal에 플레이어 위치/진행도를 공급하는 드라이버.
/// - LateUpdate에서 매 프레임 플레이어 위치를 중심으로 설정
/// - StartReveal() 호출 시 duration 동안 Progress 0→1 애니메이션
/// </summary>
public class PickupRevealDriver : MonoBehaviour
{
    [Header("References")]
    public RadialReveal radial;   // RadialReveal 컴포넌트
    public Transform player;      // 플레이어 Transform
    public Camera worldCam;       // 비워두면 Camera.main 사용

    [Header("Animation")]
    public bool playOnEnable = false;
    public float duration = 0.8f;
    public AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Behavior")]
    public bool followPlayerEveryFrame = true; // 매 프레임 중심을 플레이어로 갱신

    Coroutine _co;

    Camera Cam => worldCam != null ? worldCam : Camera.main;

    void OnEnable()
    {
        if (playOnEnable) StartReveal();
    }

    void LateUpdate()
    {
        if (!radial || !player) return;

        // 매 프레임 플레이어 위치 기준으로 중심 갱신
        if (followPlayerEveryFrame)
        {
            radial.ConfigureForPlayer(Cam, player.position);
        }
    }

    /// <summary>
    /// 진행도 0→1 연출 시작
    /// </summary>
    public void StartReveal()
    {
        if (!radial || !player) return;
        if (_co != null) StopCoroutine(_co);

        // 시작 시점에도 한 번 플레이어 기준으로 세팅
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

            // followPlayerEveryFrame=false 라도 최소한 애니메이션 중에는 갱신
            if (!followPlayerEveryFrame && player)
                radial.ConfigureForPlayer(Cam, player.position);

            yield return null;
        }

        radial.SetProgress(1f);
        _co = null;
    }
}
