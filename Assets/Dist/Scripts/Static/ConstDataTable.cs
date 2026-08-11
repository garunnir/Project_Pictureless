using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstDataTable
{
    public const string DefalutLang = "ko";
    public class Map
    {
        public const string designation = "Map";
        /// <summary>
        /// 맵번호
        /// </summary>
        public const string ID = "Map.ID";
        /// <summary>
        /// 맵위치
        /// </summary>
        public const string Pos = "Map.PosID";
    }
    public class Actor
    {
        public const int createStart = 10000;//start id to created charactor
        public const int PlayerID = 1;
        public class Skill
        {
            public const string Active = "Skill.Active"; 
            public const string Passive = "Skill.Passive";
        }
        public class Status
        {
            /// <summary>
            /// 성향
            /// </summary>
            public const string Alignment = "Status.Alignment";
            /// <summary>
            /// 힘
            /// </summary>
            public const string Str = "Status.Str";
            /// <summary>
            /// 생명력
            /// </summary>
            public const string Con = "Status.Con";
            /// <summary>
            /// 손재주
            /// </summary>
            public const string Dex = "Status.Dex";
            /// <summary>
            /// 지력
            /// </summary>
            public const string Int = "Status.Int";
            /// <summary>
            /// 영적감수성
            /// </summary>
            public const string Wis = "Status.Wis";
            /// <summary>
            /// 카리스마
            /// </summary>
            public const string Cha = "Status.Cha";
            /// <summary>
            /// 생명력
            /// </summary>
            public const string Hp = "Status.Hp";
        }
        public class BodyData//todo 차후 보디 데이터 각 무개의 합으로 평가한다.
        {
            public const string Weight = "Body.Personalized";
        }
        public class Conversation
        {
            public const string Personalized = "Conv.PersonalizedConversation";
        }
    }
    public class Item
    {
        /// <summary>무기 인덱스 — Dialogue Actor 필드 키 (레거시 Equipment.* 문자열 유지)</summary>
        public const string Weapon = "Equipment.Weapon";
        public const string Equip = "Equipment.Equip";
    }
    public class ActorHStatus
    {

    }
        public class Tile
        {
            const string KeyUncategorizedCategory = "Tile.UncategorizedCategory";

            /// <summary>
            /// category가 비어있는 타일의 기본 분류명
            /// </summary>
            public static string UncategorizedCategory =>
                Loc.Get(KeyUncategorizedCategory);
        }
    public class AssetPath
    {
        public class LocalizeTable
        {
            /// <summary>
            /// 반복 사용되는 캐릭터 대사가 들어있는 SO
            /// </summary>
            public const string ActorBark = "Assets/Dist/Scripts/CustomDS/LocalizeDialogueTable.asset";
            /// <summary>
            /// 로컬라이징 대응 액터이름
            /// </summary>
            public const string ActorName = "Assets/Dist/Scripts/CustomDS/LocalizeActorName.asset";
            /// <summary>
            /// 신규 UI 키 테이블 Resources.Load 경로 (확장자 제외). Loc / LocalizationTable SSOT.
            /// </summary>
            public const string UI = "Localization/UI_ko";
            /// <summary>활성 언어 + 언어별 TMP 폰트. LocalizationBundle.ResourcesLoadName SSOT.</summary>
            public const string Bundle = LocalizationBundle.ResourcesLoadName;
        }
    }
}

