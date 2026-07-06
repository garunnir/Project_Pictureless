
namespace Garunnir.Runtime.ScriptableObject
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    [CreateAssetMenu(fileName = "SkillCollection", menuName = "GameDataAsset/SkillCollection")]
    public class SkillCollectionSO : ScriptableObject
    {
        [Header("스킬")]
        [SerializeField] public ActiveSkill[] skill_Active;
        [SerializeField] public PassiveSkill[] skill_Passive;
    }
}
