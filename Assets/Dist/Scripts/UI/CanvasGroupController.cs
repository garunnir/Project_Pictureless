using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupController : MonoBehaviour
{
    [SerializeField] private Canvas m_canvas;
    [SerializeField] private bool m_blocksRaycasts = true;
    public RectTransform m_rect;
    private void Awake()
    {
        m_rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (m_rect == null)
        {
            Debug.LogError("It's not UI");
        }
    }
    private CanvasGroup _canvasGroup;

    float process = 0;
    public event UnityAction Start;
    public event UnityAction End;
    public event UnityAction HideStart;
    public event UnityAction HideEnd;
    public event UnityAction ShowStart;
    public event UnityAction ShowEnd;
    public bool isEnable;
    public bool complete;
    Coroutine coroutine;
    public Func<IEnumerator> AddHideAct;
    public Func<IEnumerator> AddShowAct;
    public bool isVisible { get; set; }

    private void Reset()
    {
        m_canvas = GetComponent<Canvas>();
    }
    public void HideForce() => Hide(true);
    public void ShowForce()=>Show(true);
    public void Hide() => Hide(false);
    public void Show() => Show(false);
    private void _HideForce()
    {
        Start?.Invoke();
        ShowStart?.Invoke();
        End?.Invoke();
        ShowEnd?.Invoke();
        process = 0;
        coroutine = null;
    }
    private void _ShowForce()
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
        yield return StartCoroutine(AddHideAct.Invoke());
        End?.Invoke();
        HideEnd?.Invoke();
        process = 1;
        coroutine = null;
        DoHide();
    }
    public virtual IEnumerator Cor_ShowAct()
    {
        process = 0;
        Start?.Invoke();
        ShowStart?.Invoke();
        yield return StartCoroutine(AddShowAct.Invoke());
        End?.Invoke();
        ShowEnd?.Invoke();
        process = 1;
        coroutine = null;
        DoShow();
    }
    public void Show(bool isForce = false)
    {
        if (m_canvas != null)
        {
            m_canvas.enabled = true;
        }

        if (isForce)
        {
            DoShow();
            _ShowForce();
        }
        else if (_canvasGroup.alpha != 1)
        {
            coroutine ??= StartCoroutine(Cor_ShowAct());
            //_canvasGroup.DOFade(1f, duration).SetEase(ease).OnComplete(
            //() => {
            //    DoShow();
            //}).SetUpdate(true);
        }
    }

    public void Hide(bool isForce = false)
    {
        if (isForce)
        {
            DoHide();
            _HideForce();
        }
        else
        {
            coroutine ??= StartCoroutine(Cor_HideAct());
            //_canvasGroup.DOFade(0f, duration).SetEase(ease).OnComplete(
            //() => {
            //    DoHide();
            //}).SetUpdate(true);
        }
    }

    [ContextMenu("Show")]
    private void DoShow()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (m_canvas != null)
        {
            m_canvas.enabled = true;
        }
        _canvasGroup.alpha = 1.0f;
        _canvasGroup.blocksRaycasts = m_blocksRaycasts;
        _canvasGroup.interactable = true;
        isVisible = true;
    }

    [ContextMenu("Hide")]
    private void DoHide()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        _canvasGroup.alpha = 0.0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        if (m_canvas != null)
        {
            m_canvas.enabled = false;
        }

        isVisible = false;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(CanvasGroupController))]
public class CanvasGroupControllerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Show"))
        {
            ((CanvasGroupController)target).Show(true);
        }
        if (GUILayout.Button("Hide"))
        {
            ((CanvasGroupController)target).Hide(true);
        }
        EditorGUILayout.EndHorizontal();
    }

}
#endif

