using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Equipment",menuName = "GameDataAsset/Equipment")]
public class EquipSO : ScriptableObject
{
    //장비능력치
    [SerializeField] string equipName;
    [SerializeField,Range(1,3)] int range=1;
    [SerializeField] float damage;
    [Header("적합성")]
    [SerializeField] float strSuitability;
    [SerializeField] float conSuitability;
    [SerializeField] float dexSuitability;
    [SerializeField] float intSuitability;
    [SerializeField] float wisSuitability;
    [SerializeField] float chaSuitability;
    [SerializeField] AnimationCurve rangeSuitabilty;

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
        Keyframe[] frame=rangeSuitabilty.keys;
        for (int i = 0; i < frame.Length; i++)
        {
            Keyframe keyframe = rangeSuitabilty.keys[i];
            keyframe.value = Mathf.Clamp(keyframe.value, 0, 1);
            keyframe.time = Mathf.Clamp(keyframe.time, 0, 1);
            Debug.Log("/"+ keyframe.value);
            rangeSuitabilty.RemoveKey(i);
            rangeSuitabilty.AddKey(keyframe);
            Debug.Log(rangeSuitabilty.keys[i].value);

        }
    }
}
