
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[System.Serializable]
public class BodyPartSO : ScriptableObject
{
    public string partName = "NewPart";
    public float maxHp = 10f;
    public bool vital = false;

    // 계층 관계
    public List<BodyPartSO> children = new();
    public List<BodyPartSO> parent = new();
}


