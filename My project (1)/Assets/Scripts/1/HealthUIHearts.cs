using UnityEngine;
using UnityEngine.UI;

public class HealthUIHearts : MonoBehaviour
{
    [Header("Player")]
    public PlayerController player;          // ���� �±�:Player���� ã��
    public int maxHearts = 2;                // ��Ʈ ����(�⺻ 2)

    [Header("Sprites")]
    public Sprite fullHeart;                 // �� �� ��Ʈ
    public Sprite emptyHeart;                // �� ��Ʈ

    [Header("UI Images (drag 2)")]
    public Image[] heartImages;              // ��Ʈ �̹��� 2�� �巡��

    [Header("(Optional) �ڵ� ����")]
    public bool autoGenerate = false;        // ���� �巡�װ� �������� ���
    public Transform container;              // Horizontal Layout ���� ���� �θ�
    public GameObject heartPrefab;           // Image 1�� ����ִ� ������

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
            heartImages[i].enabled = true; // Ȥ�� ��Ȱ���� �� �־
        }
        lastShownHP = hp;
    }
}
