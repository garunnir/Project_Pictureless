using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName ="createNPCSO")]
public class NPCCharacterSO : ScriptableObject
{
    public SOContainer[] imgContainer;
}
[Serializable]
public class SOContainer
{
    public int id;
    public Texture2D texture;
}