
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class BodyPartSO : ScriptableObject
{
    public string partName = "NewPart";
    public float maxHp = 10f;
    public bool vital = false;

    // 계층 관계
    public List<BodyPartSO> children = new();
    public List<BodyPartSO> parent = new();
}


public class BodyPart
{
    public string partName = "NewPart";
    public float maxHp = 10f;
    public bool vital = false;

    // 계층 관계
    public List<BodyPart> children = new();
    public List<BodyPart> parent = new();

    public BodyPart FromDef(BodyPartSO bodyPartSO)
    {
        // Using a dictionary to track converted objects to handle graphs and prevent infinite loops.
        return FromDefRecursive(bodyPartSO, new Dictionary<BodyPartSO, BodyPart>());
    }

    private static BodyPart FromDefRecursive(BodyPartSO so, Dictionary<BodyPartSO, BodyPart> visited)
    {
        if (so == null)
        {
            return null;
        }

        // If we've already converted this SO, return the existing BodyPart instance.
        if (visited.TryGetValue(so, out BodyPart existingPart))
        {
            return existingPart;
        }

        // Create a new BodyPart and copy the properties.
        var newPart = new BodyPart
        {
            partName = so.partName,
            maxHp = so.maxHp,
            vital = so.vital
        };

        // Add the new part to the visited dictionary before recursing to handle cycles.
        visited[so] = newPart;

        // Recursively convert and add children.
        foreach (var childSO in so.children)
        {
            var childPart = FromDefRecursive(childSO, visited);
            if (childPart != null)
            {
                newPart.children.Add(childPart);
                // Set the parent relationship on the child.
                if (!childPart.parent.Contains(newPart))
                {
                    childPart.parent.Add(newPart);
                }
            }
        }

        return newPart;
    }
}