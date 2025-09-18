using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DisappearingPlatform2D : MonoBehaviour
{
    [Header("Who can trigger")]
    public string playerTag = "Player";
    [Tooltip("������ ����� ���� ����")]
    public bool requireFromAbove = true;
    [Tooltip("���� ������ Y�� �� ������ ũ�� '������'�� ����")]
    [Range(0f, 1f)] public float fromAboveNormalY = 0.3f;
    [Tooltip("�÷��̾� ��(�ݶ��̴� minY)�� �÷��� top ���� �� ����ŭ ���� '������'�� ����")]
    public float fromAboveBoundsSlack = 0.02f;

    [Header("Timing")]
    public float vanishDelay = 0f;
    public float fadeDuration = 0.2f;

    [Header("Behaviour")]
    [Tooltip("����� �� ��� Collider2D ��Ȱ��ȭ")]
    public bool disableCollidersOnVanish = true;
    [Tooltip("MovingPlatform2D ���� �̵� ��ũ��Ʈ ��Ȱ��ȭ")]
    public bool disableMovingScript = true;
    [Tooltip("����� �� �ı�(Respawn ��� �Ұ�)")]
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
    Behaviour movingScript; // MovingPlatform2D �� ù ��° �����̺��

    void Awake()
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            // �÷����� �밳 Kinematic + ������ ����
            if (rb.bodyType == RigidbodyType2D.Kinematic)
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
        srs = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        // ���� ������Ʈ(�Ǵ� �θ�)�� �ִ� �̵� ��ũ��Ʈ ã�Ƽ� ĳ��
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

        // 1) ���� ����
        for (int i = 0; i < c.contactCount; i++)
        {
            if (c.GetContact(i).normal.y >= fromAboveNormalY)
                return true;
        }

        // 2) �ٿ��� ��(�߹ٴ��� �÷��� top���� ���� ��)
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
        // ù �������� ������ �� ����
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

        // �浹 ����
        if (disableCollidersOnVanish && cols != null)
            foreach (var c in cols) if (c) c.enabled = false;

        // ���̵� �ƿ�
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
            // ��������Ʈ�� ���ų� ��� �������
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

        // �浹 �ѱ�
        if (cols != null) foreach (var c in cols) if (c) c.enabled = true;

        // ���̵� ��
        float t2 = 0f;
        if (srs != null && srs.Length > 0)
        {
            var end = new List<Color>(srs.Length);
            for (int i = 0; i < srs.Length; i++) end.Add(srs[i].color);

            while (t2 < 0.2f) // ������ ���̵� ��(0.2��)
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
        triggered = false; // �ٽ� ������ �� ���������
    }
}
