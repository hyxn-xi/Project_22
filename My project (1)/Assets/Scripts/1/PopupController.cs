using UnityEngine;
using System.Collections;

public class PopupController : MonoBehaviour
{
    public GameObject popupImage; // Inspector���� ����

    void Start()
    {
        StartCoroutine(HidePopupAfterSeconds(3f));
    }

    IEnumerator HidePopupAfterSeconds(float seconds)
    {
        popupImage.SetActive(true); // Ȥ�� �������� ���� ������ ����
        yield return new WaitForSeconds(seconds);
        popupImage.SetActive(false);
    }
}
