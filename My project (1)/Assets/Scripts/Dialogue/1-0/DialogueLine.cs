using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea]
    public string text;
    public string speakerName; // "Girl" �Ǵ� "Dad"
    public bool isLeftSide;    // ����� (����)
}

