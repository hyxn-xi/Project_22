using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AutoBounceSurface2D : MonoBehaviour
{
    [Header("Who")]
    public string playerTag = "Player";

    [Header("Bounce")]
    public float bounceVelocity = 15f;

    [Header("Require from above?")]
    public bool requireFromAbove = true;
    [Range(0f, 1f)] public float fromAboveNormalY = 0.2f; // 접촉 법선 y가 이 이상이면 OK
    public float fromAboveBoundsSlack = 0.08f;            // 플레이어 발이 윗면보다 이만큼 위면 OK

    [Header("Spam guard")]
    public float rehitCooldown = 0.08f;

    // ★★★ Bounce Animation 필드 추가 ★★★
    [Header("Bounce Animation")]
    public Animator bounceAnimator; // 짐볼 오브젝트의 Animator 컴포넌트
    public string bounceTriggerName = "Hit"; // Animator에 설정한 Trigger 이름 (예: Hit)
    // ★★★ Bounce Animation 필드 추가 끝 ★★★

    [Header("Debug")]
    public bool verbose = false;

    readonly HashSet<Rigidbody2D> _cooldown = new HashSet<Rigidbody2D>();
    Collider2D _myCol;

    // ★★★ Trigger Hash 캐싱용 변수 추가 ★★★
    private int _triggerHash;

    void Awake()
    {
        _myCol = GetComponent<Collider2D>();

        // ★★★ Awake에서 Trigger Hash 캐싱 ★★★
        if (bounceAnimator != null && !string.IsNullOrEmpty(bounceTriggerName))
        {
            _triggerHash = Animator.StringToHash(bounceTriggerName);
        }
    }

    void OnCollisionEnter2D(Collision2D c) => TryBounce(c.collider, c);
    void OnCollisionStay2D(Collision2D c) => TryBounce(c.collider, c);    // 느린 접촉 보강
    void OnTriggerEnter2D(Collider2D other) => TryBounce(other, null);

    void TryBounce(Collider2D other, Collision2D col)
    {
        if (!other || !other.CompareTag(playerTag)) return;

        var rb = other.attachedRigidbody ? other.attachedRigidbody
                                         : other.GetComponentInParent<Rigidbody2D>();
        if (!rb || _cooldown.Contains(rb)) return;

        // 위에서 밟았는지 판단(필요 시)
        if (requireFromAbove && !IsFromAbove(other, col)) return;

        // 위로 속도 부여(현재가 더 크면 유지)
        var v = rb.velocity;
        if (v.y < bounceVelocity) v.y = bounceVelocity;
        rb.velocity = new Vector2(v.x, v.y);

        // ★★★ 애니메이션 실행 로직 추가 ★★★
        if (bounceAnimator != null && _triggerHash != 0)
        {
            bounceAnimator.SetTrigger(_triggerHash);
        }
        // ★★★ 애니메이션 실행 로직 추가 끝 ★★★

        if (verbose) Debug.Log($"[AutoBounce] {other.name} -> v.y={rb.velocity.y}");

        StartCoroutine(Cooldown(rb));
    }

    bool IsFromAbove(Collider2D other, Collision2D col)
    {
        bool ok = false;

        // 1) 접촉 법선으로 먼저 판단
        if (col != null && col.contactCount > 0)
        {
            for (int i = 0; i < col.contactCount; i++)
            {
                if (col.GetContact(i).normal.y >= fromAboveNormalY) { ok = true; break; }
            }
        }

        // 2) 바운즈로 보조 판단(발바닥 높이가 내 윗면보다 충분히 위?)
        if (!ok && _myCol)
        {
            float myTop = _myCol.bounds.max.y;
            if (other.bounds.min.y >= myTop - fromAboveBoundsSlack) ok = true;
        }

        if (verbose && !ok) Debug.Log("[AutoBounce] from-above 조건 미충족");
        return ok;
    }

    IEnumerator Cooldown(Rigidbody2D rb)
    {
        _cooldown.Add(rb);
        yield return new WaitForSeconds(rehitCooldown);
        _cooldown.Remove(rb);
    }
}