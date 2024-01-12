using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Equipment",menuName = "GameDataAsset/Equipment")]
public class EquipSO : ScriptableObject
{
    //장비능력치
    [SerializeField] string equipName;
    [SerializeField] float damage;
    [Header("적합성")]
    [SerializeField] float str;
    [SerializeField] float con;
    [SerializeField] float dex;
    [SerializeField] float @int;
    [SerializeField] float wis;
    [SerializeField] float cha;
    [SerializeField] AnimationCurve range;

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
