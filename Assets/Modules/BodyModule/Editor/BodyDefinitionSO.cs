using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "BodyDefinition", menuName = "Body/Definition")]
public class BodyDefinitionSO : ScriptableObject
{
    // 여러 루트 허용
    public List<BodyPartSO> roots = new();
}