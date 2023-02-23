using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SelectBoxVisualizeEditor : EditorWindow
{
    [MenuItem("Behavior Editor/Editor")]
    static void ShowEditor()
    {
        EditorWindow window = EditorWindow.GetWindow<SelectBoxVisualizeEditor>(typeof(Comments));
        window.minSize = new Vector2(800, 600);
       
    }
    private void OnGUI()
    {
        
    }
}
