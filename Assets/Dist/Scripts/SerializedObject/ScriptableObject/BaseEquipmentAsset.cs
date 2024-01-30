using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class EquipAsset : ScriptableObject
{
    //장비능력치
    [SerializeField] public string equipName;
    [Header("적합성 곱하기 배율")]
    [SerializeField] public float str;
    [SerializeField] public float con;
    [SerializeField] public float dex;
    [SerializeField] public float @int;
    [SerializeField] public float wis;
    [SerializeField] public float cha;
    [Header("장비무개 kg")]
    [SerializeField] public float weight;
}