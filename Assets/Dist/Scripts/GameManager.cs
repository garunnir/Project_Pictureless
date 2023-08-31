using Garunnir.CharacterAppend.BodySystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Garunnir;
using PixelCrushers.Wrappers;

public static class DataConfig
{
    public const int createStart = 10000;//start id to created charactor
    private static Dictionary<Type, string> TypeDic;
    public const string form_cha_name = "Name";
    public const string form_cha_id= "Id";
    public const string form_parts_field= "Field";
    public const string form_parts_inner = "Inner";
    public const string form_parts_prev = "prev";
    public const string form_parts_next = "next";


    static DataConfig()
    {
        TypeDic = new Dictionary<Type, string>();
        TypeDic.Add(typeof(MechBody), "Bodyparts.Mech");
        TypeDic.Add(typeof(HumanoidBody), "Bodyparts.Human");
    }
    public static void Init()
    {

    }
    public static string GetTypeDic(Type type)
    {
        return TypeDic[type];
    }
}
public class GameManager : Singleton<GameManager> 
{
    CharactorManager charactorManager;
    private void Awake()
    {
        charactorManager=CharactorManager.Instance;
        charactorManager.transform.SetParent(transform);
        LoadChar();
        //StarterInit();
    }
    private void StarterInit()
    {
        //처음 시작하기를 눌렀을때 시행됨.
        charactorManager.CreateNPCs();
    }
    private void LoadChar()
    {
        SaveSystem.LoadFromSlot(0);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
