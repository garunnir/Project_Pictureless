// ============================================================
// EnvironmentRuntimeDebugWindow — Play 모드 월드 환경 Odin 디버그 창
// ============================================================

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public sealed class EnvironmentRuntimeDebugWindow : OdinEditorWindow
{
    const double RepaintIntervalSeconds = 1.0d;

    [MenuItem("Tools/Environment Runtime Debug")]
    static void Open() => GetWindow<EnvironmentRuntimeDebugWindow>("Environment Runtime Debug");

    [SerializeField, HideInInspector]
    EnvironmentRuntimeDebugModel _model = new EnvironmentRuntimeDebugModel();

    double _nextRepaintTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_model == null)
            _model = new EnvironmentRuntimeDebugModel();

        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    protected override void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.update -= OnEditorUpdate;
        base.OnDisable();
    }

    void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode
            || state == PlayModeStateChange.EnteredEditMode)
            CharacterClimateHost.DebugOutdoorOverride =
                CharacterClimateHost.EditorOutdoorOverride.Map;

        Repaint();
    }

    void OnEditorUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (EditorGUIUtility.editingTextField)
            return;

        double now = EditorApplication.timeSinceStartup;
        if (now < _nextRepaintTime)
            return;

        _nextRepaintTime = now + RepaintIntervalSeconds;
        Repaint();
    }

    [Title("Environment Runtime Debug")]
    [InfoBox("Play mode only. Edit writes are disabled outside Play.", InfoMessageType.Warning, "ShowEditModeWarning")]
    [InfoBox("$LiveStatus", InfoMessageType.None)]
    [ShowInInspector, HideLabel, InlineProperty]
    [HideReferenceObjectPicker]
    EnvironmentRuntimeDebugModel Model
    {
        get
        {
            if (_model == null)
                _model = new EnvironmentRuntimeDebugModel();
            return _model;
        }
        set
        {
            if (value != null)
                _model = value;
        }
    }

    bool ShowEditModeWarning => !Application.isPlaying;

    string LiveStatus
    {
        get
        {
            if (!Application.isPlaying)
                return "Not playing.";

            WorldClock clock = WorldClock.Instance;
            if (clock == null)
                return "No WorldClock in the loaded scenes.";

            WorldWeatherHost weatherHost = WorldWeatherHost.Instance;
            string weather = weatherHost != null
                ? WeatherExposure.KindLabel(weatherHost.CurrentKind)
                : "(no WorldWeatherHost)";

            return TimeDisplayFormat.Format(clock.DayIndex, clock.HourOfDay, clock.MinuteOfHour)
                   + "  " + clock.Period
                   + "  |  Weather=" + weather
                   + "  |  Outdoor=" + CharacterClimateHost.DebugOutdoorOverride;
        }
    }
}
