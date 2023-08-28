using PixelCrushers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Garunnir;
public class CharactorSaver : Saver
{
    private Data m_data = new Data();
    [Serializable]
    public class Data
    {
        public Charactor charactor;
    }
    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        print("custom::"+s);
        var data = SaveSystem.Deserialize<Data>(s, m_data);
        if (data == null) return;
        m_data = data;
        SaveSystem.currentSavedGameData.charactors.Add(data.charactor);
        //ComponentUtility.SetComponentEnabled(componentToWatch, data.enabled);
    }

    public override string RecordData()
    {
        //var value = (componentToWatch != null) ? ComponentUtility.IsComponentEnabled(componentToWatch) : false;
        //m_data.enabled = value;
        return SaveSystem.Serialize(m_data);
    }
}
