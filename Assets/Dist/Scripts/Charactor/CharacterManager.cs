using Garunnir.CharacterAppend.BodySystem;
using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TextCore.Text;

namespace Garunnir
{
    [Serializable]
    public class Character
    {
        //ĳ���� ��ü����
        public string name;
        public int id;
        public Actor dialogueActor;
        public Core bodyCore;
        public Guid guid;
        public List<string>uiParam = new List<string>();
        private Dictionary<string,object> field=new Dictionary<string, object>();
        public Dictionary<string, object> GetFieldDic() => field;
        public Character(string name,int id)
        {
            guid= Guid.NewGuid();
            Debug.Log(guid);
            this.name = name;
            this.id = id;
        }
        public T GetField<T>(string key)
        {
            return (T)field[key];
        }
        public void SetField(string key, object value)
        {
            if (field.ContainsKey(key))
            {
                field[key] = value;
            }
            else field.Add(key, value);
        }
        public void DeleteField(string key)
        {
            if(field.ContainsKey(key)) field.Remove(key);
        }
        public void CreateDefault()
        {
            bodyCore = BodyFactory.CreateDefault();
        }

    }
    public class CharactorManager:Singleton<CharactorManager>
    {
        public List<Character> Characters = new List<Character>();
        private void Awake()
        {
            //SaveSystem.saveDataApplied += CreateNPCs;
        }
        void Garam()
        {
            Character cha = Characters.Find(x => x.id == 1);
            if (cha != null) return;
            cha=new Character("Garam",1);
            cha.CreateDefault();
            //cha.SetProfile<HumanProfile>(
            //    ("sprite", "Garam", ComponentType.img),
            //    ("name", "����", ComponentType.text),
            //    ("age", "15", ComponentType.text),
            //    ("exp", "34/50", ComponentType.bar)
            //�ڽ�
            //�θ�
            //    );
            Characters.Add(cha);
        }
        public void CreateNPCs()
        {
            if (SaveSystem.currentSavedGameData.charactors.Count!=0) return;

                Garam();

            SaveSystem.currentSavedGameData.charactors=Characters;
        }
        public void Init()
        {
            //ó�� �����Ҷ�
        }
    }
    public abstract class Profile
    {
        public string title;
        public string value;
        public Core body;
        public ComponentType componentType;
        public Profile(string title, string value, ComponentType type)
        {
            this.title = title;
            this.value = value;
            this.componentType = type;
        }
        public Profile(Core body)
        {
            this.body = body;
        }
        public Profile()
        {

        }
    }
    public class HumanProfile : Profile
    {
        public Actor father;
        public Actor mother;
        public HumanProfile()
        {

        }
        public HumanProfile(string title, string value, ComponentType type) : base(title, value, type)
        {
        }
        public HumanProfile(Core body) : base(body)
        {
            componentType = ComponentType.none;
        }
    }
    public class CustomField : Field
    {

    }




}
namespace Garunnir.CharacterAppend.BodySystem
{
    public class BodyFactory
    {
        //public abstract void build();
        public static Core CreateDefault()
        {
            HumanoidBody body = new HumanoidBody("head",null);
            BodyParts upperbody = body.SetNext("neck").SetNext<HumanoidBody>("upperBody", new Inner.Organs.Breast("1"));
            BodyParts lhand = upperbody.SetNext("leftshoulder").SetNext("leftupperarm").SetNext("leftelbow").SetNext("leftwrist").SetNext("lhand");
            BodyParts rhand = upperbody.SetNext("rightshoulder").SetNext("rightupperarm").SetNext("rightelbow").SetNext("rightwrist").SetNext("rhand");
            BodyParts pelvis = upperbody.SetNext("velly").SetNext<MechBody>("waist").SetNext<HumanoidBody>("pelvis", new Inner.Organs.Womb("1"));
            BodyParts rfoot = pelvis.SetNext("rupperleg").SetNext("rknee").SetNext("rfoot");
            BodyParts lfoot = pelvis.SetNext("lupperleg").SetNext("lknee").SetNext("lfoot");
            lhand.SetNext("lthumb", "lindexfinger", "lmiddlefinger", "lringfinger", "lpinky");
            rhand.SetNext("rthumb", "rindexfinger", "rmiddlefinger", "rringfinger", "rpinky");
            rfoot.SetNext("rbigtoe", "rsecondtoe", "rthirdtoe", "rforthtoe", "rlittletoe");
            lfoot.SetNext("lbigtoe", "lsecondtoe", "lthirdtoe", "lforthtoe", "llittletoe");

            //���� ������ü�� �����Ұ�
            //�㸮��ü�� �õ��Ѵ�
            //Debug.Log(body.Find("waist"));
            //BodyParts mpart= new MechBody("waist");
            //body.Find("waist").Swap(mpart);
            //BodyParts tmpb = body.Find("waist");
            //tmpb.GetField().ContainsKey("DiseaseRate");
            //Debug.Log("" + tmpb.GetField().ContainsKey("DiseaseRate") + tmpb.GetField().ContainsKey("EnergyRate") + tmpb.GetField().ContainsKey("DamageRate"));
            //��ü�ص� �̸��� ������ ���̹Ƿ� Ÿ���� �־ �ٲٴ� ����� ����غ��� �ɰͰ���.
            return body.core;
        }
    }


    public class CoreEventArgs : EventArgs
    {
        public int time { get; set; }
        public DateTime TimeReached { get; set; }
    }
    [Serializable]
    public class Core
    {
        public List<BodyParts> corelist = new List<BodyParts>();
        public List<BodyParts> partslist = new List<BodyParts>();
        private Dictionary<string, InnerParts> innerDic = new Dictionary<string, InnerParts>();
        public Dictionary<string, InnerParts> GetInner() => innerDic;
        public event EventHandler<CoreEventArgs> coreChanged;
        Guid guid;
        #region Method
        public Core(BodyParts parts)
        {
            guid = Guid.NewGuid();
            corelist.Add(parts);
            Debug.Log(guid);
        }
        private void Core_coreChanged(object sender, CoreEventArgs e)
        {
            //Debug.LogError("core subupdate" +instanceID);
            int orginTime = e.time;
            int atime = GetMinEventTime(e.time);
            e.time = atime;
            coreChanged?.Invoke(sender, e);
            if (atime != e.time)
            {
                e.time = orginTime - atime;
                coreChanged?.Invoke(sender, e);
            }
        }
        public void Update(object obj, CoreEventArgs e)
        {
            //Debug.LogError("coreupdate" + instanceID);
            Core_coreChanged(obj, e);
        }
        public void CoreCommend(string commend)//�ٸ� ������ �������
        {

        }
        public InnerParts FindInner(string name)
        {
            return innerDic[name];
        }

        public int GetMinEventTime(int time)
        {
            int min = int.MaxValue;
            foreach (var item in innerDic)
            {
                int newv = item.Value.CheckMinEventTime(time);
                if (newv < min)
                {
                    min = newv;
                }
            }
            if (int.MaxValue == min)
            {
                Debug.LogWarning("innerDic is empty");
                return time;
            }
            return min;
        }
        #endregion
        public string GetJsonConvert()
        {
            Utillity.stringBuilder.Append("<Core>");
            Utillity.stringBuilder.Append("</Core>");
            Utillity.stringBuilder.Append("<Parts>\n");
            foreach (var item in partslist)
            {
                item.ToJson();
            }
            Utillity.stringBuilder.Append("</Parts>");
            Utillity.stringBuilder.Append("<Inner>\n");
            foreach (var item in innerDic.Values)
            {
                item.ToJson();
            }
            Utillity.stringBuilder.Append("</Inner>");
            return Utillity.stringBuilder.ToString();
        }
    }
    public abstract class Shape
    {
        public string name { get { return _name; } protected set { Debug.Log(_name); _name = value; } }//������
        public string _name;
        abstract public void ToJson();
        abstract public void FromJson(string[] strings);
        //public void ObjectParser(ref bool obj,string str)
        //{
        //    obj = bool.Parse(str);
        //}
        //public void ObjectParser(ref float obj,string str)
        //{
        //    obj = float.Parse(str);
        //}
        //public void ObjectParser(ref int obj,string str)
        //{
        //    obj = int.Parse(str);
        //}        
        //public void ObjectParser(ref string obj,string str)
        //{
        //    obj = str;    
        //}
        public void ObjectParser<T>(ref T obj, string str)
        {
            if (obj is bool)
            {
                obj = (T)(object)bool.Parse(str);
            }
            else if (obj is float)
            {
                obj = (T)(object)float.Parse(str);
            }
            else if (obj is int)
            {
                obj = (T)(object)int.Parse(str);
            }
            else
            {
                obj = (T)(object)str;
            }
        }
    }
    public abstract class BodyParts : Shape
    {
        //public Action<int> update;
        public Core core { get; private set; }
        public Dictionary<string, float> field = new Dictionary<string, float>();
        public int durability;
        public List<BodyParts> next = new List<BodyParts>();
        public List<BodyParts> prev = new List<BodyParts>();
        public List<InnerParts> innerParts = new List<InnerParts>();

        #region Method
        public BodyParts()
        {

        }
        public BodyParts(Core core)
        {
            AddCore(core);
        }
        public BodyParts(string name, Core core) : this(core)
        {
            this.name = name;
        }
        //public BodyParts(string name) : this()
        //{
        //    this.name = name;
        //    if(core==null)core= new Core(this);
        //}
        public BodyParts(string name, InnerParts inner) : this()
        {
            innerParts.Add(inner);
        }
        void AddCore(Core core)
        {
            this.core = core ?? new Core(this);
            this.core.partslist.Add(this);
        }
        public T SetNext<T>(string name) where T : BodyParts, new()
        {
            T parts = new T();
            parts.name = name;
            parts.AddCore(this.core);
            next.Add(parts);
            parts.prev.Add(this);
            return parts;
        }
        public T SetNext<T>(string name, InnerParts inner) where T : BodyParts, new()
        {
            T parts = new T();
            parts.core = core;
            parts.name = name;
            parts.AddCore(this.core);
            parts.innerParts.Add(inner);
            inner.Init(parts);
            next.Add(parts);
            parts.prev.Add(this);

            return parts;
        }
        public abstract BodyParts SetNext(string name);
        public abstract BodyParts[] SetNext(params string[] name);
        //BodyParts parts= new BodyParts(name);
        //next.Add(parts);
        //parts.prev.Add(this);
        //return parts;
        //}
        public BodyParts SetLink(BodyParts parts)
        {
            next.Add(parts);
            parts.prev.Add(this);
            return parts;
        }
        public List<BodyParts> GetNext()
        {
            return next;
        }
        public List<BodyParts> GetPrev() => prev;
        public BodyParts Find(string name)
        {
            if (name == this.name)
            {
                return this;
            }
            else
            {
                BodyParts parts;
                //if(next.Count == 0)return null;
                foreach (var part in next)
                {
                    if (part == null)
                    {
                        next.Remove(part);
                    }
                    else
                    {
                        parts = part.Find(name);
                        if (parts != null) return parts;
                    }
                }
                return null;
            }
        }
        public void Swap(BodyParts parts)
        {
            parts.next = this.next;
            parts.prev = this.prev;
            foreach (var part in prev)//������� ����� �ֹ������̹Ƿ� ������ ��������� ������ �ٲ���� �Ѵ�.
            {
                part.next.Remove(this);
                part.next.Add(parts);
            }
            foreach (var part in next)
            {
                part.prev.Remove(this);
                part.prev.Add(parts);
            }
            next.Clear();
            prev.Clear();
        }
        public Dictionary<string, float> GetField()
        {
            return field;
        }
        #endregion
        public override void ToJson()
        {
            //string json="prev: {";
            //foreach(var part in prev)
            //{
            //    json += part.name;
            //    json += ",";
            //}
            //json += "}";
            //json += "next: {";
            //foreach (var part in next)
            //{
            //    json += part.name;
            //    json += ",";
            //}
            //json += "}";
            Utillity.ConvertToSaver(DataConfig.GetTypeDic(this.GetType()), name, durability);
            Utillity.DicConverter(DataConfig.form_parts_field, field);
            Utillity.ListConverter(DataConfig.form_parts_inner, innerParts);
            Utillity.ListConverter(DataConfig.form_parts_prev, prev);
            Utillity.ListConverter(DataConfig.form_parts_next, next);
            Utillity.stringBuilder.Append(Utillity.divider + Utillity.lf);
        }
        public override void FromJson(string[] strings)
        {
            throw new NotImplementedException();
        }
    }
    [Serializable]
    public class HumanoidBody : BodyParts
    {
        //public List<Organ> organs = new List<Organ>();
        #region constructor
        public HumanoidBody() : base()
        {
            field.Add("DiseaseRate", 0);
            field.Add("DamageRate", 0);
        }
        //start at head
        //public HumanoidBody(string name) : base(name)
        //{
        //    field.Add("DiseaseRate", 0);
        //    field.Add("DamageRate", 0);
        //}
        public HumanoidBody(string name, Core core) : base(name, core)
        {
            field.Add("DiseaseRate", 0);
            field.Add("DamageRate", 0);
        }
        //public HumanoidBody(string name,Organ organ) : this(name)
        //{
        //    SetOrgan(organ);
        //}
        #endregion
        #region method
        public override BodyParts SetNext(string name)
        {
            return SetNext<HumanoidBody>(name);
        }

        public override BodyParts[] SetNext(params string[] name)
        {
            BodyParts[] rtn = new BodyParts[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                HumanoidBody parts = new HumanoidBody(name[i],core);
                rtn[i] = parts;
                next.Add(parts);
                parts.prev.Add(this);
            }
            return rtn;
        }
        //public HumanoidBody SetNext(string name,Organ organ) 
        //{
        //    HumanoidBody parts = new HumanoidBody(name, organ);
        //    next.Add(parts);
        //    parts.prev.Add(this);
        //    return parts;
        //}
        //public void SetOrgan(Organ organ)
        //{
        //    organs.Add(organ);
        //}
        #endregion
        //public override void ToJson()
        //{
        //    Utillity.ConvertToSaver(DataConfig.GetTypeDic(this.GetType()), name, durability);
        //    base.ToJson();
        //}
    }
    [Serializable]
    public class MechBody : BodyParts
    {
        #region Constructor
        public MechBody() : base()
        {
            field.Add("EnergyRate", 1);        
            field.Add("DamageRate", 0);
        }
        //public MechBody(string name) : base(name)
        //{
        //    field.Add("EnergyRate", 1);
        //    field.Add("DamageRate", 0);
        //}

        public MechBody(string name, Core core) : base(name, core)
        {
            field.Add("EnergyRate", 1);
            field.Add("DamageRate", 0);
        }
        #endregion
        #region Method
        public override BodyParts SetNext(string name)
        {
            BodyParts parts = new HumanoidBody(name,core);
            next.Add(parts);
            parts.prev.Add(this);
            return parts;
        }

        public override BodyParts[] SetNext(params string[] name)
        {
            throw new System.NotImplementedException();
        }
        #endregion
        //public override void ToJson()
        //{
        //    Utillity.ConvertToSaver("BodyParts.Human", name, durability);
        //    base.ToJson();
        //}
    }

    public abstract class InnerParts : Shape
    {
        public BodyParts outerBody;
        public abstract void Init(BodyParts outerBody);
        public abstract int CheckMinEventTime(int time);
        abstract public void CoreAddInnerDic();

        public void AddUpdate(EventHandler<CoreEventArgs> e)
        {
            outerBody.core.coreChanged += e;//�������
        }
        public void RemoveUpdate(EventHandler<CoreEventArgs> e)
        {
            outerBody.core.coreChanged -= e;
        }

    }
    public abstract class Organ : InnerParts
    {
        public Organ(string name)
        {
            this.name = "Organ."+GetType().Name + "." + name;
        }
        //public Organ(BodyParts parts):this()
        //{
        //    outerBody = parts;
        //}
        public override void Init(BodyParts outerBody)
        {
            this.outerBody = outerBody;
            outerBody.core.coreChanged += CoreUpdate;
            CoreAddInnerDic();
        }
        public abstract void CoreUpdate(object obj, CoreEventArgs e);

    }
    namespace Inner.Organs
    {
        public class Womb : Organ
        {
            //������ ���޵Ǹ� �۵��Ѵ�. ��� ������ �ý��� �۵��Ѵ�.
            public float bloodLv;
            public int level;
            public bool isEnable;
            public float readyFragRate;
            public int menstruationRate = 1;
            public bool actMenstruation;
            public bool canfrag;
            public float semenRate;
            public bool isfrag;
            public Womb(string name) : base(name)
            {

            }
            #region Method
            public override int CheckMinEventTime(int time)//�̰� �����ϸ� ������ �ּ� �ð��� ��ȯ�Ѵ�.
            {
                return Mathf.Min(time, actMenstruation ? (int)Mathf.Ceil(readyFragRate / menstruationRate * 2880) : (int)Mathf.Ceil((1-readyFragRate) / menstruationRate * 43200));
            }
            public void Menstruation(int time)
            {
                if (actMenstruation)
                {
                    readyFragRate -= 1f / 2880 * (time) * menstruationRate;
                    canfrag = false;
                    if (readyFragRate < 0) actMenstruation = false;
                }
                else
                {
                    readyFragRate += 1f / 43200 * (time) * menstruationRate;
                    if (canfrag == false)
                    {
                        canfrag = (readyFragRate > 0.5f && readyFragRate < 1);
                    }
                    if (readyFragRate > 1) actMenstruation = true;
                }
                if (semenRate > 0)
                {
                    semenRate -= 1f / 5760 * time * menstruationRate;
                }
                else
                {
                    semenRate = 0;
                }
                if (canfrag)
                    CheckFragnant();
            }
            public void CheckFragnant()
            {
                Breast breast = (Breast)outerBody.core.FindInner("Breast");
                if (breast != null)
                {
                    Debug.Log("~!@!");
                    breast.StartFillUp();
                }

                //if (semenRate > 0||canfrag)
                //{
                //    isfrag = true;
                //    AddUpdate(FragSequence);
                //    //�극��ũ�� �ɸ� �۵����� �ʰ� �ּҽð��� ��ȯ�Ѵ�. �׸��� �ٽ� �۵�
                //    //�̺�Ʈ�ð��� �̸� �����Ϸ���..

                //    Breast breast = (Breast)outerBody.core.FindInner("breast");
                //    if (breast != null)
                //    {
                //        Debug.Log("~!@!");
                //        breast.StartFillUp();
                //    }
                //}
            }
            public void EndFrag()
            {
                if (isfrag)
                {
                    isfrag = false;
                    RemoveUpdate(FragSequence);
                    Breast breast = (Breast)outerBody.core.FindInner("breast");
                    if (breast != null)
                    {
                        Debug.Log("~!@!");
                        breast.EndFillUp();
                    }
                }
            }
            public void FragSequence(object obj, CoreEventArgs e)
            {
                //outerBody.core.corelist[0]
                //breast �˻� �ھ����� �ǵ��
                //�׳� ���� �������......
                //�˻��� ��� ���� �����Ѵ�.

            }

            public override void CoreAddInnerDic()
            {
                outerBody.core.GetInner().Add(name, this);
            }

            public override void CoreUpdate(object obj, CoreEventArgs e)
            {
                Menstruation(e.time);
            }
            #endregion
            public override void ToJson()
            {
                Utillity.ConvertToSaver(name, bloodLv, level, isEnable, readyFragRate, menstruationRate, actMenstruation, canfrag, semenRate, isfrag);
            }
            public override void FromJson(string[] strings)
            {
                int idx=0;
                ObjectParser(ref bloodLv, strings[idx++]);
                ObjectParser(ref level, strings[idx++]);
                ObjectParser(ref isEnable, strings[idx++]);
                ObjectParser(ref readyFragRate, strings[idx++]);
                ObjectParser(ref menstruationRate, strings[idx++]);
                ObjectParser(ref actMenstruation, strings[idx++]);
                ObjectParser(ref canfrag, strings[idx++]);
                ObjectParser(ref semenRate, strings[idx++]);
                ObjectParser(ref isfrag, strings[idx++]);
                //bloodLv = float.Parse(strings[idx++]);
                //level = float.Parse(strings[idx++]);
                //isEnable = float.Parse(strings[idx++]);
                //readyFragRate = float.Parse(strings[idx++]);
                //menstruationRate = float.Parse(strings[idx++]);
                //actMenstruation = bool.Parse(strings[idx++]);
                //canfrag = bool.Parse(strings[idx++]);
                //semenRate = float.Parse(strings[idx++]);
                //isfrag = bool.Parse(strings[idx++]);

            }
        }
        public class Breast : Organ
        {
            float milkRate;
            int milkml;
            int milkmlMax;
            public int lv;
            public Breast(string name) : base(name)
            {

            }
            #region Method
            public void StartFillUp()
            {
                Debug.LogWarning("fillup");
                AddUpdate(FillUp);
                lv++;
            }
            public void EndFillUp()
            {
                RemoveUpdate(FillUp);
                lv--;
            }

            public override int CheckMinEventTime(int time)
            {
                return time;
            }

            public override void CoreAddInnerDic()
            {
                outerBody.core.GetInner().Add(name, this);
            }
            public override void CoreUpdate(object obj, CoreEventArgs e)
            {
            }
            public void FillUp(object obj, CoreEventArgs e)
            {
                milkRate += 0.001f*e.time;
            }
            #endregion

            public override void ToJson()
            {
                Utillity.ConvertToSaver(name, milkRate, milkml, milkmlMax, lv);
            }

            public override void FromJson(string[] strings)
            {
                int idx = 0;
                ObjectParser(ref milkRate, strings[idx++]);
                ObjectParser(ref milkml, strings[idx++]);
                ObjectParser(ref milkmlMax, strings[idx++]);
            }
        }
    }

}


