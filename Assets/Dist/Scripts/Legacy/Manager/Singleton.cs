using System;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : class
{
    static Lazy<T> L_instance = new Lazy<T>(() =>
    {
        if (_instance != null)
            return _instance;

        _instance = FindAnyObjectByType(typeof(T)) as T;
        if (_instance == null)
            Debug.LogWarning($"[Singleton] {typeof(T).Name} not found in scene. Auto-create is disabled (legacy).");

        return _instance;
    });

    public static T Instance => L_instance.Value;
    static T _instance;

    public void SetDontDistroy() => DontDestroyOnLoad(this);
}
