// Copyright (c) Pixel Crushers. All rights reserved.

using Garunnir;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.Wrappers
{

    /// <summary>
    /// This wrapper class keeps references intact if you switch between the 
    /// compiled assembly and source code versions of the original class.
    /// </summary>
    [HelpURL("https://pixelcrushers.com/dialogue_system/manual2x/html/dialogue_system_controller.html")]
    [AddComponentMenu("Pixel Crushers/Dialogue System/Misc/Dialogue System Controller")]
    public class DialogueSystemController : PixelCrushers.DialogueSystem.DialogueSystemController
    {
        [SerializeField] UIMapController map;
        //맵 정보에서 타겟을 찾는다
        private new void Awake()
        {
            base.Awake();
            map=FindObjectOfType<UIMapController>();
            map.MapEntered += Map_MapEntered;
            GameManager.Instance.ResourceLoadDoneEvent+=()=> StartConversation(databaseManager.DefaultDatabase.conversations[0].Title);
        }

        private void Map_MapEntered(int arg0, int arg1)
        {
            MapEntry entry=masterDatabase.GetMapContainer(arg0).GetMapEntry(arg1);
            List<Actor> actor = masterDatabase.actors.FindAll(x => x.mapPosID.Item1 == arg0&&x.mapPosID.Item2==arg1);
            if(actor.Count > 0)
            {

            }
        }
    }

}
