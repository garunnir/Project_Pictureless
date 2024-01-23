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
        float speed;

        public void SetSpeed(float speed)
        {
            this.speed = speed;
        }
        public float GetSpeed()
        {
            return this.speed;
        }
    }

    private void Awake()
    {
        m_Equipment = GameManager.Instance.GetResourceManager().GetEquipData();
        //엑터를 사용하게 변환
        ConvertSOtoActor();
        TestAction();
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
            chara.SetSpeed(GetCalSpeed(actor.actor));
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
            chara.SetSpeed(GetCalSpeed(actor.actor));
            m_Characters.Add(chara);
        }
    }
    int CalTurnAtTime(float time,float speed)
    {
        return Mathf.RoundToInt(time / speed);
    }
    int CalTurnAtTime(float time, BattleActorData data)
    {
        return Mathf.RoundToInt(time / data.GetSpeed());
    }
    BattleActorData FindNext(float time,List<BattleActorData> datas)//특정 타임에서 바로뒤에올 대상을 찾는다.
    {
        BattleActorData next=null;
        float nexttime = float.PositiveInfinity;
        foreach (var data in datas)
        {
            if (next == null) { next = data;}
            else
            {
                var aTime = CalTurnAtTime(time, data.GetSpeed()) * data.GetSpeed();
                if (nexttime > aTime)
                {
                    nexttime = aTime;
                    next = data;
                }
            }
        }
        return next;
    }
    List<BattleActorData> FindNexts(float time, List<BattleActorData> datas,float endtime)//특정 타임에서 바로뒤에올 대상을 찾는다.
    {
        Dictionary<float, BattleActorData> tosortDic= new Dictionary<float, BattleActorData>();
        foreach (var data in datas)
        {
            int turn = CalTurnAtTime(time, data);

            for (int i=turn; data.GetSpeed()*i < endtime; i++)//딕셔너리에 시간으로 삽입한다.
            {
                tosortDic.Add(data.GetSpeed(), data);
            }
        }
        var nexts= tosortDic.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,y=>y.Value).Values.ToList();
        
        return nexts;
    }
    void TestAction()
    {
        var sortedData = FindNexts(0, m_Characters, 100);
        foreach (var data in sortedData)
        {
            Debug.LogWarning($"액션: {data.actor.Name} 속도: {data.GetSpeed()}");
        }
    }
    //IEnumerator Cor_ActionStart()
    //{
    //    //캐릭터가 행동한다.
    //    //쿨타임 식으로 돌아야 함.
    //    //그건 계산량이 많은것 같다
    //    //공격 턴수, 다음 턴의 시간량

    //    BattleActorData actor = null;
    //    int turn = 0;
    //    float neednextTime = actor.GetSpeed() * turn;

    //    float timeNow = Time.time;

    //    if(timeNow > neednextTime)
    //    {
    //        turn++;
    //        TurnAction(actor);
    //    }
    //    //시간을 와방 쑤셔넣어도 순서에 맞춰서 출력하게 하고싶다.
    //    //다음 차례가 빠른 친구를 파악할 수 있을까?
    //    //배열에 순서대로 쑤셔넣으면?
    //    //유동적으로 배열을 추가할 수 있으려면
    //    //질문: 이 방식대로 채워넣었고 시간은 이만큼이다. 다음에 올 인원은?


    //    var checkTime = Time.deltaTime;

    //    while (true)
    //    {
    //        checkTime += Time.deltaTime;

    //    }
        


    //    yield return null;
    //}
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
