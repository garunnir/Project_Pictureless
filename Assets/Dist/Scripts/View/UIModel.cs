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
        var inst = new UIEventArgs();
        field?.Invoke(null,inst);
        print(inst.field);
    }

    private void UIModel_field(object sender, UIEventArgs e)
    {
        throw new NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public class UIEventArgs : EventArgs
{
    public string field;
}
