using Character.BodySystem;
using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomActor : Actor
{
    public List<Profile> profiles= new List<Profile>();

    public CustomActor()
    {
        fields = new List<Field>();
        TempGaram();
    }
    public void TempGaram()
    {
        profiles.Add(new HumanProfile(BodyFactory.CreateDefault()));
        profiles.Add(new HumanProfile("sprite", "Garam", ComponentType.img));
        profiles.Add(new HumanProfile("name", "°¡¶÷", ComponentType.text));
        profiles.Add(new HumanProfile("age", "15", ComponentType.text));
        profiles.Add(new HumanProfile("exp", "34/50", ComponentType.bar));
    }
    //public T SetProfile<T>(FieldType type,string title,string text)where T : Profile,new()
    //{
    //    T profile = new T();
    //    profile.type = type;
    //    profile.title = title;
    //    profile.value = text;
    //    profile.
    //    profiles.Add()
    //}
}

public abstract class Profile:Field
{
    public Core body;
    public ComponentType componentType;
    public Profile (string title,string value, ComponentType type)
    {
        this.title= title;
        this.value= value;
        this.componentType = type;
    }
    public Profile(Core body)
    {
        this.body = body;
    }

}
public class HumanProfile : Profile
{
    public CustomActor father;
    public CustomActor mother;
    
    public HumanProfile(string title, string value, ComponentType type) : base(title, value, type)
    {
    }
    public HumanProfile(Core body):base(body)
    {
    }
}
public class CustomField : Field
{

}