using PixelCrushers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Garunnir;
using Garunnir.CharacterAppend.BodySystem;
using Garunnir.CharacterAppend.BodySystem.Inner.Organs;
using System.Reflection;

public class CharactorSaver : Saver
{
    private Data m_data = new Data();
    [Serializable]
    public class Data
    {
        public string name;
        public string convertData;
    }
    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        print("custom::"+s);
        var data = SaveSystem.Deserialize<Data>(s, m_data);
        if (data == null) return;
        m_data = data;
        Character tmpchar = new Character();
        tmpchar.name = name;

        tmpchar.bodyCore = BodyFactory.CreateDefault();

        //SaveSystem.currentSavedGameData.charactors.Add(data.charactor);
        //ComponentUtility.SetComponentEnabled(componentToWatch, data.enabled);
    }
    string SerializeBody(Core core)
    {
        //BodyParts bodytype;


        //string bodyType;
        //string innerType;
        //string prev;
        //string next;
        //string values;
        return core.GetJsonConvert();
    }
    public override string RecordData()
    {

        //var value = (componentToWatch != null) ? ComponentUtility.IsComponentEnabled(componentToWatch) : false;
        //m_data.enabled = value;
        m_data.convertData = SaveSystem.currentSavedGameData.charactors[0].bodyCore.GetJsonConvert();
        return SaveSystem.Serialize(m_data);
    }
}
