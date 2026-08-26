// ============================================================
// FishTrapContextLabels — 통발 컨텍스트 메뉴 문구 SSOT
// ============================================================

public static class FishTrapContextLabels
{
    const string KeyDeploy = "ItemContextMenu.DeployTrap";
    const string KeyDeployBlocked = "ItemContextMenu.DeployTrapBlocked";
    const string KeyCollect = "Interaction.CollectTrap";
    const string KeyCollectBlocked = "Interaction.CollectTrapBlocked";

    public static string Deploy =>
        Loc.TryGet(KeyDeploy, out string deploy) ? deploy : "통발 설치";
    public static string DeployBlocked =>
        Loc.TryGet(KeyDeployBlocked, out string blocked) ? blocked : "통발을 설치할 수 없음";
    public static string Collect =>
        Loc.TryGet(KeyCollect, out string collect) ? collect : "수확";
    public static string CollectBlocked =>
        Loc.TryGet(KeyCollectBlocked, out string collectBlocked) ? collectBlocked : "수확할 수 없음";
}
