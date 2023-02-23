using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Comments))]
public class CustomEditors : Editor
{
    // Start is called before the first frame update
    Comments comments;

    private void OnEnable()
    {
        comments = target as Comments;
    }
    // Update is called once per frame
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        comments.text = GUILayout.TextArea(comments.text);
    }
}
