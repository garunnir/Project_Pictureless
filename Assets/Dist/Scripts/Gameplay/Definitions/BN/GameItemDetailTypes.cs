// ============================================================
// GameItemDetailTypes — ItemData 통합 게임 디테일 nested POCO
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    [Serializable]
    public sealed class ArmorDetailData
    {
        public List<string> covers;
        public int coverage;
        public int encumbrance;
        public int max_encumbrance;
        public int warmth;
        /// <summary>총 포켓 용량(ml). BN legacy storage / pockets 합.</summary>
        public int storage;
        /// <summary>선택: 포켓별 volume_ml + draw moves. 비어 있으면 storage만 사용.</summary>
        public List<ArmorPocketData> pockets;
        public int environmental_protection;
        public int material_thickness;
        public bool power_armor;
        /// <summary>BN armor layer (NORMAL/UNDER/OUTER/…). Empty → GearConstants.DefaultArmorLayer.</summary>
        public string layer;
        /// <summary>BN sided — bilateral pair slot; up to MaxSidedPerLayer on same part+layer.</summary>
        public bool sided;
    }

    /// <summary>BN/DDA pocket 조각. moves&gt;0이면 초 변환 후 InventoryTransferDuration handling에 가산.</summary>
    [Serializable]
    public sealed class ArmorPocketData
    {
        public int volume_ml;
        public int moves;
    }

    [Serializable]
    public sealed class GunMagazineGroup
    {
        public string ammo_type;
        public List<string> magazines;
    }

    [Serializable]
    public sealed class GunDetailData
    {
        public string skill;
        public List<string> ammo;
        public int ranged_damage;
        public int range;
        public int dispersion;
        public int recoil;
        /// <summary>미조준 가산. aim01=1이면 0.</summary>
        public int sight_dispersion;
        /// <summary>RMB 조준 조임 속도. 0=즉시 풀조준.</summary>
        public int aim_speed;
        /// <summary>반동 킥 배율. 0=배율 1.</summary>
        public int handling;
        public int durability;
        public int clip_size;
        public int reload;
        public int burst;
        /// <summary>BN gun.magazines — 허용 탄창 id. 비면 장착 불가(클립만).</summary>
        public List<GunMagazineGroup> magazines;
    }

    [Serializable]
    public sealed class ToolDetailData
    {
        public int max_charges;
        public int initial_charges;
        public int charges_per_use;
        public int turns_per_charge;
        public List<string> ammo;
        public string revert_to;
    }

    [Serializable]
    public sealed class ComestibleDetailData
    {
        public int calories;
        public int quench;
        public int fun;
        public float spoils_in_minutes;
        public int charges;
        public int healthy;
        public int stim;
        public string addiction_type;
        public int addiction_potential;
        /// <summary>BN vitamins id→amount. Empty if none.</summary>
        public Dictionary<string, int> vitamins;
    }

    /// <summary>Consume-only BN use_action (heal / consume_drug). Flattened scalars.</summary>
    [Serializable]
    public sealed class UseActionData
    {
        public string type;
        public int heal_amount;
        public string effect_id;
        public int duration;
    }

    [Serializable]
    public sealed class AmmoDetailData
    {
        public string ammo_type;
        /// <summary>BN damage.damage_type (bullet/bash/cut/…). Empty if source was a bare int.</summary>
        public string damage_type;
        public int damage;
        public int pierce;
        public int range;
        public int dispersion;
        public int recoil;
        public int count;
        public int shot_damage;
        public int projectile_count;
        public int shot_spread;
        public List<string> effects;
        public string casing;
        public int loudness;
    }

    [Serializable]
    public sealed class MagazineDetailData
    {
        public List<string> ammo_type;
        public int capacity;
        public string default_ammo;
        public int reliability;
        public int reload_time;
    }

    [Serializable]
    public sealed class BookDetailData
    {
        public int intelligence;
        public int fun;
        public int chapters;
        public float read_time_minutes;
    }

    [Serializable]
    public sealed class ContainerDetailData
    {
        public bool seals;
        public bool watertight;
        public bool preserves;
    }

    [Serializable]
    public sealed class ProficiencyReq
    {
        public string proficiency;
        public bool required;
        public float time_multiplier;
    }
}
