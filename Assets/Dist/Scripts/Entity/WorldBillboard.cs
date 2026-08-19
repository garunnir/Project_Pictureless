// ============================================================
// WorldBillboard — Yaw-only world UI that faces the camera
// ============================================================

using UnityEngine;

public class WorldBillboard : MonoBehaviour
{
    [SerializeField] bool _billboardEnabled = true;
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.Realtime;
    [SerializeField] Camera cam;
    [SerializeField, Range(0, 15)] float yawThresholdDeg = 1f;
    [SerializeField, Range(0, .05f)] float updateInterval = .02f;

    float nextT, lastYaw;

    public bool BillboardEnabled
    {
        get => _billboardEnabled;
        set => _billboardEnabled = value;
    }

    public void SetBillboardEnabled(bool enabled) => _billboardEnabled = enabled;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!_billboardEnabled || cam == null) return;

        float now = TimeScaleService.TimeNow(_timeChannel);
        if (now < nextT) return;
        nextT = now + updateInterval;

        float yaw = cam.transform.eulerAngles.y;
        if (Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) < yawThresholdDeg) return;
        lastYaw = yaw;

        var toCam = cam.transform.position - transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 1e-6f) return;
        transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
    }
}
