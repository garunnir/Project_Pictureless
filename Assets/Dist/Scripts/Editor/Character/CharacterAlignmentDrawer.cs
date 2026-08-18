// ============================================================
// CharacterAlignmentDrawer — Status.Alignment Vector2 클릭 위젯 (허브·Inspector 공용)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CharacterAlignmentDrawer
{
    const float WidgetSize = 100f;
    const float HalfSize = 50f;
    const float CursorSize = 20f;
    const float CursorHalf = 10f;
    const string AlignmentTexturePath = "Custom/Alignment.png";
    const string CursorTexturePath = "Dialogue System/Event.png";

    static Texture2D s_alignmentTexture;
    static Texture2D s_alignmentCursorTexture;

    public static void Draw(SerializedProperty alignmentProp)
    {
        if (alignmentProp == null)
            return;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(
            "Alignment",
            "Status.Alignment (editor only, no runtime consumer yet)");

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Box(GUIContent.none, GUILayout.Height(WidgetSize), GUILayout.Width(WidgetSize));
        Rect boxRect = GUILayoutUtility.GetLastRect();

        s_alignmentTexture ??= EditorGUIUtility.Load(AlignmentTexturePath) as Texture2D;
        if (s_alignmentTexture != null)
            GUI.DrawTexture(boxRect, s_alignmentTexture);

        Vector2 alignment = alignmentProp.vector2Value;
        HandleAlignmentInput(boxRect, ref alignment);

        s_alignmentCursorTexture ??= EditorGUIUtility.Load(CursorTexturePath) as Texture2D;
        if (s_alignmentCursorTexture != null)
        {
            float cursorX = boxRect.x - alignment.x * HalfSize + HalfSize - CursorHalf;
            float cursorY = boxRect.y - alignment.y * HalfSize + HalfSize - CursorHalf;
            GUI.DrawTexture(
                new Rect(cursorX, cursorY, CursorSize, CursorSize),
                s_alignmentCursorTexture);
        }

        alignmentProp.vector2Value = alignment;
        EditorGUILayout.LabelField($"X/Y: {alignment.x:0.##} / {alignment.y:0.##}");
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    static void HandleAlignmentInput(Rect boxRect, ref Vector2 alignment)
    {
        Event current = Event.current;
        if (current.type != EventType.MouseDown || current.button != 0)
            return;

        if (current.mousePosition.x < boxRect.x ||
            current.mousePosition.x > boxRect.x + boxRect.width ||
            current.mousePosition.y < boxRect.y ||
            current.mousePosition.y > boxRect.y + boxRect.height)
        {
            return;
        }

        Vector2 local = current.mousePosition - new Vector2(boxRect.x + HalfSize, boxRect.y + HalfSize);
        local = -local / HalfSize;
        if (local.magnitude > 1f)
            local = local.normalized;

        alignment = local;
        GUI.changed = true;
        current.Use();
    }
}
#endif
