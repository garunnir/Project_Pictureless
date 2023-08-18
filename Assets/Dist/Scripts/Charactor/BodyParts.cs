using System.Collections;
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
    }
  

    public class Humanoid:BodyFactory
    {
        public Humanoid()
        {
            HumanoidBody body = new HumanoidBody("head");
            BodyParts upperbody = body.SetNext("neck").SetNext("upperBody");
            BodyParts lhand = upperbody.SetNext("leftshoulder").SetNext("leftupperarm").SetNext("leftelbow").SetNext("leftwrist").SetNext("lhand");
            BodyParts rhand = upperbody.SetNext("rightshoulder").SetNext("rightupperarm").SetNext("rightelbow").SetNext("rightwrist").SetNext("rhand");
            BodyParts pelvis= upperbody.SetNext("velly").SetNext<MechBody>("waist").SetNext("pelvis");
            BodyParts rfoot= pelvis.SetNext("rupperleg").SetNext("rknee").SetNext("rfoot");
            BodyParts lfoot= pelvis.SetNext("lupperleg").SetNext("lknee").SetNext("lfoot");
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
            Debug.Log(""+tmpb.GetField().ContainsKey("DiseaseRate") + tmpb.GetField().ContainsKey("EnergyRate")+ tmpb.GetField().ContainsKey("DamageRate"));
            //��ü�ص� �̸��� ������ ���̹Ƿ� Ÿ���� �־ �ٲٴ� ����� ����غ��� �ɰͰ���.
        }
    }
    public class Core
    {
        public List<BodyParts> corelist = new List<BodyParts>();
        public List<BodyParts> partslist = new List<BodyParts>();
        public Core() { }
    }

    public abstract class BodyParts 
    {
        public bool isCore;
        public Core core;

        public Dictionary<string, float> field = new Dictionary<string, float>();
        public string name;//������
        public int durability;
        public List<BodyParts> next = new List<BodyParts>();
        public List<BodyParts> prev = new List<BodyParts>();

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
        public BodyParts(string name,bool isCore=false):this(name)
        {
            if(isCore)
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
                        if(parts != null)return parts;
                    }
                }
                return null;
            }
        }
        public void Swap(BodyParts parts)
        {
            parts.next=this.next;
            parts.prev=this.prev;
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
    public class HumanoidBody:BodyParts
    {
        public HumanoidBody() : base()
        {
            field.Add("DiseaseRate", 0);
            field.Add("DamageRate", 0);
        }
        //start at head
        public HumanoidBody(string name):base (name)
        {
            field.Add("DiseaseRate", 0);
            field.Add("DamageRate", 0);
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
            BodyParts[] rtn= new BodyParts[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                HumanoidBody parts = new HumanoidBody(name[i]);
                rtn[i]= parts;
                next.Add(parts);
                parts.prev.Add(this);
            }
            return rtn;
        }

    }
    public class MechBody : BodyParts
    {
        public MechBody():base()
        {
            field.Add("EnergyRate", 1);
            field.Add("DamageRate", 0);
        }
        public MechBody(string name):base (name)
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
    public abstract class BodyField
    {
        public Dictionary<string, float> field=new Dictionary<string, float>();
        public float GetField(string name)
        {
            if (field.ContainsKey(name))
            {
                return field[name];
            }
            else 
            {
                Debug.LogWarning("not exist field");
                return -1; 
            }
        }
    }
    public class HumanField:BodyField
    {
        //����ϰ��������, ����� ���������� ��Ÿ���� �;
        //�ȵǴ� ���� �ҷ��ö�...
        public HumanField()
        {
            field.Add("DiseaseRate", 0);
            field.Add("DamageRate", 0);
        }
    }
    public class MachField:BodyField
    {
        public MachField()
        {
            field.Add("EnergyRate", 1);
            field.Add("DamageRate", 0);
        }
    }
}


