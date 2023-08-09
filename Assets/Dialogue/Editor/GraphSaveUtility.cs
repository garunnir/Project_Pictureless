using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialougeGraphView _targetGraphView;
    private DialougeContainer _containerCache;
    public const string _saveKey = "EditerWindow_DialogueEditor_Key";
    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<DialogueNode> Nodes => _targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList();
    public static GraphSaveUtility  Getinstance(DialougeGraphView targetGraphView)
    {
        return new GraphSaveUtility
        {
            _targetGraphView = targetGraphView
        };
    }
    public void SaveGraph(string fileName)
    {
        if (!Edges.Any()) return;//if there are no edges(no connection) then return

        var dialogueContainer = ScriptableObject.CreateInstance<DialougeContainer>();

        var connectedPorts = Edges.Where(x => x.input.node != null).ToList();

        for(var i=0; i < connectedPorts.Count; i++)
        {
            var outPutNode = connectedPorts[i].output.node as DialogueNode;
            var inPutNode= connectedPorts[i].input.node as DialogueNode;

            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outPutNode.GUID,
                PortName = connectedPorts[i].output.portName,
                TargetNodeGuid = inPutNode.GUID
            });
        }
        foreach(var dialogueNode in Nodes.Where(node => !node.EntryPoint))
        {
            dialogueContainer.DialogueNodeData.Add(new DialogueNodeData 
            {
                Guid=dialogueNode.GUID,
                DialogueText=dialogueNode.DialogueText,
                Position=dialogueNode.GetPosition().position,
                Title=dialogueNode.title
                

            });
        }

        
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets","Resources");
        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/SODialogue/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }
    public bool LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<DialougeContainer>("SODialogue/"+ fileName);
        if (_containerCache == null)
        {
            EditorUtility.DisplayDialog("파일을 찾을 수 없음", "대상 대화 그래프가 존재하지 않습니다.", "확인");
            PlayerPrefs.DeleteKey(GraphSaveUtility._saveKey);
            return false;
        }
        else
        {
            PlayerPrefs.SetString(GraphSaveUtility._saveKey, fileName);
            ClearGraph();
            CreateNodes();
            ConnectNodes();
            return true;
        }

    }

    private void ConnectNodes()
    {
        for(var i = 0; i < Nodes.Count; i++)
        {
            var connections = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[i].GUID).ToList();
            for(var j = 0; j < connections.Count; j++)
            {
                var targetNodeGuid = connections[j].TargetNodeGuid;
                var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);
                LinkNodes(Nodes[i].outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]);

                targetNode.SetPosition(new Rect(_containerCache.DialogueNodeData.First(x => x.Guid == targetNodeGuid).Position, _targetGraphView.DefaultNodeSize));

            }
        }
    }

    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge
        {
            output = output,
            input = input
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);
        _targetGraphView.Add(tempEdge);
    }

    private void CreateNodes()
    {
        foreach(var nodeData in _containerCache.DialogueNodeData)
        {
            var tempNode = _targetGraphView.CreateDialogueNode(nodeData.Title,nodeData.DialogueText,Vector2.zero);
            tempNode.GUID = nodeData.Guid;
            _targetGraphView.AddElement(tempNode);

            var nodePorts = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
            nodePorts.ForEach(x => _targetGraphView.AddChoicePort(tempNode, x.PortName));
        }
    }


    private void ClearGraph()
    {
        //set entry points guid back from the save. discard existing guid.
        if (_containerCache.NodeLinks!=null&& _containerCache.NodeLinks.Count() < 0) return;
        Nodes.Find(x => x.EntryPoint).GUID = _containerCache.NodeLinks[0].BaseNodeGuid;

        foreach(var node in Nodes)
        {
            if (node.EntryPoint) continue;

            //remove edges that connected to this node
            Edges.Where(x => x.input.node == node).ToList().ForEach(edge=>_targetGraphView.RemoveElement(edge));
            //then remove the node
            _targetGraphView.RemoveElement(node);
        }
    }
    public void ClearNodes()
    {
        Nodes.Find(x => x.EntryPoint);
        foreach (var node in Nodes)
        {
            if (node.EntryPoint) continue;

            //remove edges that connected to this node
            Edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));
            //then remove the node
            _targetGraphView.RemoveElement(node);
        }

    }
    public bool FileExist(string fileName)
    {
        return File.Exists($"Assets/Resources/SODialogue/{fileName}.asset");
    }
}
