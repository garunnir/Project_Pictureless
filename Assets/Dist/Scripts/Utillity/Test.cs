using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using System;
using Garunnir;

public class Test : MonoBehaviour
{
    [DllImport("Dll1")]
    public static extern double Sum(double a,double b);
    // Start is called before the first frame update

    void Start()
    {
        //SaveSystem.saveStarted += () => print("savstart");
        //SaveSystem.saveEnded += () =>
        //{
        //    print("saveend");
        //    SaveSystem.currentSavedGameData = new SavedGameData();
        //    SaveSystem.LoadFromSlot(0);
        //};
        //SaveSystem.loadStarted += () => print("loadStart");
        //SaveSystem.saveDataApplied += () =>
        //{
        //    SaveSystem.SaveToSlotImmediate(0);
        //    print(SaveSystem.currentSavedGameData.charactors[0].profiles.Find((x) => x.componentType == ComponentType.none).body.FindInner("Breast"));
        //};
        //SaveSystem.loadEnded += () =>
        //{

        //};

    }
    public void Btn_Update()
    {
        SaveSystem.currentSavedGameData.charactors[0].bodyCore.Update(null, new Garunnir.CharacterAppend.BodySystem.CoreEventArgs() { time = 5 });
    }
    public void Btn_Save()
    {
        SaveSystem.SaveToSlotImmediate(0);
    }
    public void Btn_load()
    {
        SaveSystem.currentSavedGameData = new SavedGameData();
        SaveSystem.LoadFromSlot(0);
    }
}
