// ============================================================
// HudLayoutStore — HUD 참가자 anchoredPosition/sizeDelta PlayerPrefs SSOT
// ============================================================

using UnityEngine;

public static class HudLayoutStore
{
    const string Prefix = "HudLayout.";

    public static bool TryLoad(string participantId, out Vector2 anchoredPosition, out Vector2 sizeDelta)
    {
        anchoredPosition = default;
        sizeDelta = default;

        if (string.IsNullOrEmpty(participantId))
            return false;

        string posXKey = Prefix + participantId + ".pos.x";
        string posYKey = Prefix + participantId + ".pos.y";
        string sizeXKey = Prefix + participantId + ".size.x";
        string sizeYKey = Prefix + participantId + ".size.y";
        if (!PlayerPrefs.HasKey(posXKey) || !PlayerPrefs.HasKey(sizeXKey))
            return false;

        anchoredPosition = new Vector2(
            PlayerPrefs.GetFloat(posXKey),
            PlayerPrefs.GetFloat(posYKey));
        sizeDelta = new Vector2(
            PlayerPrefs.GetFloat(sizeXKey),
            PlayerPrefs.GetFloat(sizeYKey));
        return true;
    }

    public static void Save(string participantId, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (string.IsNullOrEmpty(participantId))
            return;

        string posKey = Prefix + participantId + ".pos";
        string sizeKey = Prefix + participantId + ".size";
        PlayerPrefs.SetFloat(posKey + ".x", anchoredPosition.x);
        PlayerPrefs.SetFloat(posKey + ".y", anchoredPosition.y);
        PlayerPrefs.SetFloat(sizeKey + ".x", sizeDelta.x);
        PlayerPrefs.SetFloat(sizeKey + ".y", sizeDelta.y);
        PlayerPrefs.Save();
    }
}
