using UnityEngine;
using UnityEngine.SceneManagement;

public class SonSceneController : MonoBehaviour
{
    // ���� ��� '���ư���' ��ư Ŭ�� �� ���� ȭ�� ������ ��ȯ
    public void OnBackButtonClick()
    {
        SceneManager.LoadScene("MemoryScene");
    }

    public void OnRightButtonClick()
    {
        SceneManager.LoadScene("SonMemoryScene2");
    }

    public void OnRightButtonClick2()
    {
        SceneManager.LoadScene("SonMemoryScene3");
    }

    public void OnLeftButtonClick()
    {
        SceneManager.LoadScene("SonMemoryScene");
    }

    public void OnLeftButtonClick2()
    {
        SceneManager.LoadScene("SonMemoryScene2");
    }
}
