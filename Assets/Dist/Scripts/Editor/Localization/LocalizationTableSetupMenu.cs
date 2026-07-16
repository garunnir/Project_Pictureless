// ============================================================
// LocalizationTableSetupMenu — UI_ko LocalizationTable 생성/시드
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static class LocalizationTableSetupMenu
{
    const string AssetFolder = "Assets/Dist/Resources/Localization";

    [MenuItem("Dist/Localization/Create Or Refresh UI_ko Table")]
    static void CreateOrRefreshUiKoTable()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Resources"))
            AssetDatabase.CreateFolder("Assets/Dist", "Resources");
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Resources", "Localization");

        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationTable.AssetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, LocalizationTable.AssetPath);
        }

        table.EditorSetEntries(BuildDefaultEntries());
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = table;
        Debug.Log($"[LocalizationTableSetupMenu] Updated {LocalizationTable.AssetPath} ({table.Entries.Count} entries). Resources.Load=\"{LocalizationTable.ResourcesLoadName}\"");
    }

    static List<LocalizationTable.Entry> BuildDefaultEntries()
    {
        return new List<LocalizationTable.Entry>
        {
            Entry("ItemContextMenu.Craft", "제작"),
            Entry("ItemContextMenu.Uncraft", "분해"),
            Entry("ItemContextMenu.MiscGroup", "기타"),
            Entry("ItemContextMenu.UncraftPrefix", "분해: "),
            Entry("ItemContextMenu.CraftBlocked", "재료·도구·스킬 부족"),
            Entry("ItemContextMenu.UncraftBlocked", "분해 불가"),
            Entry("ItemContextMenu.UnknownResult", "?"),
            Entry("RecipeCategory.CC_FOOD", "음식"),
            Entry("RecipeCategory.CC_DRINK", "음료"),
            Entry("RecipeCategory.CC_CHEM", "화학"),
            Entry("RecipeCategory.CC_AMMO", "탄약"),
            Entry("RecipeCategory.CC_WEAPON", "무기"),
            Entry("RecipeCategory.CC_ARMOR", "방어구"),
            Entry("RecipeCategory.CC_ELECTRONIC", "전자"),
            Entry("RecipeCategory.CC_MISC", "기타"),
            Entry("RecipeCategory.CSC_FOOD_MEAT", "육류"),
            Entry("RecipeCategory.CSC_FOOD_VEGGI", "채소"),
            Entry("RecipeCategory.CSC_FOOD_OTHER", "기타 음식"),
            Entry("ItemDamage.1", "손상됨"),
            Entry("ItemDamage.2", "크게 손상됨"),
            Entry("ItemDamage.3", "심하게 손상됨"),
            Entry("ItemDamage.4", "거의 파괴됨"),
            Entry("RecipeKnowledge.Invalid", "Invalid recipe"),
            Entry("RecipeKnowledge.SkillRequired", "스킬 lv{0} 필요"),
            Entry("RecipeKnowledge.BookRequired", "책 필요"),
            Entry("RecipeKnowledge.Locked", "Locked"),
            Entry("Inventory.PrimaryTitle", "Inventory"),
            Entry("Inventory.LootTitle", "Loot"),
            Entry("Inventory.EmptyWeight", "— kg"),
            Entry("Inventory.EmptyVolume", "— L"),
            Entry("Interaction.DoorOpen", "문 열기"),
            Entry("Interaction.DoorClose", "문 닫기"),
            Entry("Tile.UncategorizedCategory", "기타"),
        };
    }

    static LocalizationTable.Entry Entry(string key, string text)
    {
        return new LocalizationTable.Entry { key = key, text = text };
    }
}
#endif
