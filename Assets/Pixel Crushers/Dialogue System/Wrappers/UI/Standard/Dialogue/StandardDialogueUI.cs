// Copyright (c) Pixel Crushers. All rights reserved.

using Garunnir;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem.Wrappers
{

    /// <summary>
    /// This wrapper class keeps references intact if you switch between the 
    /// compiled assembly and source code versions of the original class.
    /// </summary>
    [HelpURL("http://www.pixelcrushers.com/dialogue_system/manual2x/html/standard_dialogue_u_i.html")]
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/Standard UI/Dialogue/Standard Dialogue UI")]
    public class StandardDialogueUI : PixelCrushers.DialogueSystem.StandardDialogueUI
    {
        //[SerializeField] private RawImage bg; 
        public override void Awake()
        {
            base.Awake();
            print("~~~");
            Localization.language=Localization.GetLanguage(SystemLanguage.Korean);
        }
    }

}
