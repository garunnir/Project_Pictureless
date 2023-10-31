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
using UnityEngine.XR;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace Garunnir
{
    public enum Form
    {
        none,
        id, field, inner, prev, next,

        profile, firstName, outerAge, exp, nickName, gender, lastName, age

    }
    public enum Form0
    {
        none,
        status,
        character, bodyparts,
    }
    public enum Gender
    {
        none,
        male, female
    }
    
    public class GameManager : Singleton<GameManager>
    {
        #region DataConfig
        public const int createStart = 10000;//start id to created charactor
        public const int PlayerID = 1;
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
            FormStrDic.Add((Form0.character, Form.id), "Id");
            FormStrDic.Add((Form0.character, Form.field), "Field");
            FormStrDic.Add((Form0.bodyparts, Form.field), "Field");
            FormStrDic.Add((Form0.bodyparts, Form.inner), "Inner");
            FormStrDic.Add((Form0.bodyparts, Form.prev), "prev");
            FormStrDic.Add((Form0.bodyparts, Form.next), "next");
            FormStrDic.Add((Form0.character, Form.profile), "Character.Profile");
            FormStrDic.Add((Form0.character, Form.firstName), "Character.FirstName");
            FormStrDic.Add((Form0.character, Form.outerAge), "Character.OuterAge");
            FormStrDic.Add((Form0.status, Form.exp), "Status.Exp");
            FormStrDic.Add((Form0.none, Form.none), "Null");
            FormStrDic.Add((Form0.character, Form.nickName), "Character.Profile");
            FormStrDic.Add((Form0.character, Form.lastName), "Character.LastName");
            FormStrDic.Add((Form0.character, Form.gender), "Character.Gender");
            FormStrDic.Add((Form0.character, Form.age), "Character.Age");
            FormStrDic.Add((Form.gender, Gender.male), "Gender.Male");
            FormStrDic.Add((Form.gender, Gender.none), "Gender.None");
            FormStrDic.Add((Form.gender, Gender.female), "Gender.female");
            charProfleImg = Utillity.CombinePath(Application.persistentDataPath, "Char", "CharProfile");
            Utillity.CheckFolderInPath(charProfleImg,false);
        }

        public string GetTypeDic(Type type)
        {
            return TypeDic[type];
        }
        public string GetFormDic(Enum form0, Enum form)
        {
            return FormStrDic[(form0, form)];
        }

        #endregion
        #region SO
#if UNITY_EDITOR
        [SerializeField] TextTable localizeTable;
        public TextTable GetLoTable() => localizeTable;
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
            AddTableField(Form0.character, Form.firstName, "이름");
            AddTableField(Form0.character, Form.nickName, "별명");
            AddTableField(Form0.character, Form.outerAge, "외관나이");
            AddTableField(Form0.status, Form.exp, "경험치");
            AddTableField(Form0.character, Form.lastName, "성");
            AddTableField(Form0.character, Form.age, "나이");
            AddTableField(Form0.character, Form.gender, "성별");
            AddTableField(Form.gender, Gender.male, "남");
            AddTableField(Form.gender, Gender.female, "여");
            AddTableField(Form.gender, Gender.none, "무성");
        }
        void AddTableField(Enum arg0, Enum arg, string value, string lang = "ko")
        {
            localizeTable.AddField(GetFormDic(arg0, arg));
            localizeTable.SetFieldTextForLanguage(GetFormDic(arg0, arg), lang, value);
        }
#endif
        #endregion
        #region Managers
        [SerializeField]CharacterManager _charactorManager;
        [SerializeField] DialogueSystemController _dialogueSystemController;
        [SerializeField]UIManager _uiManager;
        public UIManager GetUIManager() => _uiManager;
        #endregion

        #region CacheData
        public int GetCurLocID() => Localization.GetCurrentLanguageID(UILocalizationManager.instance.textTable);

#if !UNITY_EDITOR
        AndroidJavaClass ajc = new AndroidJavaClass("com.garunnir.File.FileController");
#endif
        private Dictionary<string,Texture2D> m_imgDic = new Dictionary<string, Texture2D>();
        public Texture2D GetImg(string key) 
        {
            if (!m_imgDic.ContainsKey(key))
            {
                Debug.LogError("cantfind:" + key);
                return null;
            }
            return m_imgDic[key]; 
        }
        List<string> tmpPathContainer = new List<string>();

        public static event UnityAction InitDone;
      
        #endregion
        private void Init()
        {
            _charactorManager.Init();
            _uiManager.Init();

            SetDontDistroy();
            InitDone?.Invoke();
        }
        private void Awake()
        {
            DataConfig();
            Debug.Log("StartGameManagerInit");
            ResourceLoadDoneEvent += ()=>FindObjectOfType<DialogueSystemTrigger>()?.OnUse();
            Init();
#if UNITY_EDITOR
            UpdateSO();
#endif
            StarterInit();
            LoadChar();
            ResourceLoad();
            Debug.Log("EndGameManagerInit");
        }
        private void StarterInit()
        {

        }
        private void LoadChar()
        {
            SaveSystem.LoadFromSlot(0);
        }
        public event Action ResourceLoadDoneEvent;
        bool isResourceLoadDone = false;
        void LoadAllImg()
        {
            InitCharImg(() => { LoadImgByDir(); isResourceLoadDone = true; print("Invoke"); ResourceLoadDoneEvent?.Invoke(); });
            print(tmpPathContainer.Count);
        }
        void LoadImgByDir()
        {
            //이미지를 경로로부터 가져온다.
            //경로를 어떻게?
            foreach (string key in tmpPathContainer)
            {
                string keyname = key.Replace(Application.persistentDataPath+"/", string.Empty);
#if !UNITY_EDITOR
                m_imgDic.Add(keyname, NativeGallery.LoadImageAtPath(key));
#else
                Texture2D tex =new Texture2D(8,8);
                if (!File.Exists(key))
                {
                    Debug.LogError("cantfind:" + key);
                }
                tex.LoadImage(File.ReadAllBytes(key));
                m_imgDic.Add(keyname, tex);
                Debug.LogWarning(keyname);
#endif
            }
        }

#if !UNITY_EDITOR
        void InitCharImg(Action done)
        {
            int totalTasks = 2; // 총 작업 수
            int completedTasks = 0; // 완료된 작업 수
            Action action= () => { 
                completedTasks++;
                Debug.LogWarning("com: "+completedTasks);
                if (completedTasks == totalTasks)
                {
                    Debug.Log("delete");
                    ajc.CallStatic<bool>("deleteDirectoryIn", "DCIM", "/_.PPResource");
                    done();
                }
            };
            ImgResourceInit("m_Background", action);
            ImgResourceInit("Character", action);
        }
        private void ImgResourceInit(string rpath,Action done)
        {
            //가지고 있던 이미지들을 전부 겔러리에 저장한다.
            //팝업이 뜨면 실패.
            //가지고 있는것이 있는지 먼저 판단
            //파일명부터 가져오자 경로를 긁어오는 메서드 있는지?
            Texture2D[] textures = Resources.LoadAll<Texture2D>("Img/" + rpath);
            //AndroidJavaClass ajc = new AndroidJavaClass("com.garunnir.File.FileController");
            string path = ajc.CallStatic<string>("GetFileEnvPath", "DCIM", "");
            //Debug.LogWarning(ajc.CallStatic<bool>("IsExistFileEnv", "DCIM", "/.PPResource/Background/portal.png"));
            print(ajc.CallStatic<bool>("CreateDirectoryIn", "DCIM", $"/.PPResource/{rpath}"));
            int tmpDone = textures.Length;
            bool check=false;
            foreach (Texture2D tex in textures)
            {
                tmpPathContainer.Add(path + $"/.PPResource/{rpath}/" + tex.name + ".png");

                if (ajc.CallStatic<bool>("IsExistFileEnv", "DCIM", $"/.PPResource/{rpath}/" + tex.name + ".png"))
                {
                    Debug.Log("already Exist: " + tex.name + ".png");
                    tmpDone--;
                    continue;
                }

                //File.WriteAllBytes(path+"/"+tex.name+ ".png", tex.GetRawTextureData());
                //if (textures.Last() != tex)
                //{

                //}
                //else
                //{

                //}
                check=true;
                NativeGallery.Permission permission =NativeGallery.SaveImageToGallery(tex, $".PPResource/{rpath}", tex.name + ".png", (x, y) =>
                {
                    print(y);//path
                    if (!x)
                    {
                        Debug.LogError("Save Failed");
                    }
                    tmpDone--;
                    ajc.CallStatic<bool>("MoveTo", "DCIM", $"/_.PPResource/{rpath}/" + tex.name + ".png", $"/.PPResource/{rpath}/" + tex.name + ".png");
                    if (tmpDone == 0)
                    {
                        done();
                    }
                });
                
            }
            //if (!File.Exists(path))
            //{
            //    Utillity.CheckFolderInPath(path);
            //}
           if (tmpDone == 0&&!check)
           done();

            print("end");
            //파일있는지 확인하고 없으면 그 경로에 파일을 복사해넣는다.
        }
#else
        async void InitCharImg(Action done)
        {
            List<Task> tasks = new List<Task>();

            // 비동기 작업들을 tasks 리스트에 추가
            tasks.Add(ImgResourceInit("Background"));
            tasks.Add(ImgResourceInit("Character"));

            // 모든 작업이 완료될 때까지 기다림
            await Task.WhenAll(tasks);

            done();
            Console.WriteLine("모든 작업이 완료되었습니다.");
        }
        async Task ImgResourceInit(string rpath)
        {
            //가지고 있던 이미지들을 전부 겔러리에 저장한다.
            //팝업이 뜨면 실패.
            //가지고 있는것이 있는지 먼저 판단
            //파일명부터 가져오자 경로를 긁어오는 메서드 있는지?
            Texture2D[] textures = Resources.LoadAll<Texture2D>("Img/" + rpath);
            string path =Utillity.CheckFolderInPath(Application.persistentDataPath + ("/Img/" + rpath));
            print("start");
            foreach (var item in textures)
            {
                await File.WriteAllBytesAsync(path +"/"+ item.name + ".png", Utillity.GetTextureBytesFromCopy(item));
                tmpPathContainer.Add(path + "/" + item.name + ".png");
            }
            print("end");
            //파일있는지 확인하고 없으면 그 경로에 파일을 복사해넣는다.
        }
#endif
        private void ResourceLoad()
        {
            LoadAllImg();
            //로드시 시간이 걸리는 데이터들을 미리 로드한다.
            //만들어진 캐릭터 프로필사진들을 로드한다.
            LoadCharPicture();
        }
        void LoadCharPicture()
        {
            if (_charactorManager.characters.Count == 0) return;
            var player = _charactorManager.characters.Find(x => x.Name == "Player");
            player.portrait = Utillity.LoadImage(GameManager.charProfleImg + player.id);
        }
        //private void Update()
        //{
        //    Debug.Log("!");
        //}
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
        public static string CombinePath(params string[] path)
        {
            string str=string.Empty;
            foreach (var item in path)
            {
                str += item;
                if(path.Last()!=item)
                str += "/";
            }
            return str;
        }
        public static string CheckFolderInPath(string path,bool isOnlyDir=true)
        {
            Debug.LogWarning(path);
            if (!isOnlyDir)
                path = Path.GetDirectoryName(path);
            if (!Directory.Exists(path))
            {
                Debug.Log("Create");
                Directory.CreateDirectory(path);
            }
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
        public static string TupleSigleConv(string a, bool b, object c)
        {
            if (c.ToString() == string.Empty) c = "Null";
            return string.Format("{0}={1}|{2}", a, b, c);
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
        public static string GetJsonConvert(Actor character)
        {
            Utillity.stringBuilder.Clear();
            Utillity.stringBuilder.Append($"{lf}{GameManager.Instance.GetFormDic(Form0.character, Form.id)}:");
            Utillity.stringBuilder.Append(character.id);
            Utillity.stringBuilder.Append(lf);
            //TupleDicConv(GameManager.Instance.GetFormDic(Form0.character, Form.field), character.fields);
            Utillity.stringBuilder.Append(divider);
            Utillity.stringBuilder.Append(lf);
            character.bodyCore.GetJsonConvert();
            return Utillity.stringBuilder.ToString();
        }
        //static Rect CalculateAbsoluteRect(RectTransform target)
        //{
        //    // 부모 Transform의 위치
        //    Vector3 parentPosition = target.parent.position;

        //    // Rect의 위치와 크기를 부모 위치에 더하여 절대 좌표를 계산
        //    float absoluteX = target.rect.x + parentPosition.x;
        //    float absoluteY = target.rect.y + parentPosition.y;
        //    float absoluteWidth = target.rect.width;
        //    float absoluteHeight = target.rect.height;

        //    // 계산된 절대 좌표로 Rect 생성
        //    Rect absoluteRect = new Rect(absoluteX, absoluteY, absoluteWidth, absoluteHeight);

        //    return absoluteRect;
        //}
        //public static void CopyDifParentRect(RectTransform source,RectTransform target)
        //{
        //    Rect rect=CalculateAbsoluteRect(source);
        //    Transform parent = target.parent;
        //    target.parent = null;
        //    target.ForceUpdateRectTransforms();
        //    target.rect.Set(0,0,rect.width,rect.height);
        //    target.parent = parent;
        //}

        public static byte[] GetTextureBytesFromCopy(Texture2D texture, bool isJpeg=false)
        {
            // Texture is marked as non-readable, create a readable copy and save it instead
            Debug.LogWarning("Saving non-readable textures is slower than saving readable textures");

            Texture2D sourceTexReadable = null;
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
            RenderTexture activeRT = RenderTexture.active;

            try
            {
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;

                sourceTexReadable = new Texture2D(texture.width, texture.height, isJpeg ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
                sourceTexReadable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0, false);
                sourceTexReadable.Apply(false, false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                UnityEngine.Object.DestroyImmediate(sourceTexReadable);
                return null;
            }
            finally
            {
                RenderTexture.active = activeRT;
                RenderTexture.ReleaseTemporary(rt);
            }

            try
            {
                return isJpeg ? sourceTexReadable.EncodeToJPG(100) : sourceTexReadable.EncodeToPNG();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceTexReadable);
            }
        }
    }
    static class Extentions
    {

    }
}

