// ============================================================
// UiCancelRouter — ESC/Cancel 우선순위 레지스트리 (확장 SSOT)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class UiCancelRouter : MonoBehaviour
{
    static UiCancelRouter _instance;

    readonly List<IUiCancelConsumer> _consumers = new(8);
    bool _sortedDirty = true;

    public static UiCancelRouter Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            _instance = FindAnyObjectByType<UiCancelRouter>();
            return _instance;
        }
    }

    public static void Register(IUiCancelConsumer consumer)
    {
        if (consumer == null)
            return;

        UiCancelRouter router = EnsureInstance();
        if (router == null || router._consumers.Contains(consumer))
            return;

        router._consumers.Add(consumer);
        router._sortedDirty = true;
    }

    public static void Unregister(IUiCancelConsumer consumer)
    {
        if (consumer == null || _instance == null)
            return;

        if (_instance._consumers.Remove(consumer))
            _instance._sortedDirty = true;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[UiCancelRouter] Duplicate ignored.", this);
            return;
        }

        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void Update()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        if (!input.TryReadCancelPerformedThisFrame(out bool canceled) || !canceled)
            return;

        if (_sortedDirty)
            SortConsumers();

        for (int i = 0; i < _consumers.Count; i++)
        {
            IUiCancelConsumer consumer = _consumers[i];
            if (consumer == null)
                continue;

            if (consumer.TryHandleCancel())
                return;
        }
    }

    void SortConsumers()
    {
        _consumers.Sort(ComparePriority);
        _sortedDirty = false;
    }

    static int ComparePriority(IUiCancelConsumer a, IUiCancelConsumer b)
    {
        int diff = b.CancelPriority.CompareTo(a.CancelPriority);
        return diff != 0 ? diff : 0;
    }

    static UiCancelRouter EnsureInstance()
    {
        if (_instance != null)
            return _instance;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[UiCancelRouter] Canvas not found — cannot register cancel consumer.");
            return null;
        }

        if (!canvas.TryGetComponent(out UiCancelRouter router))
            router = canvas.gameObject.AddComponent<UiCancelRouter>();

        return router;
    }
}
