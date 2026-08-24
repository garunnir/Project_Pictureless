// ============================================================
// PlayerSightVisionBinder — possessed 캐릭터 시야 반경을 PlayerSight Spot에 반영
// ============================================================

using UnityEngine;

public static class PlayerSightVisionBinder
{
    static Light s_spotLight;
    static float s_lastRange = -1f;

    public static void Sync(PlayerPossessedInputHost host)
    {
        if (host == null || host.Body == null)
            return;

        if (!TryResolveSpotLight(out Light spot))
            return;

        CharacterVision vision = host.Body.GetComponent<CharacterVision>();
        float targetRange = vision != null
            ? vision.EffectiveDetectRadius
            : CharacterVisionDefaults.DetectRadius;

        if (Mathf.Abs(s_lastRange - targetRange) < 0.001f)
            return;

        spot.range = targetRange;
        s_lastRange = targetRange;
    }

    static bool TryResolveSpotLight(out Light spot)
    {
        if (s_spotLight != null)
        {
            spot = s_spotLight;
            return true;
        }

        GameObject root = GameObject.Find("PlayerSight");
        if (root == null)
        {
            spot = null;
            return false;
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null || lights[i].type != LightType.Spot)
                continue;
            s_spotLight = lights[i];
            break;
        }

        spot = s_spotLight;
        return spot != null;
    }
}
