using System;
using System.Collections.Generic;
using UnityEngine;

namespace Character.BodySystem
{
    public class BodyGenerator
    {
        BodyGenerator()
        {

        }
        public BodyFactory DefaultBody()
        {
            return new Humanoid() as BodyFactory;
        }
    }
    public abstract class BodyFactory
    {
        //public abstract void build();
        public Action<int> update;
    }


    public class Humanoid : BodyFactory
    {
        public Humanoid()
        {
            HumanoidBody body = new HumanoidBody("head");
            update += body.core.update;
            body.core.Update(update);
            BodyParts upperbody = body.SetNext("neck").SetNext<HumanoidBody>("upperBody",new Inner.Organs.Breast());
            BodyParts lhand = upperbody.SetNext("leftshoulder").SetNext("leftupperarm").SetNext("leftelbow").SetNext("leftwrist").SetNext("lhand");
            BodyParts rhand = upperbody.SetNext("rightshoulder").SetNext("rightupperarm").SetNext("rightelbow").SetNext("rightwrist").SetNext("rhand");
            BodyParts pelvis = upperbody.SetNext("velly").SetNext<MechBody>("waist").SetNext<HumanoidBody>("pelvis",new Inner.Organs.Womb());
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
            BodyParts tmpb = body.Find("waist");
            //tmpb.GetField().ContainsKey("DiseaseRate");
            Debug.Log("" + tmpb.GetField().ContainsKey("DiseaseRate") + tmpb.GetField().ContainsKey("EnergyRate") + tmpb.GetField().ContainsKey("DamageRate"));
            //교체해도 이름은 동일할 것이므로 타입을 넣어서 바꾸는 방법을 고려해봐도 될것같다.
        }
    }
    public class Core
    {
        public List<BodyParts> corelist = new List<BodyParts>();
        public List<BodyParts> partslist = new List<BodyParts>();
        public Dictionary<string,InnerParts> innerDic = new Dictionary<string, InnerParts>();
        public Action<int> update;
        public Core() 
        {
            update += AccUpdate;
        }
        public void CoreCommend(string commend)//다른 파츠에 명령전달
        {

        }
        public InnerParts FindInner(string name)
        {
            return innerDic[name];
        }
        public void Update(Action<int> timeupdate)
        {
            timeupdate += update;//매개변수로 들어와서 복사취급이 되는것인가..?
        }
        void AccUpdate(int time)
        {
            int atime = GetMinEventTime(time);
            update(GetMinEventTime(atime));
            if (atime != time) update(atime);
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
            if (core == null)
            {
                isCore = true;
                core = new Core();
                core.partslist.Add(this);
                core.corelist.Add(this);
            }
            else
            {
                isCore = false;
                core.partslist.Add(this);
            }
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
        public T SetNext<T>(string name) where T : BodyParts, new()
        {
            T parts = new T();
            parts.name = name;
            next.Add(parts);
            parts.prev.Add(this);
            return parts;
        }
        public T SetNext<T>(string name, InnerParts inner) where T : BodyParts, new()
        {
            T parts = new T();
            parts.name = name;
            parts.innerParts.Add(inner);
            inner.outerBody = parts;
            inner.Init();
            inner.CoreAdd(inner);
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
            HumanoidBody parts = new HumanoidBody(name);
            next.Add(parts);
            parts.prev.Add(this);
            return parts;
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
        public abstract void Init();
        public abstract int CheckMinEventTime(int time);
        abstract public void CoreAdd(InnerParts thisparts);

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
        public override void Init()
        {
            outerBody.core.update += sequnce;
        }
        public Action<int> sequnce;

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
                sequnce += Fnc;
            }
            public void Fnc(int time)//타임 단위는 1분
            {
                //시간을 오버하면....
                //특정지점에서 한번 멈춰야 한다.
                //특정시점에서 멈추고 이벤트를 진행해야 한다.
                int minActTime = CheckMinEventTime(time);
                int remainTime = time - minActTime;
                if (remainTime > 0)
                {
                    ActALL(minActTime);
                    Fnc(remainTime);
                }
            }
            public void ActALL(int time)
            {
                Menstruation(time);
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
            }
            public void Fragnant(int time)
            {
                if (semenRate > 0)
                {
                    semenRate -= 1f / 5760 * time * menstruationRate;
                    if (canfrag)
                    {
                        isfrag = true;
                        sequnce += FragSequence;
                    }
                }
                else
                {
                    if (isfrag)
                    {
                        isfrag = false;
                        sequnce -= FragSequence;
                    }
                    semenRate = 0;
                }
            }
            public void FragSequence(int time)
            {
                //outerBody.core.corelist[0]
                //breast 검색 코어한테 피드백
                //그냥 직접 명령하자......
                //검색을 어떻게 할지 생각한다.
                Breast breast = (Breast)outerBody.core.FindInner("breast");
                if (breast != null)
                {
                    Debug.Log("~!@!");
                    breast.lv++;
                    breast.StartFillUp();
                }
            }

            public override void CoreAdd(InnerParts thisparts)
            {
                outerBody.core.innerDic.Add(nameof(Womb), this);
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
                sequnce += FillUp;
            }
            public void FillUp(int time)
            {
                milkRate += 0.001f;
            }

            public override int CheckMinEventTime(int time)
            {
                throw new NotImplementedException();
            }

            public override void CoreAdd(InnerParts thisparts)
            {
                outerBody.core.innerDic.Add(nameof(Breast), this);
            }
        }
    }

}


