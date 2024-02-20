
namespace Garunnir.Runtime.ScriptableObject
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using PixelCrushers.DialogueSystem;
    using System.Text;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    public abstract class SkillSO : ScriptableObject
    {
        public string displayName;
        protected StringBuilder log=new StringBuilder();
#if UNITY_EDITOR
        private void OnDisable()
        {
            EditorUtility.SetDirty(this);//임시
            AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
        //스킬을 담고 실행할때 작동한다.
        public abstract void Excute();
        public void DealDamage(Actor actor,int damage)
        {
            PlusIntValue(actor,ConstDataTable.Actor.Status.Hp, damage);
            RecordLog($"{actor.Name}에 {damage} 피해를 입혔다.");
        }
        public void PlusIntValue(Actor actor,string key,int value)
        {
            int intval = Field.LookupInt(actor.fields, key);
            Field.SetValue(actor.fields, key, intval + value);
        }
        public void GetEquipment()
        {
            //장비정보를 가져온다.
        }
        public void RecordLog(string value)
        {
            //행동정보 기록한다.
            log.AppendLine(value);
        }
    }
}
