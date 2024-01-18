using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentCollection", menuName = "GameDataAsset/EquipmentCollection")]
public class EquipmentCollectionSO : ScriptableObject
{
    [Header("장비/무기")]
    [SerializeField] public WeaponSO[] equipment_weapon;
    [SerializeField] public EquipSO[] equipment_Equip;

    
}
