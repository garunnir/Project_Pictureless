// ============================================================
// CatalogBrowserFields — Catalog 브라우저 공용 필드 헬퍼
// ============================================================

using System;
using System.IO;
using Garunnir.Runtime.Gameplay.Data;
using UnityEditor;
using UnityEngine;

static class CatalogBrowserFields
{
    public static void EditLocalizedItemName(
        string itemId,
        CatalogDataSession session,
        Action invalidateFilter)
    {
        DisplayLanguage lang = session.ActiveDisplayLanguage;
        string langCode = DisplayLanguageCodes.ToCode(lang);
        string current = ItemNameTable.TryGetRaw(itemId, lang, out string raw)
            ? raw
            : string.Empty;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Name ({langCode})", GUILayout.Width(120));
        string newVal = EditorGUILayout.TextField(current ?? string.Empty);
        EditorGUILayout.EndHorizontal();

        if (newVal != (current ?? string.Empty))
        {
            ItemNameTable.Set(itemId, lang, newVal);
            invalidateFilter?.Invoke();
        }
    }

    public static void EditLocalizedItemDescription(string itemId, CatalogDataSession session)
    {
        DisplayLanguage lang = session.ActiveDisplayLanguage;
        string langCode = DisplayLanguageCodes.ToCode(lang);
        string current = ItemNameTable.TryGetRaw(ItemLocaleKind.Description, itemId, lang, out string raw)
            ? raw
            : string.Empty;

        EditorGUILayout.LabelField($"Description ({langCode})");
        string newVal = EditorGUILayout.TextArea(current ?? string.Empty, GUILayout.MinHeight(48f));
        if (newVal != (current ?? string.Empty))
            ItemNameTable.Set(ItemLocaleKind.Description, itemId, lang, newVal);
    }

    public static void EditField(string label, ref string value, Action markDirty)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        string newVal = EditorGUILayout.TextField(value ?? "");
        if (newVal != (value ?? ""))
        {
            value = newVal;
            markDirty?.Invoke();
        }

        EditorGUILayout.EndHorizontal();
    }

    public static void EditIntField(string label, ref int value, Action markDirty)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        int newVal = EditorGUILayout.IntField(value);
        if (newVal != value)
        {
            value = newVal;
            markDirty?.Invoke();
        }

        EditorGUILayout.EndHorizontal();
    }

    public static void EditFloatField(string label, ref float value, Action markDirty)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        float newVal = EditorGUILayout.FloatField(value);
        if (!Mathf.Approximately(newVal, value))
        {
            value = newVal;
            markDirty?.Invoke();
        }

        EditorGUILayout.EndHorizontal();
    }

    public static void ReadField(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        EditorGUILayout.SelectableLabel(
            value ?? "—",
            EditorStyles.wordWrappedLabel,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();
    }

    public static void ReadFieldWithCopy(string label, string value)
    {
        string text = value ?? string.Empty;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        EditorGUILayout.SelectableLabel(
            string.IsNullOrEmpty(text) ? "—" : text,
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (GUILayout.Button("Copy", GUILayout.Width(48)))
            EditorGUIUtility.systemCopyBuffer = text;
        EditorGUILayout.EndHorizontal();
    }

    public static void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Rect texRect = sprite.textureRect;
        var uv = new Rect(
            texRect.x / sprite.texture.width,
            texRect.y / sprite.texture.height,
            texRect.width / sprite.texture.width,
            texRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv);
    }
}
