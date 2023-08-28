using Character.BodySystem;
using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Garunnir
{
    public class Charactor
    {
        public string name;
        //캐릭터 개체정보
        public Actor dialogueActor;
        
        public Core bodyCore;
        public Charactor()
        {
            bodyCore=BodyFactory.CreateDefault();
            profiles.Add(new HumanProfile(bodyCore));
        }
        public List<Profile> profiles = new List<Profile>();
        

        public void SetProfile(params Profile[] profiles)
        {
            foreach (var item in profiles)
            {
                this.profiles.Add(item);
            }
        }
        public void SetProfile<T>(params (string,string,ComponentType)[] profiles)where T : Profile, new()
        {
            foreach (var item in profiles)
            {
                T addT = new T();
                addT.title = item.Item1;
                addT.value = item.Item2;
                addT.componentType = item.Item3;
                this.profiles.Add(addT);
            }
        }
        public void SetProfile(object profile)
        {

        }
    }
    public class CharactorManager:MonoBehaviour
    {
        private void Start()
        {
            CreateNPCs();
        }
        void Garam()
        {
            Charactor cha=new Charactor();
            cha.SetProfile<HumanProfile>(
                ("sprite", "Garam", ComponentType.img),
                ("name", "가람", ComponentType.text),
                ("age", "15", ComponentType.text),
                ("exp", "34/50", ComponentType.bar)
                );
        }
        void CreateNPCs()
        {
            Garam();
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
