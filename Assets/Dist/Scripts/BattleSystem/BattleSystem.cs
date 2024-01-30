using Garunnir;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using Garunnir.Runtime.ScriptableObject;
public class BattleSystem : MonoBehaviour
{
    #region variables
    [SerializeField] ScrollRect m_srect;
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
    #endregion
    #region 컨테이너
    /// <summary>
    /// 여기서 순서등에 활용하는 전용 컨테이너.
    /// </summary>
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
            return 100/this.speed;
        }
    }
    #endregion
    #region 구현부
    void TestAction()
    {
        var sortedData = FindNexts(0, m_Characters, 100);
        foreach (var data in sortedData)
        {
            ExecuteActor(data);
        }
    }
    private void Awake()
    {
        m_Equipment = GameManager.Instance.GetResourceManager().GetEquipData();
        //엑터를 사용하게 변환
        ConvertSOtoActor();
        TestAction();
    }
    #endregion
    #region 순서 세팅 관련
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
        
        Dictionary<float, List<BattleActorData>> tosortDic= new Dictionary<float, List<BattleActorData>>();
        
        foreach (var data in datas)
        {
            int turn = CalTurnAtTime(time, data);
            for (int i=turn; data.GetSpeed()*i < endtime; i++)//딕셔너리에 시간으로 삽입한다.
            {
                if (data.GetSpeed() * i == 0) continue;

                if (!tosortDic.ContainsKey(data.GetSpeed()*i))
                {
                    tosortDic.Add(data.GetSpeed()*i, new List<BattleActorData>());
                }
                
                tosortDic[data.GetSpeed()*i].Add(data);
            }
        }
        List<BattleActorData> nexts =SortingData(tosortDic);
        return nexts;
    }
    /// <summary>
    /// 스피드 순으로 줄세우기
    /// </summary>
    /// <param name="rawnexts"></param>
    /// <returns></returns>
    private List<BattleActorData> SortingData(Dictionary<float, List<BattleActorData>> rawnexts)
    {
        var rawdata = rawnexts.OrderBy(x=>x.Key).ToList();
        List<BattleActorData> result=new List<BattleActorData>();
        foreach (var item in rawdata)//todo 스피드가 곂칠때 분류기준이 필요.
        {
            foreach (var inner in item.Value)
            {
                result.Add(inner);
            }
        }
        return result;
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
        var weapon= GameManager.Instance.GetResourceManager().GetWeapon(actor);
        float weaponWeight = weapon?weapon.weight:0;

        float aSpeed = dexvalue * 0.5f+bodyweight*-1+strvalue*2+weaponWeight*-1;
        
        float speed=aSpeed;
        return speed;
    }

    #endregion
    #region 가공부
    /// <summary>
    /// 액터의 턴행동
    /// </summary>
    /// <param name="data"></param>
    private void ExecuteActor(BattleActorData data)
    {
        UseLogPanel($"액션: {data.actor.Name} 속도: {data.GetSpeed()}");
    }
    void UseLogPanel(string value)
    {
        var text=LeanPool.Spawn(m_text, m_srect.content);
        text.text = value;
    }
    #endregion

}
