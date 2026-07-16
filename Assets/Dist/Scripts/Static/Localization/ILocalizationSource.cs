// ============================================================
// ILocalizationSource — 키 → 문구 조회 백엔드 (SO / TextTable 교체용)
// ============================================================

public interface ILocalizationSource
{
    bool TryGet(string key, out string text);
}
