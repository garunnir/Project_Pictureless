using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Equip", menuName = "GameDataAsset/Equipment/Equip")]
public class EquipSO : EquipAsset
{
    [Header("기초 방어력")]
    [SerializeField] public float value;
}