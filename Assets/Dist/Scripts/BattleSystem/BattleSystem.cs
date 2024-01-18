using Garunnir;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_text;
    [SerializeField] List<ActorSO> m_RedCharactersInput=new List<ActorSO>();
    [SerializeField] List<ActorSO> m_BlueCharactersInput=new List<ActorSO>();
    EquipmentCollectionSO m_Equipment;
    const int m_MaxCount = 6;

    //공방 시스템을 만들어보자
    //적.(같은 액터로 사용)
    //투입은 엑터
    List<BattleActorData> m_Characters = new List<BattleActorData>();

    //어떤 공격방식을 차용할것인가
    //반자동전투.
    //스피드를 기준으로 공격한다.
    //무기에 따라 최종이 변한다.

    class BattleActorData
    {
        public int teamID;
        public Actor actor;
        public float speed;
    }

    private void Awake()
    {
        m_Equipment = GameManager.Instance.GetResourceManager().GetEquipData();
        //엑터를 사용하게 변환
        ConvertSOtoActor();
    }

    void ConvertSOtoActor()
    {
        var count=0;
        foreach (var actor in m_RedCharactersInput) {
            if (count == m_MaxCount) break;
            count++;
            var chara = new BattleActorData();
            chara.teamID = 0;
            chara.actor = actor.actor;
            chara.speed = GetCalSpeed(actor.actor);
            m_Characters.Add(chara);
        }
        count = 0;
        foreach (var actor in m_BlueCharactersInput)
        {
            if (count == m_MaxCount) break;
            count++;
            var chara = new BattleActorData();
            chara.teamID = 1;
            chara.actor = actor.actor;
            chara.speed = GetCalSpeed(actor.actor);
            m_Characters.Add(chara);
        }
    }
    IEnumerator Cor_ActionStart()
    {
        //캐릭터가 행동한다.
        var checkTime = Time.deltaTime;

        while (true)
        {
            checkTime += Time.deltaTime;

        }
        
        yield return null;
    }
    private void GetNextTurn()
    {
        //시간이 흐르고
        //다음 순서의 캐릭터가 행동한다.
    }
    /// <summary>
    /// 스피드 계산
    /// </summary>
    /// <param name="actor">계산할 대상</param>
    /// <returns></returns>
    private float GetCalSpeed(Actor actor)
    {
        //스피드를 가져온다.
        //스피드 계산식은?
        //장비무개-1 체중-1 근력+2 재주 0.5
        //Todo 같은 스피드일 경우 랜덤하게 체크.
        
        int dexvalue=Field.LookupInt(actor.fields, ConstDataTable.Actor.Status.Dex);
        int bodyweight=Field.LookupInt(actor.fields, ConstDataTable.Actor.BodyData.Weight);
        int strvalue=Field.LookupInt(actor.fields,ConstDataTable.Actor.Status.Str);
        float weaponWeight = GameManager.Instance.GetResourceManager().GetWeapon(actor).weight;

        float aSpeed = dexvalue * 0.5f+bodyweight*-1+strvalue*2+weaponWeight*-1;
        
        float speed=aSpeed;
        return speed;
    }


}
