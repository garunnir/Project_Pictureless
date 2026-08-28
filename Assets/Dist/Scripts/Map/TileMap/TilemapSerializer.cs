using UnityEngine;
using System.IO;

namespace IsoTilemap
{
    public class TileMapSerializer : IMapSerializer
    {


        public MapSaveJsonDto Read(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"Map file not found: {fullPath}");
                return null;
            }

            string json = File.ReadAllText(fullPath);
            MapSaveJsonDto mapData = JsonUtility.FromJson<MapSaveJsonDto>(json);

            if (mapData == null || mapData.tiles == null)
            {
                Debug.LogWarning("Map data is null or invalid.");
                return null;
            }
            if (mapData.wallEdges == null)
                mapData.wallEdges = new System.Collections.Generic.List<WallEdgeSaveData>();
            if (mapData.floorFaces == null)
                mapData.floorFaces = new System.Collections.Generic.List<FloorFaceSaveData>();
            if (mapData.liquidAuthoringFaces == null)
                mapData.liquidAuthoringFaces = new System.Collections.Generic.List<FloorFaceSaveData>();

            // 구 JSON은 물이 floorFaces에 들어 있다. 여기서 한 번 저작 레이어로 옮겨 두면
            // 이후 경로(DtoMapper·시드·bake·에디터 마커)는 liquidAuthoringFaces만 보면 된다.
            MapLiquidAuthoringBake.PromoteLegacyFloorFaces(mapData);
            return mapData;
        }
     


        public void Write(string path, MapSaveJsonDto dto)
        {
            string json = JsonUtility.ToJson(dto, true);
            File.WriteAllText(path, json);
        }
    }
}
