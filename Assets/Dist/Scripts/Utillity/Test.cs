using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using System;
using Garunnir;
using TMPro;
using UnityEngine.EventSystems;

public class Test : MonoBehaviour,IPointerClickHandler
{
    TextMeshProUGUI m_TextMeshPro;
    Camera m_Camera;
    Canvas m_Canvas;

    void Start()
    {
        m_Camera = Camera.main;

        m_Canvas = gameObject.GetComponentInParent<Canvas>();
        if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            m_Camera = null;
        else
            m_Camera = m_Canvas.worldCamera;

        m_TextMeshPro = gameObject.GetComponent<TextMeshProUGUI>();
        m_TextMeshPro.ForceMeshUpdate();
        m_TextMeshPro.text = " <link=얌마짬마>Click here</link>";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, Input.mousePosition, m_Camera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
            print(linkInfo.GetLinkID());
        }
    }
    //[DllImport("Dll1")]
    //public static extern double Sum(double a,double b);
    // Start is called before the first frame update
    //TMP_Text text;
    //void Start()
    //{
    //    //Lua.RegisterFunction("dd", this, SymbolExtensions.GetMethodInfo(() => DD((int)0)));
    //    //Lua.UnregisterFunction("dd");
    //    //SaveSystem.saveStarted += () => print("savstart");
    //    //SaveSystem.saveEnded += () =>
    //    //{
    //    //    print("saveend");
    //    //    SaveSystem.currentSavedGameData = new SavedGameData();
    //    //    SaveSystem.LoadFromSlot(0);
    //    //};
    //    //SaveSystem.loadStarted += () => print("loadStart");
    //    //SaveSystem.saveDataApplied += () =>
    //    //{
    //    //    SaveSystem.SaveToSlotImmediate(0);
    //    //    print(SaveSystem.currentSavedGameData.charactors[0].profiles.Find((x) => x.componentType == ComponentType.none).body.FindInner("Breast"));
    //    //};
    //    //SaveSystem.loadEnded += () =>
    //    //{
    //    //};
    //}

    //void DD(int i)
    //{

    //}
    //public void Btn_Update()
    //{
    //    GameManager.Instance.characters[0].bodyCore.Update(null, new Garunnir.CharacterAppend.BodySystem.CoreEventArgs() { time = 5 });
    //}
    //public void Btn_Save()
    //{
    //    SaveSystem.SaveToSlotImmediate(0);
    //}
    //public void Btn_load()
    //{
    //    SaveSystem.currentSavedGameData = new SavedGameData();
    //    SaveSystem.LoadFromSlot(0);
    //}
}
