using Garunnir.CharacterAppend.BodySystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Garunnir;
using PixelCrushers.Wrappers;
using System.IO;
public class GameManager : Singleton<GameManager>
{
    #region GameManager.Instance

    public const int createStart = 10000;//start id to created charactor
    private static Dictionary<Type, string> TypeDic;
    public const string form_cha_name = "Name";
    public const string form_cha_id  = "Id";
    public const string form_parts_field = "Field";
    public const string form_parts_inner  = "Inner";
    public const string form_parts_prev  = "prev";
    public const string form_parts_next  = "next";
    //public const string form_cha_profile_syntax_show  = "[show]";
    //public const string form_cha_profile_syntax_hide  = "[hide]";
    public static string path_img_mainP { get; private set; }
    void DataConfig()
    {
        TypeDic = new Dictionary<Type, string>();
        TypeDic.Add(typeof(MechBody), "Bodyparts.Mech");
        TypeDic.Add(typeof(HumanoidBody), "Bodyparts.Human");
        path_img_mainP = Path.Combine(Application.persistentDataPath,"mainChara");
        //Debug.Log("DT:" + path_img_mainP);//??이거 안들어가면 인식 안됨 ㅋㅋ
    }
    public string GetTypeDic(Type type)
    {
        return TypeDic[type];
    }
    public static string CheckFolderInPath(string path)
    {
        string[] dirs = path.Split('/');
        string tmpstr = dirs[0];
        for (int i = 1; i < dirs.Length; i++)
        {
            if (!Directory.Exists(tmpstr))
            {
                Directory.CreateDirectory(tmpstr);
            }
            tmpstr += "/" + dirs[i];
        }
        return path;
    }
    #endregion
    #region Managers
    CharactorManager charactorManager;
    #endregion
    private void Awake()
    {
        DataConfig();
        charactorManager=CharactorManager.Instance;
        charactorManager.transform.SetParent(transform);
        LoadChar();
        SetDontDistroy();
        StarterInit();
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
