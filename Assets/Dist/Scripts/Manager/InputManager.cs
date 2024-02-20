using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static Func<bool> click;
    private void Awake()
    {
        click = IsClike;
    }
    private bool IsClike() {  return Input.GetMouseButtonDown(0); }
    public static RaycastHit RayCast()//todo 공통사용가능한 부위로 옮겨야함.
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit info;
        bool ishit = Physics.Raycast(ray, out info);
        if (ishit) Debug.Log(info.collider.name);
        return info;
    }
}
