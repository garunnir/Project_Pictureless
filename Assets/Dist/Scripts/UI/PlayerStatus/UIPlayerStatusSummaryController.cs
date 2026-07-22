// ============================================================

// UIPlayerStatusSummaryController — HUD 상태 요약 패널 바인드·갱신

// ============================================================



using UnityEngine;



public sealed class UIPlayerStatusSummaryController : MonoBehaviour

{

    [SerializeField] UIPlayerStatusSummaryPanel _panel;

    [SerializeField] UICanvasLayerHost _layerHost;

    [SerializeField] UIPlayerStatusSummaryPanel _panelPrefab;

    [SerializeField] Canvas _uiCanvas;



    PlayerStatusViewModel _viewModel;



    void Awake()

    {

        EnsurePanel();

        if (!PlayerStatusUIBridge.TryResolve(out _viewModel))

        {

            Debug.LogError(

                "[UIPlayerStatusSummaryController] PlayerStatusUIBridge not found in scene.",

                this);

            return;

        }



        if (_panel != null)

            _panel.BindViewModel(_viewModel);



        _viewModel.MoodChanged += OnMoodChanged;

        Refresh();

    }



    void OnDestroy()

    {

        if (_viewModel != null)

            _viewModel.MoodChanged -= OnMoodChanged;

    }



    void OnMoodChanged() => Refresh();



    void Refresh()

    {

        if (_panel != null)

            _panel.Refresh();

    }



    void EnsurePanel()

    {

        if (_panel != null)

            return;



        EnsureReferences();

        if (_panelPrefab == null || _uiCanvas == null)

            return;



        Transform hudRoot = _layerHost != null

            ? _layerHost.GetLayerRoot(UICanvasLayer.HUD)

            : _uiCanvas.transform;



        _panel = Instantiate(_panelPrefab, hudRoot);

        _panel.name = "Grp_PlayerStatusSummary";

    }



    void EnsureReferences()

    {

        if (_uiCanvas == null)

            _uiCanvas = FindAnyObjectByType<Canvas>();

        if (_layerHost == null && _uiCanvas != null)

            _layerHost = _uiCanvas.GetComponent<UICanvasLayerHost>();

    }



    public void SetPanelPrefab(UIPlayerStatusSummaryPanel prefab) => _panelPrefab = prefab;

}


