using System;
using System.Collections.Generic;
using UnityEngine;

namespace Character.BodySystem
{
    public class BodyFactory
    {
        //public abstract void build();
        public static Core CreateDefault()
        {
            HumanoidBody body = new HumanoidBody("head");
            body.AddCore(new Core());
            BodyParts upperbody = body.SetNext("neck").SetNext<HumanoidBody>("upperBody", new Inner.Organs.Breast());
            BodyParts lhand = upperbody.SetNext("leftshoulder").SetNext("leftupperarm").SetNext("leftelbow").SetNext("leftwrist").SetNext("lhand");
            BodyParts rhand = upperbody.SetNext("rightshoulder").SetNext("rightupperarm").SetNext("rightelbow").SetNext("rightwrist").SetNext("rhand");
            BodyParts pelvis = upperbody.SetNext("velly").SetNext<MechBody>("waist").SetNext<HumanoidBody>("pelvis", new Inner.Organs.Womb());
            BodyParts rfoot = pelvis.SetNext("rupperleg").SetNext("rknee").SetNext("rfoot");
            BodyParts lfoot = pelvis.SetNext("lupperleg").SetNext("lknee").SetNext("lfoot");
            lhand.SetNext("lthumb", "lindexfinger", "lmiddlefinger", "lringfinger", "lpinky");
            rhand.SetNext("rthumb", "rindexfinger", "rmiddlefinger", "rringfinger", "rpinky");
            rfoot.SetNext("rbigtoe", "rsecondtoe", "rthirdtoe", "rforthtoe", "rlittletoe");
            lfoot.SetNext("lbigtoe", "lsecondtoe", "lthirdtoe", "lforthtoe", "llittletoe");

            //조건 파츠교체가 가능할것
            //허리교체를 시도한다
            //Debug.Log(body.Find("waist"));
            //BodyParts mpart= new MechBody("waist");
            //body.Find("waist").Swap(mpart);
            //BodyParts tmpb = body.Find("waist");
            //tmpb.GetField().ContainsKey("DiseaseRate");
            //Debug.Log("" + tmpb.GetField().ContainsKey("DiseaseRate") + tmpb.GetField().ContainsKey("EnergyRate") + tmpb.GetField().ContainsKey("DamageRate"));
            //교체해도 이름은 동일할 것이므로 타입을 넣어서 바꾸는 방법을 고려해봐도 될것같다.
            return body.core;
        }
    }


    public class CoreEventArgs:EventArgs
    {
        public int time { get; set; }
        public DateTime TimeReached { get; set; }
    }
    [SerializeField]
    public class Core
    {
        [SerializeField]
        public List<BodyParts> corelist = new List<BodyParts>();
        public List<BodyParts> partslist = new List<BodyParts>();
        public Dictionary<string,InnerParts> innerDic = new Dictionary<string, InnerParts>();
        public event EventHandler<CoreEventArgs> coreChanged;
        public Guid instanceID;
        public Core() 
        {
            Subscribe();
            instanceID= Guid.NewGuid();
            Debug.Log(instanceID);
        }
        public void Subscribe()
        {
        }
        private void Core_coreChanged(object sender, CoreEventArgs e)
        {
            //Debug.LogError("core subupdate" +instanceID);
            int orginTime=e.time;
            int atime = GetMinEventTime(e.time);
            e.time = atime;
            coreChanged?.Invoke(sender,e);
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
        public void CoreCommend(string commend)//다른 파츠에 명령전달
        {

        }
        public InnerParts FindInner(string name)
        {
            return innerDic[name];
        }

        //~Core() 
        //{
        //    update -= AccUpdate;
        //}
        internal int GetMinEventTime(int time)
        {
            int min = int.MaxValue;
            foreach (var item in innerDic)
            {
                int newv=item.Value.CheckMinEventTime(time);
                if (newv < min)
                {
                    min= newv;
                }
            }
            if(int.MaxValue == min)
            {
                Debug.LogWarning("innerDic is empty");
                return time;
            }
            return min;
        }
    }
    [SerializeField]
    public abstract class BodyParts
    {
        //public Action<int> update;
        public bool isCore;
        public Core core;

        public Dictionary<string, float> field = new Dictionary<string, float>();
        public string name;//부위명
        public int durability;
        public List<BodyParts> next = new List<BodyParts>();
        public List<BodyParts> prev = new List<BodyParts>();
        public List<InnerParts> innerParts = new List<InnerParts>();

        public BodyParts()
        {

        }
        public BodyParts(string name) : this()
        {
            this.name = name;
        }
        public BodyParts(string name,InnerParts inner) : this(name)
        {
            innerParts.Add(inner);
        }
        public BodyParts(string name, bool isCore = false) : this(name)
        {
            if (isCore)
            {
                this.isCore = true;
                if (core == null)
                {
                    core = new Core();
                }
                core.partslist.Add(this);
                core.corelist.Add(this);
            }

        }
        public void AddCore(Core core)
        {
            if (core == null)
            {
                isCore = true;
                this.core = new Core();
                Debug.Log("created");
                this.core.partslist.Add(this);
                this.core.corelist.Add(this);
            }
            else
            {
                this.core = core;
                isCore = false;
                this.core.partslist.Add(this);
            }
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
            foreach (var part in prev)//전노드의 연결과 쌍방통행이므로 전노드와 다음노드의 참조도 바꿔줘야 한다.
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

    }
    public class HumanoidBody : BodyParts
    {
        public List<Organ> organs = new List<Organ>();
        public HumanoidBody() : base()
        {
            field.Add("DiseaseRate", 0);
            field.Add("DamageRate", 0);
        }
        //start at head
        public HumanoidBody(string name) : base(name)
        {
            field.Add("DiseaseRate", 0);
            field.Add("DamageRate", 0);
        }
        public HumanoidBody(string name,Organ organ) : this(name)
        {
            SetOrgan(organ);
        }
        public override BodyParts SetNext(string name)
        {
            return SetNext<HumanoidBody>(name);
        }

        public override BodyParts[] SetNext(params string[] name)
        {
            BodyParts[] rtn = new BodyParts[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                HumanoidBody parts = new HumanoidBody(name[i]);
                rtn[i] = parts;
                next.Add(parts);
                parts.prev.Add(this);
            }
            return rtn;
        }
        public HumanoidBody SetNext(string name,Organ organ) 
        {
            HumanoidBody parts = new HumanoidBody(name, organ);
            next.Add(parts);
            parts.prev.Add(this);
            return parts;
        }
        public void SetOrgan(Organ organ)
        {
            organs.Add(organ);
        }

    }
    public class MechBody : BodyParts
    {
        public MechBody() : base()
        {
            field.Add("EnergyRate", 1);
            field.Add("DamageRate", 0);
        }
        public MechBody(string name) : base(name)
        {
            field.Add("EnergyRate", 1);
            field.Add("DamageRate", 0);
        }
        public override BodyParts SetNext(string name)
        {
            BodyParts parts = new HumanoidBody(name);
            next.Add(parts);
            parts.prev.Add(this);
            return parts;
        }

        public override BodyParts[] SetNext(params string[] name)
        {
            throw new System.NotImplementedException();
        }
    }

    public abstract class InnerParts
    {
        public BodyParts outerBody;
        public abstract void Init(BodyParts outerBody);
        public abstract int CheckMinEventTime(int time);
        abstract public void CoreAddInnerDic();
        public void AddUpdate(EventHandler<CoreEventArgs> e)
        {
            outerBody.core.coreChanged += e;//통상적용
        }
        public void RemoveUpdate(EventHandler<CoreEventArgs> e)
        {
            outerBody.core.coreChanged -= e;
        }

    }
    public abstract class Organ:InnerParts
    {
        public Organ()
        {
        }
        public Organ(BodyParts parts):this()
        {
            outerBody = parts;
        }
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
            //혈류만 공급되면 작동한다. 장기 단위로 시스템 작동한다.
            public float bloodLv;
            public int level;
            public bool isEnable;
            public float readyFragRate;
            public int menstruationRate = 1;
            public bool actMenstruation;
            public bool canfrag;
            public float semenRate;
            public bool isfrag;

            public Womb() : base()
            {
            }
            public override int CheckMinEventTime(int time)//이걸 실행하면 오버시 최소 시간을 반환한다.
            {
                return Mathf.Min(time, actMenstruation ? (int)Mathf.Ceil(readyFragRate / menstruationRate * 2880) : (int)Mathf.Ceil((readyFragRate - 1) / menstruationRate * 43200));
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
                if(canfrag)
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
                //    //브레이크를 걸면 작동하지 않고 최소시간을 반환한다. 그리고 다시 작동
                //    //이벤트시간을 미리 예측하려면..

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
            public void FragSequence(object obj,CoreEventArgs e)
            {
                //outerBody.core.corelist[0]
                //breast 검색 코어한테 피드백
                //그냥 직접 명령하자......
                //검색을 어떻게 할지 생각한다.

            }

            public override void CoreAddInnerDic()
            {
                outerBody.core.innerDic.Add(nameof(Womb), this);
            }

            public override void CoreUpdate(object obj, CoreEventArgs e)
            {
                Menstruation(e.time);
            }
        }
        public class Breast : Organ
        {
            float milkRate;
            int milkml;
            int milkmlMax;
            public int lv;


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
                outerBody.core.innerDic.Add(nameof(Breast), this);
            }
            public override void CoreUpdate(object obj, CoreEventArgs e)
            {
            }
            public void FillUp(object obj, CoreEventArgs e)
            {
                milkRate += 0.001f;
            }
        }
    }

}


