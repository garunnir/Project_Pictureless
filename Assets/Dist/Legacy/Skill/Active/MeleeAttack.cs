using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Garunnir.Runtime.ScriptableObject
{
    [CreateAssetMenu(fileName = "MeleeAttack", menuName = "GameDataAsset/Character/MeleeAttack")]
    public class MeleeAttack : ActiveSkill
    {
        [Header("공격횟수"),Range(0,3),SerializeField]
        int attackrate;
        public override object Excute(Actor targetActor)
        {
            for (int i = 0; i < attackrate; i++)
            {
                DealDamage(targetActor, 10);//todo 데미지는 무기와 스테이터스에 의한 값으로 변경할것.
            }
            return VFXExecute();
        }

        private object VFXExecute()
        {
            //여기선 실행후의 로그를 전달하는것으로 한다.
            //행동했던것을 기록하고 전달.
            //문장은 유동적으로 달라져야 함.
            return log;
        }

        public override void Excute()
        {
            throw new System.NotImplementedException();
        }
    }
}
