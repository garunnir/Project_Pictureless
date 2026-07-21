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
        public int storage;
        public int environmental_protection;
        public int material_thickness;
        public bool power_armor;
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
        public int durability;
        public int clip_size;
        public int reload;
        public int burst;
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
    }

    [Serializable]
    public sealed class AmmoDetailData
    {
        public string ammo_type;
        public int damage;
        public int pierce;
        public int range;
        public int dispersion;
        public int recoil;
        public int count;
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
