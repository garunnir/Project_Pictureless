using Garunnir;
using PixelCrushers.DialogueSystem;
using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.Wrappers;
using PixelCrushers.DialogueSystem.Wrappers;
using UnityEngine.UI;

public class LuaInputManager : MonoBehaviour
{
    //편리하게 쓰고싶다
    //그러면서 일괄적으로 여기에 관리하고싶다
    //그러면서 원본을 변형하고 싶지 않다
    [SerializeField]BookModule bookModule;
    void OnEnable()
    {
        Lua.RegisterFunction(nameof(AddKeyWord), this, SymbolExtensions.GetMethodInfo(() => AddKeyWord(string.Empty,string.Empty)));
        Lua.RegisterFunction(nameof(OverwriteKeyWord), this, SymbolExtensions.GetMethodInfo(() => OverwriteKeyWord(string.Empty,string.Empty)));
        Lua.RegisterFunction(nameof(OpenCustomResponse), this, SymbolExtensions.GetMethodInfo(() => OpenCustomResponse(string.Empty)));
        Lua.RegisterFunction(nameof(BGChange), this, SymbolExtensions.GetMethodInfo(() => BGChange(string.Empty)));
        Lua.RegisterFunction(nameof(JumpToOtherConv), this, SymbolExtensions.GetMethodInfo(() => JumpToOtherConv(string.Empty)));
        

        //Lua.UnregisterFunction("dd");
    }
    private void OnDisable()
    {
        Lua.UnregisterFunction(nameof(AddKeyWord));
        Lua.UnregisterFunction(nameof(OverwriteKeyWord));
        Lua.UnregisterFunction(nameof(OpenCustomResponse));
        Lua.UnregisterFunction(nameof(BGChange));
        Lua.UnregisterFunction(nameof(JumpToOtherConv));
    }

    void AddKeyWord(string keyWord,string locTablekey)
    {
        string description = FindDescription(locTablekey);
        bookModule.AddKeyWord(keyWord, description,false);
        bookModule.RePaint();
    }
    void OverwriteKeyWord(string keyWord,string locTablekey)
    {
        string description = FindDescription(locTablekey);
        bookModule.AddKeyWord(keyWord, description,true);
        bookModule.RePaint();
    }
    string FindDescription(string locTablekey)
    {
        PixelCrushers.Wrappers.UILocalizationManager instance = PixelCrushers.Wrappers.UILocalizationManager.instance;
        return instance.GetLocText(locTablekey, instance.KeywordTable);
    }
    private void BGChange(string imgname)
    {
        RawImage img = GameManager.Instance.GetUIManager().GetBackground();
        img.texture = GameManager.Instance.GetResourceManager().GetBG(imgname);
        Garunnir.UIUtility.AdjustSize(GameManager.Instance.GetUIManager().GetUpperRect(), img.rectTransform, img.texture);
    }
    void OpenCustomResponse(string excute)
    {
        switch (excute)
        {
            case "Conversation":
                //캐릭터 목록을 소환
                Debug.Log("Cov");
                break;
            case "Investigate":
                break;
                default: 
                Debug.LogError(excute+" is Not Vaild. Please Check DialogueData");
                break;
        }
    }
    void JumpToOtherConv(string title)
    {
        DialogueManager.StopConversation();
        DialogueManager.StartConversation(title);
    }

}
