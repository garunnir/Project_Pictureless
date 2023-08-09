using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialougeGraph : EditorWindow
{
    MiniMap _currentMinimap;
    private DialougeGraphView _graphView;
    private string _fileName="새 이야기";
    [MenuItem("그래프/대화그래프")]
    public static void OpenDialogueGraphWindow()
    {
        var window = GetWindow<DialougeGraph>();
        window.titleContent = new GUIContent(text:"대화 그래프");

    }
    private void OnInspectorUpdate()
    {
        if(_currentMinimap != null)
        {
            Rect tmprect=_currentMinimap.GetPosition();
            tmprect.position = new Vector2(position.xMax-position.x - tmprect.width, position.yMax-position.y - tmprect.height);
            _currentMinimap.SetPosition(tmprect);
        }
    }
    private void OnEnable()
    {
        _graphView = new DialougeGraphView(this)
        {
            name = "Dialoge Graph"
        };
        
        if (PlayerPrefs.HasKey(GraphSaveUtility._saveKey))
        {
            var saveUtility = GraphSaveUtility.Getinstance(_graphView);
            if (saveUtility.LoadGraph(PlayerPrefs.GetString(GraphSaveUtility._saveKey)))
            {
                _fileName = PlayerPrefs.GetString(GraphSaveUtility._saveKey);
            }
            
        }

        maxSize = new Vector2( Screen.currentResolution.width, Screen.currentResolution.height );
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
        GenerateToolbar();
        GenerateMiniMap();
        GenerateBlackBoard();
    }
    

    private void GenerateBlackBoard()
    {
        var blackBoard = new Blackboard(_graphView);
        blackBoard.Add(new BlackboardSection {title= "Exposed Properties" });
        blackBoard.addItemRequested = _blackboard =>{_graphView.AddPropertyToBlackBoard(new ExposedProperty());};
        blackBoard.SetPosition(new Rect(10, 30,200,300));
        _graphView.Add(blackBoard);
        _graphView.Blackboard = blackBoard;
    }

    private void ConstructGraphView()
    {
        _graphView = new DialougeGraphView(this)
        {
            name = "대화 그래프"
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }
    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var fileNameTextField = new TextField(label: "파일 이름");
        var saveUtility = GraphSaveUtility.Getinstance(_graphView);
        fileNameTextField.SetValueWithoutNotify(_fileName);
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt=> _fileName = evt.newValue);
        toolbar.Add(fileNameTextField);
        toolbar.Add(new Button(() => RequestDataOperation(true)) { text="데이터 세이브"});
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "데이터 로드" });
        toolbar.Add(new Button(() => {
            saveUtility.ClearNodes(); 
            fileNameTextField.SetValueWithoutNotify("새 이야기");
            fileNameTextField.MarkDirtyRepaint();
        } ) {text="클리어" });


        rootVisualElement.Add(toolbar);
    }
    private void GenerateMiniMap()
    {
        var miniMap = new MiniMap { anchored=true};
        _currentMinimap = miniMap;
        //var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x-10,30));
        //var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(1,1));
        miniMap.SetPosition(new Rect(position.width, 30, 200, 140));
        
        _graphView.Add(miniMap);

    }
    private void RequestDataOperation(bool save)
    {
        if (string.IsNullOrEmpty(_fileName))
        {
            EditorUtility.DisplayDialog("존재하지 않는 파일 이름입니다.", "유효한 파일 이름을 입력해 주세요.", "확인");
            return;
        }
        var saveUtility = GraphSaveUtility.Getinstance(_graphView);
        if (save)
        {
            if(saveUtility.FileExist(_fileName))//파일이 있으면 덮어쓰기 확인
            {
                if(EditorUtility.DisplayDialog("덮어쓰기", "덮어쓰게 됩니다만 괜찮겠습니까?", "네", "아니오"))
                {
                    saveUtility.SaveGraph(_fileName);
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                saveUtility.SaveGraph(_fileName);
            }
        }
        else
        {
            saveUtility.LoadGraph(_fileName);
        }
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }
}
