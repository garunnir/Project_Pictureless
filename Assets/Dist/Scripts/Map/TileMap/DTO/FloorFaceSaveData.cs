using System;
using UnityEngine;

namespace IsoTilemap
{
    [Serializable]
    public class FloorFaceSaveData
    {
        public int x;
        public int y;
        public int z;
        /// <summary>+Y 면.</summary>
        public byte face;
        public string prefabId;

        /// <summary>
        /// liquidAuthoringFaces 전용. true면 Play 시드 직후 해당 액체 셀을 flow dirty로 넣는다.
        /// floorFaces·구 JSON 누락 시 false(정지 수면).
        /// </summary>
        public bool simulateFlow;

        /// <summary>
        /// <see cref="MapSaveJsonDto.floorFaces"/> 좌표 → walkable.
        /// schema &lt; <see cref="MapSaveSchema.FloorWalkableCoords"/> 이면 레거시 CellBelow 앵커.
        /// </summary>
        public Vector3Int ResolveWalkableFromFloorFaceSave(int schemaVersion) =>
            schemaVersion >= MapSaveSchema.FloorWalkableCoords
                ? new Vector3Int(x, y, z)
                : new Vector3Int(x, y + 1, z);

        /// <summary><see cref="MapSaveJsonDto.liquidAuthoringFaces"/> — 항상 CellBelow 앵커.</summary>
        public FloorFaceKey ToFloorFaceKeyForLiquidAuthoring() =>
            new FloorFaceKey(new Vector3Int(x, y, z), FloorFace.PosY);

        public FloorFaceKey ToFloorFaceKeyForFloorTileSave(int schemaVersion) =>
            FloorFaceKey.ForWalkableCell(ResolveWalkableFromFloorFaceSave(schemaVersion));
    }
}
