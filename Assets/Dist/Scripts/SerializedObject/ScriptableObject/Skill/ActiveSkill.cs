namespace Garunnir.Runtime.ScriptableObject
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    public class ActiveSkill : SkillSO
    {
        //ActorSO에 장착되어 활용될 스킬.
        //항상 그랬듯이 필드 파라미터로 들어갈 것.
        //에디터에선 표기됨.
        //사용은 베틀시스템이서 콜하고 
        public override void Excute()
        {
            base.Excute();
        }
    }

}