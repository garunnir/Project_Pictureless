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
        print(SaveSystem.currentSavedGameData.charactors[0].profiles.Find((x) => x.componentType == ComponentType.none).body.FindInner("Breast"));
        SaveSystem.saveStarted += () => print("savstart");
        SaveSystem.saveEnded += () =>
        {
            print("saveend");
            SaveSystem.currentSavedGameData = new SavedGameData();
            SaveSystem.LoadFromSlot(0);
        };
        SaveSystem.loadStarted += () => print("loadStart");
        SaveSystem.loadEnded += () => print(SaveSystem.currentSavedGameData.charactors[0].profiles.Find((x) => x.componentType == ComponentType.none).body.FindInner("Breast"));
        SaveSystem.saveDataApplied += () => print(SaveSystem.currentSavedGameData.charactors[0].profiles.Find((x) => x.componentType == ComponentType.none).body.FindInner("Breast"));
        SaveSystem.SaveToSlotImmediate(0);

    }

}
