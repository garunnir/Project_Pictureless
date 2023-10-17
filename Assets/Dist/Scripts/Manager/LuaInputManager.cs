using Garunnir;
using PixelCrushers.DialogueSystem;
using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        //Lua.UnregisterFunction("dd");
    }
    private void OnDisable()
    {
        Lua.UnregisterFunction(nameof(AddKeyWord));
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
        int locid = GameManager.Instance.GetCurLocID();
        var field = UILocalizationManager.instance.additionalTextTables[0].GetField(locTablekey);
        if (field == null) return $"{Localization.language}: Not exist Localization Field Data";
        else return field.HasTextForLanguage(locid) ? field.GetTextForLanguage(locid) : $"{Localization.language}: Not exist Localization Text Data";
    }
}
