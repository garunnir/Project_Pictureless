using Garunnir;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIModel : MonoBehaviour
{
    public event EventHandler<UIEventArgs> field;
    // Start is called before the first frame update
    void Start()
    {

    }
    public void Excute(int id)
    {
        var inst = new UIEventArgs();
        Character cha=GameManager.Instance.characters.Find(x => x.id == id);
        if (cha == null)
        {
            Debug.LogWarning("isn't Exist charactor. id=" + id);
            return;
        }
        field?.Invoke(cha, inst);
        cha.SetField(inst.field);
        Debug.Log(inst.sfield);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public class UIEventArgs : EventArgs
{
    public string sfield;
    public Dictionary<string, (bool, object)> field = new Dictionary<string, (bool, object)>();
}
