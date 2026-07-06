namespace Garunnir.Runtime.ScriptableObject
{
    using PixelCrushers.DialogueSystem;
    using System.Collections;
    using System.Collections.Generic;
    
    public abstract class ActiveSkill : SkillSO
    {
        //ActorSO에 장착되어 활용될 스킬.
        //항상 그랬듯이 필드 파라미터로 들어갈 것.
        //에디터에선 표기됨.
        //사용은 베틀시스템이서 콜하고 
        public abstract object Excute(Actor targetActor);
        //스킬 실구현부에선 어떻게 할 것인가...
        //에셋 참조하는 주소를 직접적으로 저장한다면?
    }
}
