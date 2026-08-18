// ============================================================
// HudPopupVisibility — HUD 팝업 개별 표시 PlayerPrefs SSOT
// ============================================================

using System;
using System.Collections.Generic;

public static class HudPopupVisibility
{
    const string Prefix = "HudPopup.";
    const string Suffix = ".visible";
    static readonly List<HudLayoutParticipant> _participants = new(8);



    public static event Action Changed;



    public static bool IsVisible(string participantId)

    {

        if (string.IsNullOrEmpty(participantId))

            return true;



        string key = Prefix + participantId + Suffix;

        if (!UnityEngine.PlayerPrefs.HasKey(key))

            return true;



        return UnityEngine.PlayerPrefs.GetInt(key, 1) != 0;

    }



    public static void SetVisible(string participantId, bool visible)

    {

        if (string.IsNullOrEmpty(participantId))

            return;



        string key = Prefix + participantId + Suffix;

        int next = visible ? 1 : 0;

        if (UnityEngine.PlayerPrefs.HasKey(key) &&

            UnityEngine.PlayerPrefs.GetInt(key) == next)

            return;



        UnityEngine.PlayerPrefs.SetInt(key, next);

        UnityEngine.PlayerPrefs.Save();

        ApplyToParticipants(participantId);

        Changed?.Invoke();

    }



    internal static void Register(HudLayoutParticipant participant)

    {

        if (participant == null || _participants.Contains(participant))

            return;



        _participants.Add(participant);

    }



    internal static void Unregister(HudLayoutParticipant participant)

    {

        if (participant == null)

            return;



        _participants.Remove(participant);

    }



    static void ApplyToParticipants(string participantId)

    {

        for (int i = _participants.Count - 1; i >= 0; i--)

        {

            HudLayoutParticipant participant = _participants[i];

            if (participant == null)

            {

                _participants.RemoveAt(i);

                continue;

            }



            if (participant.ParticipantId == participantId)

                participant.ApplyStoredVisibility();

        }
    }
}