using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourcePoolManager : SingletonClass<ResourcePoolManager>
{

    Dictionary<string, Dictionary<string,object>>_dataTable = new Dictionary<string, Dictionary<string, object>>();
    //리소스를 관리하여 초반에 미리 리소스를 한번에 불러오는 친구.
    void Awake()
    {
        LoadAll();
    }

    private void LoadAll()
    {
        Load<Sprite>();
        Load<DialougeContainer>();
        Load<Font>();
        Load<GameObject>();
    }

    public T GetObj<T>(string key)
    {
        string temstr = typeof(T).Name;
        if (_dataTable[temstr].ContainsKey(key))
        {
            T result = (T)_dataTable[temstr][key];
            if (result == null)
            {
                Debug.LogWarning("Found Key but it is Null");
            }
            return result;
        }
        else
        {
            Debug.LogWarning(key+":Is Not Exist Resource");
            return (T)_dataTable[temstr]["NULL"];
        }

    }
    void Load<T>()
    {



        switch (typeof(T).Name)
        {

            case nameof(Sprite):
                //스프라이트저장
                Dictionary<string, object> spriteDic =new Dictionary<string, object>();
                for (int i = 0; i < 2; i++)
                {
                    //여기안에 제이슨같은거 들어가야 일일이 입력하지 않아도 됨. 근데 제이슨드가도 어짜피 일일이 넣는건 같지안냐...? 활용도는 제이슨이 낫긴할거같긴함.
                }
                spriteDic.Add("kobayasi", Resources.Load<Sprite>("Img/1"));
                spriteDic.Add("NULL", Resources.Load<Sprite>("Img/null"));
                _dataTable.Add(nameof(Sprite), spriteDic);//최종등록
                break;

            case nameof(AudioClip):
                //

                break;
            case nameof(DialougeContainer):
                Dictionary<string, object> sODic = new Dictionary<string, object>();
                DialougeContainer[] dc=Resources.LoadAll<DialougeContainer>("SODialogue");
                foreach (DialougeContainer a in dc)
                {
                    sODic.Add(a.name, a);
                }
                sODic.Add("NULL", null);
                _dataTable.Add(nameof(DialougeContainer), sODic);//최종등록

                break;

            case nameof(Font):
                Dictionary<string, object> fontDic = new Dictionary<string, object>();
                fontDic.Add("KATURI", Resources.Load<Font>("UnLoadSource/Fonts/KATURI"));
                _dataTable.Add(nameof(Font), fontDic);
                break;

            case nameof(GameObject):
                Dictionary<string, object> prefabsDic = new Dictionary<string, object>();
                GameObject[] gameObjects=Resources.LoadAll<GameObject>("prefabs");
                foreach(GameObject a in gameObjects)
                {
                    prefabsDic.Add(a.name, a);
                }
                _dataTable.Add(nameof(GameObject), prefabsDic);
                break;


            default:
                Debug.LogWarning("Invaild LoadType");
                break;
        }
        //해당 리소스를 로드함과 동시에 주소를 달아준다.


    }
    void SavePath()
    {
        //경로를 여기에서 저장해준다.
        //ResourcePathContainer a = new ResourcePathContainer { name = "kobayasi", path = "Img/1"};//메모리 차지하므로 비효율적.
    }
}
//class ResourcePathContainer
//{
//    public string name;
//    public string path;
//}