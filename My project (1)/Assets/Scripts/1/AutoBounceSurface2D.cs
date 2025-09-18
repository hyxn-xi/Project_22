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
    [Range(0f, 1f)] public float fromAboveNormalY = 0.2f; // ���� ���� y�� �� �̻��̸� OK
    public float fromAboveBoundsSlack = 0.08f;            // �÷��̾� ���� ���麸�� �̸�ŭ ���� OK

    [Header("Spam guard")]
    public float rehitCooldown = 0.08f;

    [Header("Debug")]
    public bool verbose = false;

    readonly HashSet<Rigidbody2D> _cooldown = new HashSet<Rigidbody2D>();
    Collider2D _myCol;

    void Awake() { _myCol = GetComponent<Collider2D>(); }

    void OnCollisionEnter2D(Collision2D c) => TryBounce(c.collider, c);
    void OnCollisionStay2D(Collision2D c) => TryBounce(c.collider, c);   // ���� ���� ����
    void OnTriggerEnter2D(Collider2D other) => TryBounce(other, null);

    void TryBounce(Collider2D other, Collision2D col)
    {
        if (!other || !other.CompareTag(playerTag)) return;

        var rb = other.attachedRigidbody ? other.attachedRigidbody
                                         : other.GetComponentInParent<Rigidbody2D>();
        if (!rb || _cooldown.Contains(rb)) return;

        // ������ ��Ҵ��� �Ǵ�(�ʿ� ��)
        if (requireFromAbove && !IsFromAbove(other, col)) return;

        // ���� �ӵ� �ο�(���簡 �� ũ�� ����)
        var v = rb.velocity;
        if (v.y < bounceVelocity) v.y = bounceVelocity;
        rb.velocity = new Vector2(v.x, v.y);

        if (verbose) Debug.Log($"[AutoBounce] {other.name} -> v.y={rb.velocity.y}");

        StartCoroutine(Cooldown(rb));
    }

    bool IsFromAbove(Collider2D other, Collision2D col)
    {
        bool ok = false;

        // 1) ���� �������� ���� �Ǵ�
        if (col != null && col.contactCount > 0)
        {
            for (int i = 0; i < col.contactCount; i++)
            {
                if (col.GetContact(i).normal.y >= fromAboveNormalY) { ok = true; break; }
            }
        }

        // 2) �ٿ���� ���� �Ǵ�(�߹ٴ� ���̰� �� ���麸�� ����� ��?)
        if (!ok && _myCol)
        {
            float myTop = _myCol.bounds.max.y;
            if (other.bounds.min.y >= myTop - fromAboveBoundsSlack) ok = true;
        }

        if (verbose && !ok) Debug.Log("[AutoBounce] from-above ���� ������");
        return ok;
    }

    IEnumerator Cooldown(Rigidbody2D rb)
    {
        _cooldown.Add(rb);
        yield return new WaitForSeconds(rehitCooldown);
        _cooldown.Remove(rb);
    }
}
