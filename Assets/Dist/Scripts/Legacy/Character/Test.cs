using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using System;
using Garunnir;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public class Test : MonoBehaviour
{
    private void Start()
    {
        AddNewAsset(DialogueManager.masterDatabase.actors);
    }
    private T AddNewAsset<T>(List<T> assets) where T : Asset, new()
    {
        Template template = Template.FromDefault();
        T asset = new T();
        int highestID = DialogueManager.masterDatabase.baseID - 1;
        assets.ForEach(a => highestID = Mathf.Max(highestID, a.id));
        asset.id = Mathf.Max(1, highestID + 1);
        asset.fields = template.CreateFields(template.actorFields);
        asset.Name = string.Format("New {0} {1}", typeof(T).Name, asset.id);
        assets.Add(asset);
        print(DialogueManager.masterDatabase.actors.Last().Name);
        return asset;
    }
}
