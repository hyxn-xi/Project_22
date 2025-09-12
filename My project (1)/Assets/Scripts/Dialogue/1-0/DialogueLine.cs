using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea]
    public string text;
    public string speakerName; // "Girl" 또는 "Dad"
    public bool isLeftSide;    // 연출용 (선택)
}

