using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine.UIElements;

public class NodeSearchWindow : ScriptableObject,ISearchWindowProvider
{
    private DialougeGraphView _graphView;
    private EditorWindow _window;
    private Texture2D _indentationIcon;

    public void init(EditorWindow window, DialougeGraphView graphView)
    {
        _graphView=graphView;
        _window = window;

        _indentationIcon = new Texture2D(1, 1);
        _indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
        _indentationIcon.Apply();
    }
    List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeGroupEntry(new GUIContent("Dialogue Node"), 1),
            new SearchTreeEntry(new GUIContent("Dialogue Node",_indentationIcon))
            {
                userData=new DialogueNode(),level=2
            }
        };
        return tree;
    }

    bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        var worldMousePosition = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent, context.screenMousePosition - _window.position.position);
        var localMousePosition = _graphView.contentContainer.WorldToLocal(worldMousePosition);
        switch(SearchTreeEntry.userData)
        {
            case DialogueNode dialougeNode:
                _graphView.CreateNode("Dialogue Node","Null",localMousePosition);
                return true;

            default:
                return false;
        }
        return true;

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
