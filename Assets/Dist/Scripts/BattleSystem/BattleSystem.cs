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
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class BattleSystem : MonoBehaviour
{
    #region variables
    [SerializeField] ScrollRect m_srect;
    [SerializeField] TextMeshProUGUI m_text;
    [SerializeField] List<AssetReferenceT<ActorSO>> m_RedCharactersInput = new List<AssetReferenceT<ActorSO>>();
    [SerializeField] List<AssetReferenceT<ActorSO>> m_BlueCharactersInput =new List<AssetReferenceT<ActorSO>>();
    [SerializeField] UISelectBtnsPopup selectionPop;
    EquipmentCollectionSO equipment;
    const int m_MaxCount = 6;

    //공방 시스템을 만들어보자
    //적.(같은 액터로 사용)
    //투입은 엑터
    /// <summary>
    /// 배틀 시스템에 사용할 단위
    /// </summary>
    List<BattleActorData> m_Characters = new List<BattleActorData>();
    //어떤 공격방식을 차용할것인가
    //반자동전투.
    //스피드를 기준으로 공격한다.
    //무기에 따라 최종이 변한다.
    [Header("vfx module")]
    [SerializeField] TargetIndicateHandeler trailHandler;
    #endregion
    #region 컨테이너
    /// <summary>
    /// 여기서 순서등에 활용하는 전용 컨테이너.
    /// </summary>
    class BattleActorData
    {
        public int teamID;
        public Actor actor;
        public Transform ObjTransform;
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
    #region Input>>>구현부<<<
    private void Update()
    {
    }
    /// <summary>
    /// 특정시간안의 전체 턴들을 계산한다.
    /// </summary>
    void TurnAction()
    {
        var sortedData = FindNexts(0, m_Characters, 100);
        StartCoroutine(Cor_TurnPlay(sortedData));
        //foreach (var data in sortedData)
        //{
        //    ExecuteActor(data);
        //}
    }
    bool readyNext=false;
    private IEnumerator Cor_TurnPlay(List<BattleActorData>datas)
    {
        foreach (var data in datas)
        {
            ExecuteActor(data);
            PopSelections(data.actor);
            yield return new WaitUntil(() => {return readyNext; });//선택지 조작이 있을때까지 대기
            readyNext = false;
        }
        yield return null;
    }
    /// <summary>
    /// 액터의 턴행동
    /// </summary>
    /// <param name="data"></param>
    private void ExecuteActor(BattleActorData data)
    {
        //List<Field> actorfield = data.actor.fields;
        //엑터 SO에 셋팅된 행동을 한다.
        //스킬 발동기준.
        //수동일 때
        //선택 UI를 표시한다. 이 UI는 엑티브 스킬들을 포함한다.
        //자동일 때
        //이전에 선택됐던 그대로 발동한다. 없을시 첫번째 스킬을 대상으로 한다. 여기서 저장된것은 세이브데이터에 포함되어야 함.
        //자동 모드전환
        //컨트롤 키를 누르거나 스킵버튼을 누르고 있는동안.

        //스테미너는 행동할때마다 소모되며 턴을 넘기면 보충됨.
        //공격을 자동회피해도 소모됨
        //스테미너가 부족하면 부족한 수치에 비례하여 회피할 수 없다.
        //스테미너의 최대치는 근력에서 유래하고 소모량은 재주로 감소시킬 수 있음.

        //영혼력의 최대치는 지혜로 상승하며
        //충전속도도 지혜로 상승.


        //명중판정 후 회피판정.
        //이동력이 없으면 회피할 수 없음.
        //이동력 계산은 힘 무게 재주가 관여함.

        UseLogPanel($"액션: {data.actor.Name} 속도: {data.GetSpeed()}");
    }
    public delegate UnityAction ButtonAction(Actor actor,ActiveSkill skill);
    ButtonAction obj;
    /// <summary>
    /// 스킬 선택지창. 팝업
    /// </summary>
    //private void PopSelections(params ActiveSkill[] skills)
    //{
    //    selectionPop.ClearAll();
    //    obj = delegate (ActiveSkill activeSkill)
    //    {
    //        UseLogPanel(activeSkill.displayName);
    //        readyNext = true;
    //        activeSkill.Excute();
    //        m_srect.normalizedPosition = Vector2.zero;

    //        return null;
    //    };
    //    foreach (var item in skills)
    //    {
    //        if (item == null) continue;
    //        Button button = selectionPop.CreateBtns(item.displayName);
    //        button.onClick.RemoveAllListeners();
    //        button.onClick.AddListener(() => { obj(item);});
    //    }
    //}
    private void PopSelections(Actor actor)
    {
        ActiveSkill[] skills = ActorSO.GetASkillAll(actor);
        selectionPop.ClearAll();
        obj = delegate (Actor actor, ActiveSkill activeSkill)
        {
            //여기에선 선택 화살표가 등장해야 한다.
            
            //trailHandler.DrawIndicate()
            UseLogPanel(activeSkill.Excute(actor).ToString());
            readyNext = true;
            m_srect.normalizedPosition = Vector2.zero;
            return null;
        };
        foreach (var item in skills)
        {
            if (item == null) continue;
            Button button = selectionPop.CreateBtns(item.displayName);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { obj(actor,item); });
        }


    }
    Coroutine inputcor;
    void SelectionStart(BattleActorData actorData) => inputcor = StartCoroutine(Cor_SelectionStart(actorData));
    IEnumerator Cor_SelectionStart(BattleActorData actorData)
    {
        while (true)
        {
            InfiniteLoopDetector.Run();
            //선택 활성화한다.
            trailHandler.DrawIndicate(actorData.ObjTransform.position,InputManager.RayCast().transform.position,10);//선택 화살표 활성화
            yield return new WaitUntil(InputManager.click);
        }
    }
        //private bool CheckBtnSelected(Button[] watchers)
        //{
        //    bool check = false;
        //}
        private void BtnAction(ActiveSkill skill)
    {

    }

    #endregion
    #region 순서 세팅 관련
    /// <summary>
    /// 어드레서블 데이터로드 이니셜라이징
    /// </summary>
    /// <param name="rtn"></param>
    /// <returns></returns>
    IEnumerator Cor_InitSrc(Action rtn)
    {
        var count = 0;
        var completeCount = 0;
        foreach (var asset in m_RedCharactersInput)
        {
            if (count == m_MaxCount) break;
            ActorSO actor;
            asset.LoadAssetAsync().Completed += (x) =>
            {
                actor = x.Result;
                var chara = new BattleActorData();
                chara.teamID = 0;
                chara.actor = actor.actor;
                chara.SetSpeed(GetCalSpeed(actor.actor));
                m_Characters.Add(chara);
                completeCount++;
            };
            count++;
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitUntil(() => completeCount == count);
        Debug.Log("<color=#336633>BattleAssetLoad(1/2)</color>");
        completeCount =count = 0;
        foreach (var asset in m_BlueCharactersInput)
        {
            if (count == m_MaxCount) break;
            ActorSO actor;
            asset.LoadAssetAsync().Completed += (x) =>
            {
                actor = x.Result;
                var chara = new BattleActorData();
                chara.teamID = 0;
                chara.actor = actor.actor;
                chara.SetSpeed(GetCalSpeed(actor.actor));
                m_Characters.Add(chara);
                completeCount++;
            };
            count++;
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitUntil(() => completeCount == count);
        Debug.Log("<color=#336633>BattleAssetLoad(2/2)</color>");
        rtn.Invoke();
        Debug.Log("<color=#338833>BattleAssetLoadDone!</color>");
    }
    /// <summary>
    /// 액터 어드레서블 불러오기
    /// </summary>
    void ConvertSOtoActor()
    {
        StartCoroutine(Cor_InitSrc(() => { TurnAction(); }));
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
                InfiniteLoopDetector.Run();
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

    void UseLogPanel(string value)
    {
        var text=LeanPool.Spawn(m_text, m_srect.content);
        text.text = value;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_srect.content.transform);
    }
    #region BehaviourMathod
    private void Awake()
    {
        equipment = GameManager.Instance.GetResourceManager().GetEquipData();
        //엑터를 사용하게 변환
        ConvertSOtoActor();
        //TestAction();
    }
    #endregion
    #endregion

}
