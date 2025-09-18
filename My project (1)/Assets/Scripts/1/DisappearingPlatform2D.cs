using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DisappearingPlatform2D : MonoBehaviour
{
    [Header("Who can trigger")]
    public string playerTag = "Player";
    [Tooltip("위에서 밟았을 때만 반응")]
    public bool requireFromAbove = true;
    [Tooltip("접촉 법선의 Y가 이 값보다 크면 '위에서'로 인정")]
    [Range(0f, 1f)] public float fromAboveNormalY = 0.3f;
    [Tooltip("플레이어 발(콜라이더 minY)이 플랫폼 top 보다 이 값만큼 위면 '위에서'로 인정")]
    public float fromAboveBoundsSlack = 0.02f;

    [Header("Timing")]
    public float vanishDelay = 0f;
    public float fadeDuration = 0.2f;

    [Header("Behaviour")]
    [Tooltip("사라질 때 모든 Collider2D 비활성화")]
    public bool disableCollidersOnVanish = true;
    [Tooltip("MovingPlatform2D 같은 이동 스크립트 비활성화")]
    public bool disableMovingScript = true;
    [Tooltip("사라진 뒤 파괴(Respawn 사용 불가)")]
    public bool destroyOnVanish = false;

    [Header("Respawn (optional)")]
    public bool respawn = true;
    public float respawnDelay = 5f;

    [Header("Debug")]
    public bool verbose = false;

    // --- internal
    bool triggered = false;
    Collider2D[] cols;
    SpriteRenderer[] srs;
    Behaviour movingScript; // MovingPlatform2D 등 첫 번째 비헤이비어

    void Awake()
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            // 플랫폼은 대개 Kinematic + 보간이 좋음
            if (rb.bodyType == RigidbodyType2D.Kinematic)
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
        srs = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        // 같은 오브젝트(또는 부모)에 있는 이동 스크립트 찾아서 캐싱
        movingScript = GetComponent<MovingPlatform2D>();
        if (!movingScript) movingScript = GetComponentInParent<MovingPlatform2D>();
    }

    bool IsPlayer(Collider2D c)
    {
        if (!c) return false;
        if (c.CompareTag(playerTag)) return true;
        if (c.GetComponentInParent<PlayerController>() != null) return true;
        return false;
    }

    bool IsFromAbove(Collision2D c)
    {
        if (!requireFromAbove) return true;

        // 1) 접촉 법선
        for (int i = 0; i < c.contactCount; i++)
        {
            if (c.GetContact(i).normal.y >= fromAboveNormalY)
                return true;
        }

        // 2) 바운즈 비교(발바닥이 플랫폼 top보다 조금 위)
        var myTop = GetComponent<Collider2D>().bounds.max.y;
        var otherMin = c.collider.bounds.min.y;
        if (otherMin >= myTop - fromAboveBoundsSlack)
            return true;

        if (verbose) Debug.Log($"[Disappear] not from above: normal/bounds fail (otherMin={otherMin:F3}, myTop={myTop:F3})",
                               this);
        return false;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        TryTrigger(c);
    }
    void OnCollisionStay2D(Collision2D c)
    {
        // 첫 프레임을 놓쳤을 때 보정
        TryTrigger(c);
    }

    void TryTrigger(Collision2D c)
    {
        if (triggered) return;
        if (!IsPlayer(c.collider)) return;
        if (requireFromAbove && !IsFromAbove(c)) return;

        if (verbose) Debug.Log("[Disappear] TRIGGER", this);
        StartCoroutine(CoVanish());
    }

    IEnumerator CoVanish()
    {
        if (triggered) yield break;
        triggered = true;

        if (disableMovingScript && movingScript) movingScript.enabled = false;

        if (vanishDelay > 0f) yield return new WaitForSeconds(vanishDelay);

        // 충돌 끄기
        if (disableCollidersOnVanish && cols != null)
            foreach (var c in cols) if (c) c.enabled = false;

        // 페이드 아웃
        if (fadeDuration > 0f && srs != null && srs.Length > 0)
        {
            float t = 0f;
            var start = new List<Color>(srs.Length);
            for (int i = 0; i < srs.Length; i++) start.Add(srs[i].color);

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                for (int i = 0; i < srs.Length; i++)
                {
                    var c = start[i];
                    c.a = Mathf.Lerp(start[i].a, 0f, k);
                    srs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < srs.Length; i++)
            {
                var c = srs[i].color; c.a = 0f; srs[i].color = c;
            }
        }
        else
        {
            // 스프라이트가 없거나 즉시 사라지게
            if (srs != null) foreach (var sr in srs) { var c = sr.color; c.a = 0f; sr.color = c; }
        }

        if (destroyOnVanish)
        {
            Destroy(gameObject);
            yield break;
        }

        if (!respawn) yield break;

        // ---- Respawn ----
        yield return new WaitForSeconds(respawnDelay);

        // 충돌 켜기
        if (cols != null) foreach (var c in cols) if (c) c.enabled = true;

        // 페이드 인
        float t2 = 0f;
        if (srs != null && srs.Length > 0)
        {
            var end = new List<Color>(srs.Length);
            for (int i = 0; i < srs.Length; i++) end.Add(srs[i].color);

            while (t2 < 0.2f) // 간단한 페이드 인(0.2초)
            {
                t2 += Time.deltaTime;
                float k = Mathf.Clamp01(t2 / 0.2f);
                for (int i = 0; i < srs.Length; i++)
                {
                    var c = end[i];
                    c.a = Mathf.Lerp(0f, 1f, k);
                    srs[i].color = c;
                }
                yield return null;
            }
            for (int i = 0; i < srs.Length; i++)
            {
                var c = srs[i].color; c.a = 1f; srs[i].color = c;
            }
        }

        if (disableMovingScript && movingScript) movingScript.enabled = true;
        triggered = false; // 다시 밟으면 또 사라지도록
    }
}
