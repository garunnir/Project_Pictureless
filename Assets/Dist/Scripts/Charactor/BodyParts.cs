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
            Debug.Log(lhand.GetField().ContainsKey("DiseaseRate"));
            BodyParts rhand = upperbody.SetNext("rightshoulder").SetNext("rightupperarm").SetNext("rightelbow").SetNext("rightwrist").SetNext("rhand");
            BodyParts pelvis= upperbody.SetNext("velly").SetNext("waist").SetNext("pelvis");
            pelvis.SetNext("rupperleg").SetNext("rknee").SetNext("rfoot");
            lhand.SetNext("lthumb");
            lhand.SetNext("lindexfinger");
            lhand.SetNext("lmiddlefinger");
            lhand.SetNext("lringfinger");
            lhand.SetNext("lpinky");

            rhand.SetNext("rthumb");
            rhand.SetNext("rindexfinger");
            rhand.SetNext("rmiddlefinger");
            rhand.SetNext("rringfinger");
            rhand.SetNext("rpinky");

            //���� ������ü�� �����Ұ�
            //�㸮��ü�� �õ��Ѵ�
            body.Find("waist").Swap(new MechBody("waist"));
            //��ü�ص� �̸��� ������ ���̹Ƿ� Ÿ���� �־ �ٲٴ� ����� ����غ��� �ɰͰ���.
        }
    }
    public abstract class BodyParts
    {
        //start at head
        public Dictionary<string, float> field = new Dictionary<string, float>();
        public string name;//������
        public int durability;
        public List<BodyParts> next = new List<BodyParts>();
        public List<BodyParts> prev = new List<BodyParts>();
        public BodyParts(string name)
        {
            this.name = name;
        }
        public abstract BodyParts SetNext(string name);
        //{
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
                if(next.Count == 0)return null;
                foreach (var part in next)
                {
                    if (part == null)
                    {
                        next.Remove(part);
                    }
                    else
                    {
                        return part.Find(name);
                    }
                }
                return null;
            }
        }
        public void Swap(BodyParts parts)
        {
            parts.next=this.next;
            parts.prev=this.prev;
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
    }
    public class MechBody : BodyParts
    {
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


