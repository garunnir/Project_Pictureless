using System;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : class
{
    public static Lazy<T> L_instance = new Lazy<T>(() =>
    {
        if (_instance == null)
        {
            T findinst = FindObjectOfType(typeof(T)) as T;
            if (findinst == null)
            {
                _instance=new GameObject(nameof(T),typeof(T)).GetComponent<T>();
            }
            return _instance;
        }
        else return _instance;

    });

    public static T Instance { get => L_instance.Value; }
    static T _instance;

    public void SetDontDistroy() => DontDestroyOnLoad(this);  
}
