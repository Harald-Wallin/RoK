using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [TextArea(2, 4)]
    public string english;

    [TextArea(2, 4)]
    public string swedish;
}
