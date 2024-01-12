using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Character", menuName = "GameDataAsset/Character")]
public class CharacterSO : ScriptableObject
{
    [SerializeField,Character] Actor actor; 
}
