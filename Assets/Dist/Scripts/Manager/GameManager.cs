using Garunnir.CharacterAppend.BodySystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Garunnir;
using PixelCrushers.Wrappers;
using System.IO;
using System.Linq;
using System.Text;
using PixelCrushers.DialogueSystem;

namespace Garunnir
{
    public enum Form
    {
        none,
        id, field, inner, prev, next,
        
        profile,firstName, outerAge, exp,nickName, gender, lastName,age

    }
    public enum Form0
    {
        none,
        image, text, bar,
        character,bodyparts,
    }
    public enum Gender
    {
        none,
        male,female
    }
    public class GameManager : Singleton<GameManager>
    {
        #region SO
        [SerializeField] TextTable localizeTable;
        [SerializeField] NPCCharacterSO characterSO;
        public TextTable GetLoTable() => localizeTable;
        #endregion
        #region DataConfig
        public const int createStart = 10000;//start id to created charactor
        private Dictionary<Type, string> TypeDic;
        public Dictionary<(Enum, Enum), string> FormStrDic { get; private set; }
        //public const string form_cha_name = "Name";
        //public const string form_cha_id = "Id";
        //public const string form_parts_field = "Field";
        //public const string form_parts_inner = "Inner";
        //public const string form_parts_prev = "prev";
        //public const string form_parts_next = "next";
        //public const string form_cha_profile_syntax_show  = "[show]";
        //public const string form_cha_profile_syntax_hide  = "[hide]";
        public static string charProfleImg { get; private set; }
        void DataConfig()
        {
            TypeDic = new Dictionary<Type, string>();
            TypeDic.Add(typeof(MechBody), "Bodyparts.Mech");
            TypeDic.Add(typeof(HumanoidBody), "Bodyparts.Human");

            FormStrDic = new Dictionary<(Enum, Enum), string>();
            FormStrDic.Add((Form0.character,Form.id), "Id");
            FormStrDic.Add((Form0.character,Form.field), "Field");
            FormStrDic.Add((Form0.bodyparts,Form.field), "Field");
            FormStrDic.Add((Form0.bodyparts,Form.inner), "Inner");
            FormStrDic.Add((Form0.bodyparts, Form.prev), "prev");
            FormStrDic.Add((Form0.bodyparts, Form.next), "next");
            FormStrDic.Add((Form0.image, Form.profile), "Image.Profile");
            FormStrDic.Add((Form0.text, Form.firstName), "Text.FirstName");
            FormStrDic.Add((Form0.text, Form.outerAge), "Text.OuterAge");
            FormStrDic.Add((Form0.bar, Form.exp), "Bar.Exp");
            FormStrDic.Add((Form0.none, Form.none), "Null");
            FormStrDic.Add((Form0.text, Form.nickName), "Text.NickName");
            FormStrDic.Add((Form0.text, Form.lastName), "Text.LastName");
            FormStrDic.Add((Form0.text, Form.gender), "Text.Gender");
            FormStrDic.Add((Form0.text, Form.age), "Text.Age");
            FormStrDic.Add((Form.gender, Gender.male),"Gender.Male");
            FormStrDic.Add((Form.gender, Gender.none),"Gender.None");
            FormStrDic.Add((Form.gender, Gender.female),"Gender.female");
            charProfleImg = Path.Combine(Application.persistentDataPath, "Char","CharProfile");
            Utillity.CheckFolderInPath(charProfleImg);
        }
        public string GetTypeDic(Type type)
        {
            return TypeDic[type];
        }
        public string GetFormDic(Enum form0, Enum form)
        {
            return FormStrDic[(form0,form)];
        }

        #endregion
#if UNITY_EDITOR
        #region SO
        private void UpdateSO()
        {
            UpdataLocTextTable();
        }
        void UpdataLocTextTable()
        {
            if (UILocalizationManager.instance.textTable != null)
            {
                localizeTable = UILocalizationManager.instance.textTable as TextTable;
            }
            AddTableField(Form0.none, Form.none, "없음");
            AddTableField(Form0.text, Form.firstName, "이름");
            AddTableField(Form0.text, Form.nickName, "별명");
            AddTableField(Form0.text, Form.outerAge, "외관나이");
            AddTableField(Form0.bar, Form.exp, "경험치");
            AddTableField(Form0.text, Form.lastName, "성");
            AddTableField(Form0.text, Form.age, "나이");
            AddTableField(Form0.text, Form.gender, "성별");
            AddTableField(Form.gender, Gender.male, "남");
            AddTableField(Form.gender, Gender.female, "여");
            AddTableField(Form.gender, Gender.none, "무성");
        }
        void AddTableField(Enum arg0,Enum arg,string value, string lang = "ko")
        {
            localizeTable.AddField(GetFormDic(arg0, arg));
            localizeTable.SetFieldTextForLanguage(GetFormDic(arg0, arg), lang, value);
        }
        #endregion
#endif
        #region Managers
        CharactorManager charactorManager;
        #endregion

        #region CacheData
        public List<Character>characters = new List<Character>();
        #endregion
        private void Init()
        {
            charactorManager = CharactorManager.Instance;
            charactorManager.transform.SetParent(transform);
            if (characterSO == null) Debug.LogError("CharSO not exist. check GM property");
            SetDontDistroy();

        }
        private void Awake()
        {
            Init();
            DataConfig();
#if UNITY_EDITOR
            UpdateSO();
#endif
            StarterInit();
            LoadChar();
            ResourceLoad();
        }
        private void StarterInit()
        {
            characters=charactorManager.CreateNPCs();
            foreach (Character character in characters)
            {
                if(character.img_profile==null)
                {
                    List<SOContainer> charaimg =characterSO.imgContainer.Where(x => x.id == character.id).ToList<SOContainer>();
                    if(charaimg.Count > 0)
                    {
                        character.img_profile = charaimg[0].texture;
                    }
                }
            }
        }
        private void LoadChar()
        {
            SaveSystem.LoadFromSlot(0);
        }
        private void ResourceLoad()
        {
            //로드시 시간이 걸리는 데이터들을 미리 로드한다.
            //캐릭터 프로필사진들을 로드한다.
            LoadCharPicture();
        }
        void LoadCharPicture()
        {
            if (characters.Count == 0) return;
            for (int i = 0; i < characters.Count; i++)
            {
                characters[i].img_profile = Utillity.LoadImage(GameManager.charProfleImg + i);
            }
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
        public static string CheckFolderInPath(string path)
        {
            path=path.Remove(path.LastIndexOf('\\'));
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            directoryInfo.Create();
            return path;
        }
        public static Texture2D LoadImage(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes != null)
            {
                //print("findByte");
                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                bool boolen = texture.LoadImage(bytes);
                if (boolen)
                {
                    //print("loadDone");
                    return texture;
                }
                else return null;
            }
            else { return null; }
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
        public static string TupleSigleConv(string a,bool b,object c)
        {
            if (c.ToString() == string.Empty) c = "Null";
            return string.Format("{0}={1}|{2}",a, b, c);
        }
        public static void TupleDicConv(string head, Dictionary<string, (bool, object)> tupledic)
        {
            stringBuilder.Append($"{head}/Dic:");
            foreach (var obj in tupledic)
            {
                stringBuilder.Append(TupleSigleConv(obj.Key, obj.Value.Item1, obj.Value.Item2));
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
            Utillity.stringBuilder.Append($"{lf}{GameManager.Instance.GetFormDic(Form0.character,Form.id)}:");
            Utillity.stringBuilder.Append(character.id);
            Utillity.stringBuilder.Append(lf);
            TupleDicConv(GameManager.Instance.GetFormDic(Form0.character,Form.field), character.field);
            Utillity.stringBuilder.Append(divider);
            Utillity.stringBuilder.Append(lf);
            character.bodyCore.GetJsonConvert();
            return Utillity.stringBuilder.ToString();
        }
    }
}

