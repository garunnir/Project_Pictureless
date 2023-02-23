using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class DialougeContainer : ScriptableObject
{
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
    public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
}
