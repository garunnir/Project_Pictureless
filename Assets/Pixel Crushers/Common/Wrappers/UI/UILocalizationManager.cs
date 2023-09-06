// Copyright (c) Pixel Crushers. All rights reserved.

using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace PixelCrushers.Wrappers
{

    /// <summary>
    /// This wrapper for PixelCrushers.UILocalizationManager keeps references intact if you switch 
    /// between the compiled assembly and source code versions of the original class.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Common/UI/UI Localization Manager")]
    public class UILocalizationManager : PixelCrushers.UILocalizationManager
    {
        private static UILocalizationManager s_instance = null;
        private static bool s_isQuitting = false;

        /// <summary>
        /// Current global instance of UILocalizationManager. If one doesn't exist,
        /// a default one will be created.
        /// </summary>
        new public static UILocalizationManager instance
        {
            get
            {
                if (s_instance == null && !s_isQuitting)
                {
                    s_instance = GameObjectUtility.FindFirstObjectByType<UILocalizationManager>();
                    if (s_instance == null && Application.isPlaying)
                    {
                        var globalTextTable = GameObjectUtility.FindFirstObjectByType<GlobalTextTable>();
                        s_instance = (globalTextTable != null) ? globalTextTable.gameObject.AddComponent<UILocalizationManager>()
                            : new GameObject("UILocalizationManager").AddComponent<UILocalizationManager>();
                    }
                }
                return s_instance;
            }
            set
            {
                s_instance = value;
            }
        }
        public string AddLoc(string text)
        {
            if (Localization.language == Localization.GetLanguage(SystemLanguage.Korean))
            {
                //Debug.Log("lang!");
                text = Myevan.Korean.ReplaceJosa(text);
                //Debug.Log(text);
                return text;
            }
            else
            {
                return text;
            }
        }
        private void Awake()
        {
            currentLanguage = Localization.GetLanguage(SystemLanguage.Korean);
            TextTable.currentLanguageID=textTable.GetLanguageID(currentLanguage);
        }
    }

}
