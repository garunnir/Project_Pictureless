// Copyright (c) Pixel Crushers. All rights reserved.

using Garunnir;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace PixelCrushers.DialogueSystem.Wrappers
{
    /// <summary>
    /// This wrapper class keeps references intact if you switch between the 
    /// compiled assembly and source code versions of the original class.
    /// </summary>
    [HelpURL("https://pixelcrushers.com/dialogue_system/manual2x/html/dialogue_system_controller.html")]
    [AddComponentMenu("Pixel Crushers/Dialogue System/Misc/Dialogue System Controller")]
    public class DialogueSystemController : PixelCrushers.DialogueSystem.DialogueSystemController
    {
        public List<Actor> mapActorCache;
        public Conversation basicConv;
        enum Execute
        {
            /// <summary>
            /// 조사하기
            /// </summary>
            Investigate=6,
            /// <summary>
            /// 대화하기
            /// </summary>
            Conversation=4,
        }
        
        [SerializeField] UIMapController map;
        public UIMapController GetMapController=>map;
        //맵 정보에서 타겟을 찾는다
        private new void Awake()
        {
            base.Awake();
            map=FindObjectOfType<UIMapController>();
            map.MapEntered += Map_FindCharInMap;
            GameManager.Instance.ResourceLoadDoneEvent += () =>
            {
                Init();
                //StartConversation(basicConv.Title);
                StartConversation(masterDatabase.GetConversation(2).Title);
            };
        }
        void Init()
        {
            basicConv = masterDatabase.GetConversation(1);
        }
        private void SelectionEnable(Execute execute,bool boolean)
        {
            DialogueEntry dialogueEntry = GetBasicDialogueEntry(execute);
            dialogueEntry.conditionsString = boolean?"true":"false";
        }
        private DialogueEntry GetBasicDialogueEntry(Execute execute)
        {
            return basicConv.GetDialogueEntry((int)execute);
        }
        private void MoveToEntry(DialogueEntry entry)
        {
            conversationController.GotoState(conversationModel.GetState(entry));
        }
        public void AskToChar(string value)
        {
            Actor actor = null;
            Conversation conversation = masterDatabase.GetConversation(conversationModel.conversationTitle);
            string keyword = value;
            DialogueEntry entry = conversation.dialogueEntries.Find(x => x.Title == keyword);
            if (entry != null)
            {
                DialogueManager.StartConversation(conversationModel.conversationTitle, null, null, entry.id);
            }
            else
            {
                InterceptDialogue();
                //아돈노 대사 출력
                //
            }
            //캐릭터한테 키워드로 말건다
            //캐릭터에 해당키워드가 존재하면 다음으로 넘어가고 아니면 잘 모르겠다는 대사를 출력시킨다.
            //키워드에 맞는 대화로 이동한다.
        }
        public void InterceptDialogue()
        {
            Actor actor = null;
            //엑터가 대화중에 갑툭튀한다.
            CharacterInfo actorInfo = conversationModel.GetCharacterInfo(5);
            CharacterInfo listenerInfo = conversationModel.GetCharacterInfo(1);
            DialogueEntry entry= currentConversationState.subtitle.dialogueEntry;
            FormattedText formattedText = FormattedText.Parse("모루겟소요", masterDatabase.emphasisSettings);
            var prev=currentConversationState.subtitle;
            Subtitle subtitle = new Subtitle(actorInfo, listenerInfo, formattedText, "SetContinueMode(true);", "", entry);
            conversationView.dialogueUI.ShowSubtitle(subtitle);
            conversationModel.ForceNextStateToLinkToEntry(entry);
            //conversationController.currentState
            conversationView.FinishedSubtitleHandler += (x,y) => { Debug.LogError(x); };
        }
        /// <summary>
        /// 기본 선택창 표시
        /// </summary>
        private void StartConvPopBasicSelection()
        {
            StartConversation(basicConv.Title);
        }
        public void Map_FindCharInMap(MapEntry entry)
        {
            if (mapActorCache == null)
                mapActorCache = FindActorOnMap(entry);
            bool finded = mapActorCache != null||mapActorCache.Count == 0;
            SelectionEnable(Execute.Conversation, finded);
            if(finded) Map_CharSelect(entry);
            conversationView.SelectedResponseHandler += ChangeBtn;
            UpdateResponses();
        }

        private void ChangeBtn(object sender, SelectedResponseEventArgs e)
        {
            UpdateResponses();
            conversationView.SelectedResponseHandler-= ChangeBtn;
        }
        public void Map_CharSelect(MapEntry entry)
        {
            if(mapActorCache==null||mapActorCache.Count==0)
            mapActorCache = FindActorOnMap(entry);
            if(mapActorCache.Count > 0)
            {
                Subtitle subtitle = null;
                DialogueEntry dentry = GetBasicDialogueEntry(Execute.Conversation);
                dentry.outgoingLinks.Clear();
                List<string> strings=mapActorCache.ConvertAll(x=>x.Name);
                AddActorsToResponse(dentry, mapActorCache);
            }
        }

        private void AddActorsToResponse(DialogueEntry dentry, List<Actor> mapActor)
        {
            for (int i = 0; i < mapActor.Count; i++)
            {
                DialogueEntry created=AddResponse(dentry, mapActor[i].Name,"None()");
                int ownidx = mapActor[i].conversationIdx;
                DialogueEntry copyTarget = databaseManager.DefaultDatabase.conversations[ownidx].GetFirstDialogueEntry();
                CopyLink(created, copyTarget);
            }
        }
        private void CopyLink(DialogueEntry created,DialogueEntry source)
        {
            created.outgoingLinks = new List<Link>(source.outgoingLinks);
            foreach (Link link in created.outgoingLinks)
            {
                link.originDialogueID = created.id;
                link.originConversationID = created.conversationID;
            }
        }
        void GenFakeResponse()
        {
            //가짜 리스폰스를 런타임에 가공해서 넣는다.
            //기존 리스폰스는 무조건 다음 선택지를 보여주게 세팅되어 있다.
            //이거를 인터셉트 하려고 했더니 안되네?
            //런타임에 선택지를 생성할 방법이 있을것 같은데..
            //conversationController.
            Conversation conv= basicConv;



        }
        private Link NewLink(DialogueEntry origin, DialogueEntry destination, ConditionPriority priority = ConditionPriority.Normal)
        {
            var newLink = new Link();
            newLink.originConversationID = origin.conversationID;
            newLink.originDialogueID = origin.id;
            newLink.destinationConversationID = destination.conversationID;
            newLink.destinationDialogueID = destination.id;
            newLink.isConnector = (origin.conversationID != destination.conversationID);
            newLink.priority = priority;
            return newLink;
        }
        //Conversation CreateConv()
        //{
        //    var fakeConversation = new Conversation();
        //    fakeConversation.id = -1;
        //    fakeConversation.fields = new List<Field>();
        //    fakeConversation.fields.Add(new Field("Title", "런타임실행", FieldType.Text));
        //    var entry = new DialogueEntry();
        //    entry.conversationID = fakeConversation.id;
        //    entry.id = 0;
        //    entry.fields = new List<Field>();
        //    entry.DialogueText = " ";
        //    entry.Sequence = "None()";
        //    entry.outgoingLinks.Add(new Link(fakeConversation.id, 0, fakeConversation.id, 1));
        //    fakeConversation.dialogueEntries.Add(entry);
        //    entry = new DialogueEntry();
        //    entry.conversationID = fakeConversation.id;
        //    entry.fields = new List<Field>();
        //    entry.id = 1;
        //    entry.DialogueText = " ";
        //    entry.Sequence = "Delay(2)";
        //    fakeConversation.dialogueEntries.Add(entry);
        //    return fakeConversation;
        //}
        DialogueEntry CreateEntry(int convid,string text,string sequence)
        {
            var entry = new DialogueEntry();
            entry.fields = new List<Field>();
            entry.ActorID = entry.ConversantID = 1;
            entry.id = basicConv.dialogueEntries.Last().id+1;
            basicConv.dialogueEntries.Add(entry);
            entry.conversationID = convid;
            entry.DialogueText = text;
            entry.Sequence = sequence;
            DialogueLua.GetSimStatus(entry.conversationID, entry.id);//simstatus등록
            //DialogueLua.GetSimStatus(1, 1);//simstatus등록
            return entry;
        }
        void AddResponse(DialogueEntry mother,string[] item)
        {
            mother.outgoingLinks.Clear();
            for (int i = 0; i < item.Length; i++)
            {
                AddResponse(mother, item[i], "OpenCustomResponse()");
            }
        }
        DialogueEntry AddResponse(DialogueEntry mother, string display,string sequence)
        {
             var entry = CreateEntry(mother.conversationID, display, sequence);
             mother.outgoingLinks.Add(NewLink(mother, entry));
            return entry;
        }
        public List<Actor> FindActorOnMap(MapEntry entry)
        {
            return masterDatabase.actors.FindAll(x => x.mapPosID.Item1 == entry.MapID && x.mapPosID.Item2 == entry.id);
        }

        private void OnSelectEvent(object sender, SelectedResponseEventArgs e)
        {
            DialogueManager.instance.activeConversation = conversationController.activeConversationRecord;
            conversationController.GotoState(conversationModel.GetState(e.DestinationEntry));
        }

        private void StartConv()
        {
            StopConversation();
            StartConvPopBasicSelection();
        }
    }

}
