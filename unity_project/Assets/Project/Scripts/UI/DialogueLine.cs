using System;
using UnityEngine;

[Serializable]
public struct DialogueLine
{
    public string speakerName;
    public SpeakerType speakerType;
    [TextArea(2, 5)]
    public string body;

    public DialogueLine(string name, SpeakerType type, string text)
    {
        speakerName = name;
        speakerType = type;
        body = text;
    }
}
