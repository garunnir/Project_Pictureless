using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_text;


    //공방 시스템을 만들어보자
    //적.(같은 액터로 사용)
    //투입은 엑터
    Actor[] redTeamInput;
    Actor[] blueTeamInput;

    //어떤 공격방식을 차용할것인가
    //반자동전투.
    //스피드를 기준으로 공격한다.
    //무기에 따라 최종이 변한다.



    void Convert()
    {
        //엑터의 필드정보를 읽어들인다.
        foreach (var actor in redTeamInput)
        {
            //Field.Lookup(actor.fields);
        }
    }
}
