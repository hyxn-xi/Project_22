using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class CollectiblePiece : MonoBehaviour
{
    [Header("Basic")]
    public string playerTag = "Player";
    public bool destroyAfterPickup = true;

    [Header("VFX/SFX (optional)")]
    public GameObject vfxPrefab;
    public AudioClip sfx;
    public float sfxVolume = 1f;

    [Header("Vanish Effect")]
    public float vanishDuration = 0.18f;   // 사라지는 연출 시간
    public bool scaleDown = true;          // 스케일 줄이기
    public bool fadeOut = true;            // 알파 페이드

    bool _consumed;
    Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true; // 트리거 권장
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;
        if (!other || !other.CompareTag(playerTag)) return;

        _consumed = true;
        if (_col) _col.enabled = false;

        // 1) 플레이어에게 "픽업 애니 한 번만" 신호 보내기
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null) pc.PlayPickupOnce();

        // 2) VFX / SFX
        if (vfxPrefab) Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        if (sfx) AudioSource.PlayClipAtPoint(sfx, transform.position, sfxVolume);

        // 3) 사라지는 연출
        StartCoroutine(CoVanish());
    }

    IEnumerator CoVanish()
    {
        var rends = GetComponentsInChildren<SpriteRenderer>();
        Vector3 startScale = transform.localScale;

        float t = 0f;
        while (t < vanishDuration)
        {
            float u = Mathf.Clamp01(t / vanishDuration);

            if (scaleDown) transform.localScale = Vector3.Lerp(startScale, Vector3.zero, u);

            if (fadeOut)
            {
                for (int i = 0; i < rends.Length; i++)
                {
                    var c = rends[i].color;
                    c.a = 1f - u;
                    rends[i].color = c;
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (destroyAfterPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
