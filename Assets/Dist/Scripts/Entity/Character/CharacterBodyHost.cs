// ============================================================
// CharacterBodyHost — 엔티티별 ICharacterBody 소유 (플레이어·NPC)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterBodyHost : MonoBehaviour
{
    [SerializeField] bool _useGameplayDataBody;
    [SerializeField] int _seedStrength = 8;
    [SerializeField] bool _prototypeSeed;

    ICharacterBody _body;

    public ICharacterBody Body
    {
        get
        {
            if (_body == null)
                EnsureBody();
            return _body;
        }
    }

    void Awake() => EnsureBody();

    void EnsureBody()
    {
        if (_body != null)
            return;

        if (_useGameplayDataBody)
        {
            _body = GameplayData.Body;
            return;
        }

        _body = CharacterBody.CreateHumanDefault(_seedStrength, _prototypeSeed);
    }

    public void BindBody(ICharacterBody body)
    {
        _body = body;
    }
}
