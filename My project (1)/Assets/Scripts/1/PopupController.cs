using UnityEngine;
using System.Collections;

public class PopupController : MonoBehaviour
{
    public GameObject popupImage; // Inspector에서 연결

    void Start()
    {
        StartCoroutine(HidePopupAfterSeconds(3f));
    }

    IEnumerator HidePopupAfterSeconds(float seconds)
    {
        popupImage.SetActive(true); // 혹시 꺼져있을 수도 있으니 켜줌
        yield return new WaitForSeconds(seconds);
        popupImage.SetActive(false);
    }
}
