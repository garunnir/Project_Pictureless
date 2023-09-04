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
            public List<string> characterData = new List<string>();
        }

        public override void ApplyData(string s)
        {
            if (string.IsNullOrEmpty(s)) return;

            var data = SaveSystem.Deserialize<Data>(s, m_data);
            if (data == null)
                m_data = data;
            print("char::"+data.characterData[0]);
            Character cha=SplitSolution(data.characterData[0]);
            print(cha.bodyCore.FindInner("Organ.Breast.1"));
            SaveSystem.currentSavedGameData.charactors.Add(cha);
            //SaveSystem.currentSavedGameData.charactors.Add(data.charactor);
            //ComponentUtility.SetComponentEnabled(componentToWatch, data.enabled);
        }
        BodyParts CreateBody(string st)
        {
            return null;
        }
        class SavedLink
        {
            public string[] innerlinks;
            public string[] prevlinks;
            public string[] nextlinks;
        }
        Character SplitSolution(string s)
        {
            Character tmpchar = null;
            Core core = null;
            int id = -1;
            string name = string.Empty;
            string[] tmpst= s.Split("<Inner>");
            string[] strings = tmpst[0].Split("==");
            Dictionary<string, SavedLink> link = new Dictionary<string, SavedLink>();
            Dictionary<string, BodyParts> partsDic=new Dictionary<string, BodyParts>();
            Dictionary<string,InnerParts>innerDic=new Dictionary<string, InnerParts>();
            string[] inn = tmpst[1].Split('\n');
            foreach (var item in inn)
            {
                string[] inner = item.Split(':');
                if (inner == null || inner.Length == 0) continue;

                if (inner[0].Contains("Organ.Breast"))
                {
                    Breast breast = new Breast(inner[0].Split('.').Last());
                    innerDic.Add(breast.name, breast);
                    string[] mems = inner[1].Split(',');
                    breast.FromJson(mems);

                }
                else if (inner[0].Contains("Organ.Womb"))
                {
                    Womb womb = new Womb(inner[0].Split('.').Last());
                    innerDic.Add(womb.name, womb);
                    string[] mems = inner[1].Split(',');
                    womb.FromJson(mems);
                }
            }
            for (int i = 0; i < strings.Length; i++)
            {
                BodyParts parts = null;
                SavedLink savedLink = new SavedLink();
                string[] innerstrings = strings[i].Split('\n');

                for (int j = 0; j < innerstrings.Length; j++)
                {
                    if (innerstrings[j].Contains('<')|| innerstrings[j]==string.Empty) continue;
                    if (i == 0)
                    {
                        if (tmpchar == null && name != string.Empty && id != -1)
                        {
                            tmpchar = new Character(name, id);
                        }
                        if (innerstrings[j].Contains(GameManager.form_cha_id))
                        {
                            id = int.Parse(innerstrings[j].Split(':')[1]);
                        }
                        else if (innerstrings[j].Contains(GameManager.form_cha_name))
                        {
                            name = innerstrings[j].Split(':')[1];
                        }

                        else if (innerstrings[j].Contains(GameManager.form_parts_field))
                        {
                            string[] tmpStrings = innerstrings[j].Split(':')[1].Split(',');
                            foreach (var item in tmpStrings)
                            {
                                string[] strs =item.Split('=');
                                string[] strrs = strs[1].Split("|");
                                tmpchar.SetField(strs[0], strrs[1], Utillity.ObjectParser<bool>(strrs[0]));
                            }
                        }
                    }
                    if (innerstrings[j].Contains(GameManager.Instance.GetTypeDic(typeof(HumanoidBody))))
                    {
                        string[] items = innerstrings[j].Split(':')[1].Split(',');
                        parts = new HumanoidBody(items[0],core);
                        partsDic.Add(items[0], parts);
                        core=parts.core;
                        parts.durability = int.Parse(items[1]);
                        link.Add(parts.name, savedLink);
                    }
                    else if (innerstrings[j].Contains(GameManager.Instance.GetTypeDic(typeof(MechBody))))
                    {
                        string[] items = innerstrings[j].Split(':')[1].Split(',');
                        parts = new MechBody(items[0], core);
                        partsDic.Add(items[0], parts);
                        core = parts.core;
                        parts.durability = int.Parse(items[1]);
                        link.Add(parts.name, savedLink);
                    }
                    if (parts == null)
                    {
                        //Debug.LogError("SaveFileError : Can't Find BodyData");
                    }
                    else if (innerstrings[j].Contains(GameManager.form_parts_field))
                    {
                        string[] items = innerstrings[j].Split(':')[1].Split(',');
                        foreach (string part in items)
                        {
                            string[] tmpstr = part.Split("=");
                            if (parts.field.ContainsKey(tmpstr[0]))
                            {
                                parts.field[tmpstr[0]] = float.Parse(tmpstr[1]);
                            }
                            else
                            {
                                parts.field.Add(tmpstr[0], float.Parse(tmpstr[1]));
                            }
                        }
                    }
                    else if (innerstrings[j].Contains(GameManager.form_parts_inner))
                    {
                        savedLink.innerlinks = innerstrings[j].Split(':')[1].Split(',');
                    }
                    else if (innerstrings[j].Contains(GameManager.form_parts_prev))
                    {
                        savedLink.prevlinks = innerstrings[j].Split(':')[1].Split(',');
                    }
                    else if (innerstrings[j].Contains(GameManager.form_parts_next))
                    {
                        savedLink.prevlinks = innerstrings[j].Split(':')[1].Split(',');
                    }
                }
            }
            if (tmpchar != null)
            {
                tmpchar.bodyCore = core;
                foreach (var item in partsDic.Values)
                {
                    SavedLink tmplink = link[item.name];
                    if (tmplink.innerlinks != null)
                        foreach (string v in tmplink.innerlinks)
                        {
                            if (v != string.Empty) 
                            {
                                item.innerParts.Add(innerDic[v]);
                                innerDic[v].Init(item);
                            } 
                        }
                    if (tmplink.prevlinks != null)

                        foreach (string v in tmplink.prevlinks)
                        {
                            if (v != string.Empty) item.prev.Add(partsDic[v]);
                        }
                    if (tmplink.nextlinks != null)

                        foreach (string v in tmplink.nextlinks)
                        {
                            if (v != string.Empty) item.next.Add(partsDic[v]);
                        }
                }
            }
            return tmpchar;
        }
        Character SubStringSolution(string s)
        {
            return null;
        }
        public override string RecordData()
        {
            //var value = (componentToWatch != null) ? ComponentUtility.IsComponentEnabled(componentToWatch) : false;
            //m_data.enabled = value;
            m_data=new Data();
            foreach (var item in SaveSystem.currentSavedGameData.charactors)
            {
                m_data.characterData.Add(Utillity.GetJsonConvert(item));
            }
            print("convert//" + m_data.characterData);
            return SaveSystem.Serialize(m_data);
        }
    }
    public class Utillity
    {
        //public const string lf = "\r\n";
        public const string divider = "==";
        public const string lf = "\n";
        public static StringBuilder stringBuilder = new StringBuilder();
        public static T ObjectParser<T>(string str)
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)bool.Parse(str);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.Parse(str);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(str);
            }
            else
            {
                return (T)(object)str;
            }
        }
        public static void ConvertToSaver(string head, params object[] objects)
        {
            stringBuilder.Append($"{head}:");
            foreach (object obj in objects)
            {
                stringBuilder.Append(obj);
                if (obj != objects.Last())
                    stringBuilder.Append(",");
            }

            stringBuilder.Append(lf);
            //Debug.Log("Convert: " + assemble);
        }
        public static void ListConverter<T>(string head, List<T> list) where T : Shape
        {
            if (list != null && list.Count > 0 && list[0] is Organ)
            {
                stringBuilder.Append($"{typeof(Organ).Name}.{head}/List:");
            }
            else
            {
                stringBuilder.Append($"{typeof(T).Name}.{head}/List:");
            }
            foreach (var obj in list)
            {
                stringBuilder.Append(obj.name);
                if (obj != list.Last())
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(lf);
        }
        public static void ListConverter(string head, List<string> list)
        {
            stringBuilder.Append($"{head}/List:");
            foreach (var obj in list)
            {
                stringBuilder.Append(obj);
                if (obj != list.Last())
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(lf);
        }
        public static void DicConverter(string head, Dictionary<string, float> dic)
        {
            stringBuilder.Append($"{head}/Dic:");
            foreach (var obj in dic)
            {
                stringBuilder.Append(obj.Key);
                stringBuilder.Append("=");
                stringBuilder.Append(obj.Value);
                if (obj.Key != dic.Last().Key)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append(lf);
        }
        public static void DicConverter(string head, Dictionary<string, object> dic)
        {
            stringBuilder.Append($"{head}/Dic:");
            foreach (var obj in dic)
            {
                stringBuilder.Append(obj.Key);
                stringBuilder.Append("=");
                stringBuilder.Append(obj.Value);
                if (obj.Key != dic.Last().Key)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append(lf);
        }
        public static void TupleDicConv(string head,Dictionary<string,(bool,object)>tupledic)
        {
            stringBuilder.Append($"{head}/Dic:");
            foreach (var obj in tupledic)
            {
                stringBuilder.Append(obj.Key);
                stringBuilder.Append("=");
                stringBuilder.Append(obj.Value.Item1);
                stringBuilder.Append('|');
                stringBuilder.Append(obj.Value.Item2);
                if (obj.Key != tupledic.Last().Key)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append(lf);
        }
        public static string GetJsonConvert(Character character)
        {
            Utillity.stringBuilder.Clear();
            Utillity.stringBuilder.Append($"{lf}{GameManager.form_cha_id}:");
            Utillity.stringBuilder.Append(character.id);
            Utillity.stringBuilder.Append($"{lf}{GameManager.form_cha_name}:");
            Utillity.stringBuilder.Append(character.name);
            Utillity.stringBuilder.Append(lf);
            TupleDicConv(GameManager.form_parts_field,character.GetFieldDic());
            Utillity.stringBuilder.Append(divider);
            Utillity.stringBuilder.Append(lf);
            character.bodyCore.GetJsonConvert();
            return Utillity.stringBuilder.ToString();
        }
    }
}


