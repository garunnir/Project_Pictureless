using PixelCrushers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Garunnir;
using Garunnir.CharacterAppend.BodySystem;
using Garunnir.CharacterAppend.BodySystem.Inner.Organs;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Garunnir
{
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
            //s=s.Replace("\\", "");
            s = s.Replace("\\n", "\n");
            print("custom::" + s);
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
            print("convert//" + m_data.convertData);
            return SaveSystem.Serialize(m_data);
        }
    }
    public class Utillity
    {
        //public const string lf = "\r\n";
        public const string lf = "\n";
        public static StringBuilder stringBuilder = new StringBuilder();
        public static void ConvertToSaver(string head, params object[] objects)
        {
            stringBuilder.Append($">{head}:{{");
            foreach (object obj in objects)
            {
                stringBuilder.Append(obj);
                stringBuilder.Append(",");
            }

            stringBuilder.Append("}");
            stringBuilder.Append(lf);
            //Debug.Log("Convert: " + assemble);
        }
        public static void ListConverter<T>(string head, List<T> list) where T : Shape
        {
            if (list!=null||list.Count>0||list[0] is Organ)
            {
                stringBuilder.Append($">{head}.{typeof(Organ).Name}/List:{{");
            }
            else
            {
                stringBuilder.Append($">{head}.{typeof(T).Name}/List:{{");
            }
            foreach (var obj in list)
            {
                stringBuilder.Append(obj.name);
                stringBuilder.Append(",");
            }
            stringBuilder.Append("}");
            stringBuilder.Append(lf);
        }
        public static void DicConverter(string head, Dictionary<string, float> dic)
        {
            stringBuilder.Append($">{head}/Dic:{{");
            foreach (var obj in dic)
            {
                stringBuilder.Append("{");
                stringBuilder.Append(obj.Key);
                stringBuilder.Append(",");
                stringBuilder.Append(obj.Value);
                stringBuilder.Append("}");
                if (obj.Key != dic.Last().Key)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append("}");
            stringBuilder.Append(lf);
        }
    }
}


