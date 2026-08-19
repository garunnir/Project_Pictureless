// ============================================================
// WeaponAmmoDuration — 삽탄·장착 소요 시간(초)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class WeaponAmmoDuration
{
    public const float FallbackSeconds = 1f;

    public static float LoadSeconds(ItemData magazine)
    {
        int moves = magazine?.magazine != null ? magazine.magazine.reload_time : 0;
        return MovesToSeconds(moves);
    }

    public static float ClipLoadSeconds(ItemData gun)
    {
        int moves = gun?.gun != null ? gun.gun.reload : 0;
        return MovesToSeconds(moves);
    }

    public static float AttachSeconds(ItemData gun, ItemData magazine)
    {
        int moves = 0;
        if (gun?.gun != null)
            moves = gun.gun.reload;
        if (moves <= 0 && magazine?.magazine != null)
            moves = magazine.magazine.reload_time;
        return MovesToSeconds(moves);
    }

    static float MovesToSeconds(int moves)
    {
        if (moves <= 0)
            return FallbackSeconds;
        return moves / CombatMath.MovesPerSecond;
    }
}
