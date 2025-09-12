// CameraPinnedBackground.cs (�ѹ� ����)
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class CameraPinnedBackground : MonoBehaviour
{
    public Camera cam;
    public float zOffset = 10f;
    public bool matchViewSize = true;
    public float extraMargin = 1.05f; // ȭ�麸�� ��¦ ũ��

    SpriteRenderer _sr;

    void OnEnable() { _sr = GetComponent<SpriteRenderer>(); }
    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        // ī�޶� ��ġ�� ��
        var p = cam.transform.position;
        transform.position = new Vector3(p.x, p.y, p.z + zOffset);

        // ȭ�� ũ�⿡ ���� ������
        if (matchViewSize && _sr && _sr.sprite)
        {
            float h = cam.orthographicSize * 2f * extraMargin;
            float w = h * cam.aspect;
            var s = _sr.sprite.bounds.size;
            if (s.x > 0.0001f && s.y > 0.0001f)
                transform.localScale = new Vector3(w / s.x, h / s.y, 1f);
        }
    }
}
