// ============================================================
// ArmAnimHandClipsDrawer — HandClips 클립 옆 Speed (동작 줄·Catalog)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ArmAnimSlotCatalog.HandClips))]
public sealed class ArmAnimHandClipsDrawer : PropertyDrawer
{
    static readonly GUIContent SpeedContent = new GUIContent("Speed");
    const int HandCount = 3;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float gap = EditorGUIUtility.standardVerticalSpacing;
        return HandCount * line + (HandCount - 1) * gap;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Object host = property.serializedObject != null
            ? property.serializedObject.targetObject
            : null;
        WeaponAnimClipSpeeds speeds = host != null
            ? WeaponAnimClipSpeedsHost.GetExisting(host)
            : null;

        float line = EditorGUIUtility.singleLineHeight;
        float gap = EditorGUIUtility.standardVerticalSpacing;
        Rect row = new Rect(position.x, position.y, position.width, line);
        DrawHand(host, ref speeds, row, "Left", property.FindPropertyRelative("leftBase"));
        row.y += line + gap;
        DrawHand(host, ref speeds, row, "Right", property.FindPropertyRelative("rightBase"));
        row.y += line + gap;
        DrawHand(host, ref speeds, row, "TwoHand", property.FindPropertyRelative("twoHandBase"));
        EditorGUI.EndProperty();
    }

    static void DrawHand(
        Object host,
        ref WeaponAnimClipSpeeds speeds,
        Rect total,
        string handLabel,
        SerializedProperty clipProp)
    {
        if (clipProp == null)
            return;

        Rect field = EditorGUI.PrefixLabel(total, new GUIContent(handLabel));
        AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;
        if (clip == null)
        {
            EditorGUI.PropertyField(field, clipProp, GUIContent.none);
            return;
        }

        float gap = EditorGUIUtility.standardVerticalSpacing;
        float speedLabelW = EditorStyles.label.CalcSize(SpeedContent).x;
        float speedFieldW = EditorGUIUtility.fieldWidth;
        float speedBlock = speedLabelW + gap + speedFieldW;
        float minClip = EditorGUIUtility.fieldWidth;
        if (speedBlock + gap + minClip > field.width)
            speedBlock = Mathf.Max(speedFieldW, field.width - minClip - gap);

        Rect speedArea = new Rect(field.xMax - speedBlock, field.y, speedBlock, field.height);
        field.xMax = speedArea.x - gap;

        EditorGUI.PropertyField(field, clipProp, GUIContent.none);
        clip = clipProp.objectReferenceValue as AnimationClip;
        if (clip == null)
            return;

        float speed = speeds != null
            ? speeds.GetSpeed(clip)
            : WeaponAnimClipSpeeds.DefaultSpeed;
        Rect labelRect = new Rect(speedArea.x, speedArea.y, speedLabelW, speedArea.height);
        Rect valueRect = new Rect(
            labelRect.xMax + gap,
            speedArea.y,
            Mathf.Max(0f, speedArea.xMax - labelRect.xMax - gap),
            speedArea.height);
        EditorGUI.LabelField(labelRect, SpeedContent);
        float nextSpeed = EditorGUI.FloatField(valueRect, speed);
        if (nextSpeed < 0f)
            nextSpeed = 0f;
        if (Mathf.Approximately(nextSpeed, speed))
            return;

        if (speeds == null)
            speeds = WeaponAnimClipSpeedsHost.GetOrCreate(host);
        if (speeds == null)
            return;

        Undo.RecordObject(speeds, "Edit clip speed");
        speeds.SetSpeed(clip, nextSpeed);
        EditorUtility.SetDirty(speeds);
    }
}
#endif
