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

            //���� ������ü�� �����Ұ�
            //�㸮��ü�� �õ��Ѵ�
            //Debug.Log(body.Find("waist"));
            //BodyParts mpart= new MechBody("waist");
            //body.Find("waist").Swap(mpart);
            BodyParts tmpb = body.Find("waist");
            //tmpb.GetField().ContainsKey("DiseaseRate");
            Debug.Log("" + tmpb.GetField().ContainsKey("DiseaseRate") + tmpb.GetField().ContainsKey("EnergyRate") + tmpb.GetField().ContainsKey("DamageRate"));
            //��ü�ص� �̸��� ������ ���̹Ƿ� Ÿ���� �־ �ٲٴ� ����� ����غ��� �ɰͰ���.
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
        public void CoreCommend(string commend)//�ٸ� ������ �������
        {

        }
        public InnerParts FindInner(string name)
        {
            return innerDic[name];
        }
        public void Update(Action<int> timeupdate)
        {
            timeupdate += update;//�Ű������� ���ͼ� ��������� �Ǵ°��ΰ�..?
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
        public string name;//������
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

            public Womb() : base()
            {
                sequnce += Fnc;
            }
            public void Fnc(int time)//Ÿ�� ������ 1��
            {
                //�ð��� �����ϸ�....
                //Ư���������� �ѹ� ����� �Ѵ�.
                //Ư���������� ���߰� �̺�Ʈ�� �����ؾ� �Ѵ�.
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
            public override int CheckMinEventTime(int time)//�̰� �����ϸ� ������ �ּ� �ð��� ��ȯ�Ѵ�.
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
                //breast �˻� �ھ����� �ǵ��
                //�׳� ���� �������......
                //�˻��� ��� ���� �����Ѵ�.
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


