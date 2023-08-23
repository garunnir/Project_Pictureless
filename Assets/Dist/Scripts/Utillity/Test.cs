using Character.BodySystem;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Test : MonoBehaviour
{
    [DllImport("Dll1")]
    public static extern double Sum(double a,double b);
    // Start is called before the first frame update
    Character.BodySystem.Humanoid body;

    void Start()
    {
        //print(fnDll1());
        Debug.Log(Sum(2, 1));
        body = new Character.BodySystem.Humanoid();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            body.update(5);
        }
    }
}
