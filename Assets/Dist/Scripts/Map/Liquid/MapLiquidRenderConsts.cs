// ============================================================
// MapLiquidRenderConsts — 액체 수면 렌더 상수 SSOT (가시 임계·리메시 예산·셰이더 키)
// ============================================================

namespace IsoTilemap
{
    public static class MapLiquidRenderConsts
    {
        /// <summary>
        /// 이 Fill01 이하는 물이 없는 것으로 본다. 시뮬 Level 1(<see cref="MapLiquidConsts.MlPerLevel"/>)보다
        /// 낮게 잡아, 부은 물이 시뮬에는 있는데 화면에는 없는 구간이 생기지 않게 한다.
        ///
        /// "위 칸이 물인가"(= 아래 칸이 잠겼는가) 판정도 같은 임계를 쓴다. 임계를 나누면 위 칸 잔량이
        /// 그 사이에 있을 때 아래 칸이 잠기지 않은 것으로 판정되어 층 사이에 구멍이 뚫린다.
        /// 수위가 튀지 않는 이유는 solver가 위로 밀어 올리는 조건이 "아래 칸이 이미 capMl 초과"뿐이라,
        /// 위 칸에 물이 있으면 아래 칸은 사실상 항상 가득 차 있기 때문이다.
        /// </summary>
        public const float MinVisibleFill01 = 0.002f;

        /// <summary>
        /// 수면을 셀 바닥에서 최소 이만큼 띄운다(cellSize 배율) — 같은 앵커 바닥 메시와의 투명 정렬 가드.
        ///
        /// 전역 transparency sort는 <b>Default</b>(직교 카메라 거리)여야 한다. Y축 Custom Axis로 바꾸면
        /// 같은 층 타일(<c>gridPos.y</c> 동일)이 정렬 동점이 되고, 타일 셰이더는 ZWrite Off·sortingOrder 0이라
        /// 앞뒤가 무작위가 된다. RenderQueue 승격도 위층 타일을 덮으므로 금지.
        /// 수면은 이 lift와 <see cref="SurfaceTopInset01"/> 기하 오프셋만으로 바닥 메시와 분리한다.
        /// </summary>
        public const float SurfaceMinLift01 = 0.05f;

        /// <summary>
        /// 가득 찬 수면을 셀 천장에서 이만큼 아래로(relative 0~1) — 위층 바닥면(y=천장)과 coplanar 정렬 방지.
        /// </summary>
        public const float SurfaceTopInset01 = 0.04f;

        /// <summary>이미 그려진 청크를 한 프레임에 다시 메시화할 수 있는 최대 개수 — 흐름 중 스파이크 분산.</summary>
        public const int MaxChunkRemeshPerFrame = 4;

        /// <summary>한 프레임에 새로 메시화할 청크 최대 개수. 맵 로드·카메라 이동 시 팝인과 프리즈의 절충.</summary>
        public const int MaxChunkBuildPerFrame = 12;

        /// <summary>스트리밍이 꺼진 맵에서 렌더러가 쓸 메시 분할 크기. 스트리밍 시에는 TileMapChunkStreamer.ChunkSize가 SSOT.</summary>
        public const int FallbackChunkSize = 16;

        /// <summary>셰이더 시간을 감는 주기(초). 누적 초를 그대로 넘기면 노이즈 해시의 float 정밀도가 무너진다.</summary>
        public const float ShaderTimeWrapSeconds = 3600f;

        /// <summary>수면 셰이더가 읽는 전역 시간(월드 배속 반영) 프로퍼티 이름.</summary>
        public const string GlobalTimeProperty = "_MapLiquidTime";

        public const string SurfaceShaderName = "Dist/MapLiquidSurface";

        /// <summary>Inspector 미지정 시 로드할 머티리얼. Resources 경유라 빌드에도 셰이더가 포함된다.</summary>
        public const string SurfaceMaterialResourcePath = "Map/MapLiquidSurface";
    }
}
