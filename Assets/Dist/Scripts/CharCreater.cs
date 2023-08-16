using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharCreater : MonoBehaviour
{
    public DialogueDatabase dialogueDatabase;
    
    int cacheLastIdNum = DataConfig.createStart;
    // Start is called before the first frame update
    void Start()
    {
        Create();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void Create()
    {
        Actor actor=new Actor();
        //필드정보입력
        int createID=cacheLastIdNum+1;
        string name = "default";
        //런타임에 액터정보를 추가할 수 있을까? 시도해본다.
        createID=EditDuplicated(createID);
        actor.Name = name;
        actor.id = createID;

        dialogueDatabase.actors.Add(actor);
        //추가는 가능하지만..?
    }
    int EditDuplicated(int checkId)
    {
        //생각해야 하는 점. 중복체크할때 다음번호를 챙겨야 하나?
        //마지막으로 등록된 번호를 찾고 다음번호를 주는게 더 스마트 한것같은디..
        //지워질 경우도 고려해야 하는데..
        //아이디는 고유번호다
        //태어나고 사망처리는 될지언정 그 번호가 공란이 될 일은 없?을것이다.
        //하지만 최대의 자유도는 부여되어야 한다 하지만 int는 일정범위가 넘어가면 오버플로할것이다.
        //하지만 그 범위를 초과하려면 국가경영시뮬레이션은 되야할것이다 ㅋㅋ
        //확장성을 생각한다면 지우는게 맞지만..
        //그건 나중에 생각해도 늦지않다 지금은 신속하게 완성하는게 우선이니까.
        //다른 사람들은 생성 제거할때 어떤 방식을 활용하는걸까?
        //리스트에 아이디를 추가할때 번호를 다 검색해야만 하는건가?
        //고유 npc 0~9999 생성 10000~;
        
        if (dialogueDatabase.actors.Find(x => x.id == checkId) == null)
        {
            cacheLastIdNum=checkId;
            return checkId;
        }
        else
        {
            return EditDuplicated(checkId++);
        }
    }
}
