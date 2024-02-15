
namespace Garunnir.Runtime.ScriptableObject
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    public class SkillSO : ScriptableObject
    {
        public string displayName;
#if UNITY_EDITOR
        private void OnDisable()
        {
            EditorUtility.SetDirty(this);//임시
            AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
        //스킬을 담고 실행할때 작동한다.
        public virtual void Excute()
        {

        }
    }

}
