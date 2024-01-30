using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleToggle : MonoBehaviour
{
    public void Toggle(GameObject goo)
    {
        if (goo == null) return;
        goo.SetActive(!goo.activeSelf);
    }
}
