using PixelCrushers.DialogueSystem;
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
        //Lua.UnregisterFunction("dd");
    }
    private void OnDisable()
    {
        Lua.UnregisterFunction(nameof(AddKeyWord));
    }

    void AddKeyWord(string keyWord,string description)
    {
        bookModule.AddKeyWord(keyWord, description);
        Debug.LogWarning("!");
    }
}
