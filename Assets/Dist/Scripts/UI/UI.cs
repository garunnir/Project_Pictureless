using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class UI : MonoBehaviour
{
    public RectTransform rect { get; private set; }
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if(rect == null)
        {
            Debug.LogError("It's not UI");
        }
    }
    float process = 0;
    public UnityAction Start;
    public UnityAction End;
    public UnityAction HideStart;
    public UnityAction HideEnd;
    public UnityAction ShowStart;
    public UnityAction ShowEnd;
    public bool isEnable;
    public bool complete;
    Coroutine coroutine;
    public virtual void Hide()
    {
        coroutine ??= StartCoroutine(Cor_HideAct());
    }
    public virtual void Show()
    {
        coroutine ??= StartCoroutine(Cor_ShowAct());
    }
    public virtual void HideForce()
    {
        Start?.Invoke();
        HideStart?.Invoke();
        End?.Invoke();
        HideEnd?.Invoke();
        process = 0;
        coroutine = null;
    }
    public virtual void ShowForce()
    {
        Start?.Invoke();
        ShowStart?.Invoke();
        End?.Invoke();
        ShowEnd?.Invoke();
        process = 0;
        coroutine = null;
    }
    public virtual IEnumerator Cor_HideAct()
    {
        process = 0;
        Start?.Invoke();
        HideStart?.Invoke();
        yield return StartCoroutine(AddHideAct());
        End?.Invoke();
        HideEnd?.Invoke();
        process = 1;
        coroutine = null;
    }
    public virtual IEnumerator Cor_ShowAct()
    {
        process = 0;
        Start?.Invoke();
        ShowStart?.Invoke();
        yield return StartCoroutine(AddShowAct());
        End?.Invoke();
        ShowEnd.Invoke();
        process = 1;
        coroutine = null;
    }
    public abstract IEnumerator AddHideAct();
    public abstract IEnumerator AddShowAct();
}
