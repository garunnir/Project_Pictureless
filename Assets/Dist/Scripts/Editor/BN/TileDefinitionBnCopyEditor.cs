// ============================================================
// TileDefinitionBnCopyEditor — TileDefinition Inspector에 BN flags 복사
// ============================================================

using System.Collections.Generic;
using IsoTilemap;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileDefinition))]
public sealed class TileDefinitionBnCopyEditor : Editor
{
    string _tileBnId = "";
    string _tileBnCopyStatus;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var def = (TileDefinition)target;
        if (def == null)
            return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("BN terrain copy-from (선택)", EditorStyles.boldLabel);
        _tileBnId = EditorGUILayout.TextField("bnId", _tileBnId ?? "");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("BN flags 복사", GUILayout.Width(120)))
            TryCopyTileFlagsFromBnTerrain(def);
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(_tileBnCopyStatus))
            EditorGUILayout.HelpBox(_tileBnCopyStatus, MessageType.Info);
    }

    void TryCopyTileFlagsFromBnTerrain(TileDefinition def)
    {
        if (!TryGetGameplayDataTerrainFlags(_tileBnId, out List<string> bnFlags))
        {
            _tileBnCopyStatus =
                "GameplayData에 terrain이 없거나 해당 bnId를 찾지 못해 복사를 건너뜁니다.";
            return;
        }

        if (def.flags == null)
            def.flags = new List<string>();

        if (bnFlags != null)
        {
            for (int i = 0; i < bnFlags.Count; i++)
            {
                string flag = bnFlags[i];
                if (string.IsNullOrEmpty(flag) || TileFlags.HasFlag(def, flag))
                    continue;
                def.flags.Add(flag);
            }
        }

        EditorUtility.SetDirty(def);
        _tileBnCopyStatus = "BN terrain flags를 복사했습니다.";
    }

    static bool TryGetGameplayDataTerrainFlags(string bnId, out List<string> flags)
    {
        flags = null;
        if (string.IsNullOrEmpty(bnId))
            return false;

        Garunnir.Runtime.Gameplay.Data.TerrainData terrain = GameplayData.GetTerrain(bnId);
        if (terrain == null)
            return false;

        flags = terrain.flags;
        return true;
    }
}
