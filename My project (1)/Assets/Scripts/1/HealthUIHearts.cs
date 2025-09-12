using UnityEngine;
using UnityEngine.UI;

public class HealthUIHearts : MonoBehaviour
{
    [Header("Player")]
    public PlayerController player;          // 비우면 태그:Player에서 찾음
    public int maxHearts = 2;                // 하트 개수(기본 2)

    [Header("Sprites")]
    public Sprite fullHeart;                 // 꽉 찬 하트
    public Sprite emptyHeart;                // 빈 하트

    [Header("UI Images (drag 2)")]
    public Image[] heartImages;              // 하트 이미지 2개 드래그

    [Header("(Optional) 자동 생성")]
    public bool autoGenerate = false;        // 직접 드래그가 귀찮으면 사용
    public Transform container;              // Horizontal Layout 등이 붙은 부모
    public GameObject heartPrefab;           // Image 1개 들어있는 프리팹

    int lastShownHP = -999;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.GetComponent<PlayerController>();
        }

        if (autoGenerate && container && heartPrefab && (heartImages == null || heartImages.Length == 0))
        {
            heartImages = new Image[maxHearts];
            for (int i = 0; i < maxHearts; i++)
            {
                var go = Instantiate(heartPrefab, container);
                heartImages[i] = go.GetComponentInChildren<Image>();
            }
        }
    }

    void Start() => Refresh(true);

    void LateUpdate() => Refresh(false);

    void Refresh(bool force)
    {
        if (!player || heartImages == null || heartImages.Length == 0) return;

        int hp = Mathf.Clamp(player.HP, 0, maxHearts);
        if (!force && hp == lastShownHP) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (!heartImages[i]) continue;
            heartImages[i].sprite = (i < hp) ? fullHeart : emptyHeart;
            heartImages[i].enabled = true; // 혹시 비활성일 수 있어서
        }
        lastShownHP = hp;
    }
}
