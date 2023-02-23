using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialougeGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(150, 200);

    public Blackboard Blackboard;
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
    private NodeSearchWindow _searchWindow;
    public DialougeGraphView(EditorWindow editorWindow)
    {
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));
        SetupZoom(ContentZoomer.DefaultMinScale,ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        AddElement(GenerateEntryPointNode());
        AddSearchWindow(editorWindow);
    }
    
    private void AddSearchWindow(EditorWindow editorWindow)
    {
        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.init(editorWindow,this);
        nodeCreationRequest = context =>
          SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);//어느 클래스의 노드크리에이선 리퀘스트가 작동하면 서치윈도우~를 작동시킨다.
    }

    public void AddPropertyToBlackBoard()
    {

    }


    private Port GeneratePort(DialogueNode node,Direction portDirection,Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node)
                compatiblePorts.Add(port);
        });
        return compatiblePorts;
    }

    private DialogueNode GenerateEntryPointNode()
    {
        var node = new DialogueNode
        {
            title = "시작",
            GUID =Guid.NewGuid().ToString(),
            DialogueText ="시작지점",
            EntryPoint=true
        };

        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "다음";
        node.outputContainer.Add(generatedPort);


        node.capabilities &= ~Capabilities.Movable;//node.c=node.c & ~Ca~;
        node.capabilities &= ~Capabilities.Deletable;//그래프에서 기능을 제한한다 움직일수없고 삭제할수없다

        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(x: 100, y: 200, width: 100, height: 150));
        return node;
    }
    public void CreateNode(String nodeName,string dialougeTxt,Vector2 position)
    {

        AddElement(CreateDialogueNode(nodeName,dialougeTxt,position));
    }
    public DialogueNode CreateDialogueNode(string nodeName,string dialougeTxt,Vector2 position)
    {
        var dialogueNode = new DialogueNode
        {
            title = nodeName,
            DialogueText = dialougeTxt,
            GUID = Guid.NewGuid().ToString()
        };
        var inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "입력";
        dialogueNode.inputContainer.Add(inputPort);

        dialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));


        var button = new Button(() => { AddChoicePort(dialogueNode); }) ;
        button.text = "새 선택지";

        var titleTextField = new TextField();
        var textField = new TextField(string.Empty);
        textField.multiline = true;
        textField.RegisterValueChangedCallback(evt =>
        {
            dialogueNode.DialogueText = evt.newValue;
            Debug.Log("in");
        });
        textField.SetValueWithoutNotify(dialogueNode.DialogueText);

        //목표. 타이틀 변경가능하게하기
        /*
         변경가능하게하려면
         1. 변경하는곳찾기
        2.텍스트필드의 값을 넣기
        3.표시되지는 않게하기

    목표 버튼 누르면 타이틀로 넘어가게하기
        1.버튼생성
        2.버튼워크생성
        3.버튼누르면 텍스트창 뜨고안뜨고하게하기

        지금 생성버튼은 왼쪽인데 창은 오른쪽에 생성된다. 어떻게 해야할까
        
        다이얼로그 텍스트안에 포커스가 들어가면 아이엠이켜고싶음 
        1.포커싱을 캐치
        2.아이엠이 켜기
         */

        var titleButton = new Button(()=> { ChangeTitle(dialogueNode,titleTextField); });
        dialogueNode.titleContainer.Add(titleButton);
        dialogueNode.titleContainer.Add(button);
        dialogueNode.mainContainer.Add(textField);
        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
        dialogueNode.SetPosition(new Rect(position, DefaultNodeSize));

        return dialogueNode;
    }
    string tempStr;
    public void ChangeTitle(DialogueNode dialogueNode, TextField textField)
    {
        if (dialogueNode.titleContainer.Contains(textField))
        {
            dialogueNode.titleContainer.Remove(textField);
            if (string.IsNullOrEmpty(textField.value))
            {
                dialogueNode.title = tempStr;

            }
            else
            {
                dialogueNode.title = textField.value;
                Debug.Log(textField.value);
            }
            Input.imeCompositionMode = IMECompositionMode.Auto;
        }
        else
        {
            textField.StretchToParentSize();
            dialogueNode.titleContainer.Insert(1, textField);
            tempStr = dialogueNode.title;
            dialogueNode.title = string.Empty;

            Input.imeCompositionMode = IMECompositionMode.On;
            //dialogueNode.titleContainer.Add(textField);
        }
    }
    public void AddChoicePort(DialogueNode dialogueNode,string overriddenPortName="")
    {
        var generatedPort = GeneratePort(dialogueNode,Direction.Output);

        var oldLabel = generatedPort.contentContainer.Q<Label>("type");
        generatedPort.contentContainer.Remove(oldLabel);

        var outputPortCount = dialogueNode.outputContainer.Query("connector").ToList().Count;
        generatedPort.portName = $"Choice {outputPortCount}";

        var choicePortName = string.IsNullOrEmpty(overriddenPortName) ? $"Choice{outputPortCount + 1}" : overriddenPortName;

        var textField = new TextField
        {
            name = string.Empty,
            value = choicePortName
            
        };
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label("   "));
        generatedPort.contentContainer.Add(textField);

        var deleteButton = new Button(() => RemovePort(dialogueNode, generatedPort))
        {
            text = "X"
        };
        generatedPort.contentContainer.Add(deleteButton);

        generatedPort.portName = choicePortName;

        dialogueNode.outputContainer.Add(generatedPort);
        dialogueNode.RefreshPorts();
        dialogueNode.RefreshExpandedState();
    }

    private void RemovePort(DialogueNode dialogueNode, Port generatedPort)
    {
        var targetEdge = edges.ToList().Where(x => x.output.portName == generatedPort.portName && x.output.node == generatedPort.node);//지금 버튼을 누른 포트에 연결된 엣지를 죄다검색

        //오류 연결안된상태에서도 타겟엣지에니가 나타난다.
        if (targetEdge.Any())//Remove edge if port is connected
        {
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(targetEdge.First());
        }


        //delete the port whether or not connected
        dialogueNode.outputContainer.Remove(generatedPort);
        dialogueNode.RefreshPorts();
        dialogueNode.RefreshExpandedState();
    }
    public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
    {
        var property = new ExposedProperty();
        property.PropertyName = exposedProperty.PropertyName;
        property.PropertyValue = exposedProperty.PropertyValue;
        ExposedProperties.Add(property);

        var container = new VisualElement();
        var blackboardField = new BlackboardField { text = property.PropertyName, typeText = "string property" };
        container.Add(blackboardField);

        var propertyValueTextField = new TextField("Value")
        {
            value = property.PropertyValue
        };
        propertyValueTextField.RegisterValueChangedCallback(evt => {
            var changingPropertyIndex = ExposedProperties.FindIndex(x =>x.PropertyName == property.PropertyName);

            ExposedProperties[changingPropertyIndex].PropertyValue = evt.newValue;


        });
        var blackBoardValueRow = new BlackboardRow(blackboardField, propertyValueTextField);
        container.Add(blackBoardValueRow);

        Blackboard.Add(container);
    }
}

