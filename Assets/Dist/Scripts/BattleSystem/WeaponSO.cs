using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "GameDataAsset/Equipment/Weapon")]
public class WeaponSO : EquipAsset
{
    [Header("기초 데미지")]
    [SerializeField] public float damage;
    [SerializeField] public AnimationCurve range;

    private void OnEnable()
    {
        //rangeSuitabilty.keys = new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 0) };
    }
    private void OnValidate()
    {
        KeyNormalize();
    }
    private void KeyNormalize()
    {
        Keyframe[] frame=range.keys;
        for (int i = 0; i < frame.Length; i++)
        {
            Keyframe keyframe = range.keys[i];
            keyframe.value = Mathf.Clamp(keyframe.value, 0, 1);
            keyframe.time = Mathf.Clamp(keyframe.time, 0, 1);
            Debug.Log("/"+ keyframe.value);
            range.RemoveKey(i);
            range.AddKey(keyframe);
            Debug.Log(range.keys[i].value);
        }
    }
}