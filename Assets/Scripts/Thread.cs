using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class SingletonClass<T>:MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T _instance
    {
        get
        {
            if (instance == null)
            {
                
                var obj = FindObjectOfType<T>();//신내를 한번 쓱 보고 있는지 검사
                if (obj != null)//이미 존재하고있었다면.
                {
                    instance = obj;//스테틱에 등록하고 다음 리턴으로 종료한다.
                }
                else

                {

                    var newSingleton = new GameObject(typeof(T).Name).AddComponent<T>();//신내에도 아무것도 없는데 다른곳에서 요청받았다면.생성해줌

                    instance = newSingleton;

                }
            }
            return instance;
        }
        private set
        {
            instance = value;
        }
    }
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
public class Thread : SingletonClass<Thread>,IOnClickAction//스래드의 추가,삭제를 관리하는 클래스
{
    public static Queue<ThreadContainer> _threadContainers = new Queue<ThreadContainer>();
    public static int _deleteCount = 10;
    public static ThreadContainer _runningThread;
    public delegate void ThreadCallback();
    public ThreadCallback _createThreadCallback;
    [Range(0,1)]
    public float _threadWidthSize=0.5f;
    public DialougeContainer _dialougeContainer;
    
    /*
    //public GameObject ScrollContentCanvas;
    //이 클래스는 스크롤 내용이 들어가는 내용을 관장하는 클래스이다.
    //타입은 총셋 선택지가 나오는 타입,삽화가 나오는 타입,글이 출력되는 타입
    //선택지 연결은 시각적으로 표현하고 싶다. 어떻게 해야할까? 유니티 에디터로..?

    //글과 그림,선택지들을 쉽게 편집하고 싶다... 하지만 여기에선 그것을 어떻게 생성할지를 생각한다.
    //rect를 임의로 생성한다. 크기는 글자수와 같으며 스크롤 콘탠트 캔버스의 세로길이는 안에 들어가는 캔버스 크기만큼 늘어난다.
    //

    문1. 스레드 사용할때와 안사용할때를 구분하여야 한다.
    어떤 이벤트가 발생하면 스레드를 출력하면서 진행하여야한다.


    프롤로그

    내용

    클릭할때마다 문단이 나뉘어서 나타난다. 드래그로 위로 돌아가볼수있지만 로그는 일정 개수 이상은 쌓이지 않는다.

    리스트가있고 이 리스트에는 세가지타입이 들어갈수잇다.


    이벤트 단위로 묶어서 이벤트 조건이 달성되면 이벤트문에서 스레드 컨테이너를 사용해서 이벤트를 발생시킨다. 이벤트는 이름이 곂치니까 인카운터 클래스로 새로 만들어야겠다.
    
    */
    Writing _currentWriting;
    void Start()
    {
        _createThreadCallback = AutoDelete;
        new Illustration("kobayasi") ;
        new Illustration("asdf");
        EventTrigger trigger =GetComponent<EventTrigger>();
        EventTrigger.Entry entry=new EventTrigger.Entry();
        entry.callback.AddListener(data=> { CallThread(_dialougeContainer); });

        trigger.triggers.Add(entry);
    }

    // Update is called once per frame
    void Update()
    {
        //if (MyInput.Touch(TouchPhase.Began))
        //{
        //    print("텇치");
        //    CreateRect();
        //    //Destroy(ThreadContainer.threadContainers.Dequeue().contentObj.gameObject);
        //    //NextThread();
        //    print(_threadContainers.Count);
        //}

        //어떤 버튼을 누르면 스레드가 호출된다.



    }
    public void ClickSelectBox(string guid)
    {
        Debug.Log("선택지 클릭");
        //효과 다음 글을 가지고온다. 필요한거 
        var lis = _dialougeContainer.DialogueNodeData.Where(x=>x.Guid==guid).ToList();
        string nextDialogue =lis[0].DialogueText;
        //라이팅 개선필요 택스트만 짠 넣어서
        /*
         
         작동개요
        목표 롸이팅과 다이얼로그간의 연계
        쉽게 작동하게 한다.

         간편한작동을위해서 무엇을 해야 할까
        지유아디 딕셔너리?
         
         
         
         
         
         
         
         
         */
        
    }
    public void Onclick()
    {

    }
    Dictionary<string, Writing> _writingDic=new Dictionary<string, Writing>();

    public void CallThread(DialougeContainer dc)
    {
        print("call");
        string str = dc.name;
        if (!_writingDic.ContainsKey(str))
        {
            _currentWriting = new Writing(dc);
            _writingDic.Add(str, _currentWriting);
        }
        else
        {
            _currentWriting.GetNext();

        }
    }
    void NextThread()
    {
        //필요한것. 현재의 스래드
        //선택지든뭐든간에 스래드형태면 다음거를 찾아온다.
        //선택지라면 버튼을 눌러야 다음으로 넘어간다.
    }
    void AutoDelete()
    {
        print("check");
        if (_deleteCount < _threadContainers.Count)
        {
            ThreadContainer temp=_threadContainers.Dequeue();
            foreach(GameObject a in temp._contentObjs)
            {
                Destroy(a);
            }
        }
    }


}
public class ThreadContainer//삽화 글 선택지가 등장하는 컨테이너
{
    enum eEvent
    {

    }

    //내용에 들어가는 공통조건
    public List<GameObject> _contentObjs;
    public GameObject _contentObj;
    public string _name;
    Guid _node;
    Guid _nextNode;
    protected GameObject _contentParent;//임시
    protected RectTransform _parentObjRect;
    protected float _spacing;

 
    protected ThreadContainer()
    {
        Init();

    }
    protected virtual void Init()
    {
        _contentObjs = new List<GameObject>();
        Thread._threadContainers.Enqueue(this);
        Thread._runningThread = this;
        _contentParent = Thread._instance.gameObject;
        _parentObjRect = _contentParent.GetComponent<RectTransform>();
        _spacing = _contentParent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing;

    }
    protected virtual GameObject CreateInitEmptyCanvas()
    {
        GameObject tmpObject = new GameObject("NULL");
        tmpObject.transform.parent = _contentParent.transform;
        tmpObject.AddComponent<RectTransform>();
        //_contentRect.localScale = Vector3.one;
        _contentObjs.Add(tmpObject);
        Thread._instance._createThreadCallback();

        return tmpObject;
    }

    

}
namespace UnityEngine 
{
    using System.Linq;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    public class Writing : ThreadContainer
    {
        DialougeContainer _dialougeContainer;
        string[] _stringData;
        //내용
        //문단을 자동으로 끊어주는 기능도 있으면 좋을듯
        /*
         //끊어줄 단위는/n같은걸 사용. 그렇지만 우리가 원하는건 더 손쉬운 방법 즉 /n같은게 생성되어서 넘어와야한다는것..




         */
        int _index = 0;
        public enum Type
        {
            prologue
        }
        public Writing(string dialogueData,DialougeContainer dialougeContainer)
        {

            /*
             스크립터블 오브젝트를 불러온다 값을읽어서 삽입한다.

             */
            Shetterd(dialogueData);
            _dialougeContainer = dialougeContainer;
            /*
             분할출력할 방법찾기



             */


        }
        public Writing(DialougeContainer dc)

        {
            _dialougeContainer = dc;
            Shetterd(_dialougeContainer.DialogueNodeData[0].DialogueText);
            _name = dc.name;
        }
        /// <summary>
        /// 선택지 생성
        /// </summary>
        private void CreateSelectBox()
        {
            StateManager._instance.nowState = StateManager.eTreadState.SelectBox;
            string nextNode = _dialougeContainer.NodeLinks[0].TargetNodeGuid;//임시guid 이거는 첫번째 스타트지점의 guid
            var nextNodeList = _dialougeContainer.NodeLinks.Where(x => x.BaseNodeGuid == nextNode).ToList();//노드로 다음대상노드를 검색해서 리스트로 만든다.
            for (int k = 0; k < nextNodeList.Count; k++)
            {
                
                NodeLinkData a = nextNodeList[k];
                GameObject tmpObj=GameObject.Instantiate( ResourcePoolManager._instance.GetObj<GameObject>("SelectBox"));
                tmpObj.transform.SetParent(_parentObjRect);
                tmpObj.name = "ㄴSelectBox" + _index;
                Text text = tmpObj.transform.GetChild(0).GetComponent<Text>();
                text.text = a.PortName;
                text.font = ResourcePoolManager._instance.GetObj<Font>("KATURI");
                text.fontSize = 55;
                text.lineSpacing = 1.2f;
                int j = 0;//공백크기
                string charArray = " .,";
                for (int i = 0; i < text.text.Length; i++)
                {
                    if (charArray.Contains(text.text[i].ToString()))
                    {
                        j++;
                    }
                    //if(_contentText.text[i]==' ')
                    //{
                    //    j++;
                    //}
                }
                
                int tempWidth = (int)(Camera.main.pixelWidth * 0.8f);
                float tempheight = Mathf.Ceil(((text.text.Length - j) + (float)(j / 3)) * (text.fontSize - 3f) / tempWidth) * (text.fontSize + text.lineSpacing * 15) + 100f;

                RectTransform rectTransform = tmpObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(tempWidth, tempheight);

                _parentObjRect.offsetMin += -Vector2.up * (rectTransform.sizeDelta.y + _spacing);


                //이미지박스를 생성한다. 셀렉트 박스안에 지유아디를 넣는다.
                //선택시 다음지문을 출력해야한다.
                //다음지문위치
                //버튼선택가능하게 해야함.
                //버튼을 선택하면 버튼에 접근해서 셀렉트 박스에 들어있는 지유아디를 가지고오고 그 지유아디로 다음 텍스트를 가져온다.

                SelectBox sb= tmpObj.GetComponent<SelectBox>();
                sb.SetGuid(a.TargetNodeGuid);


                //이미지박스는 선택했을때 파라미터가 바뀔수있어야한다.





            }

            //=a.PortName;
            //담을 상자를 만들자

        }

        //public Writing(Type input)
        //{

        //    /*
        //     스크립터블 오브젝트를 불러온다 값을읽어서 삽입한다.

        //     */
        //    _dialougeContainer = ResourcePoolManager._instance.GetObj<DialougeContainer>(input.ToString());
        //    Debug.Log(_dialougeContainer.DialogueNodeData[0].DialogueText);
        //    /*




        //     */


        //}
        bool FindSelection(string str)
        {
            return true;
        }
        string FirstNodeTargetGUID()
        {
            return _dialougeContainer.NodeLinks[0].TargetNodeGuid;
        }
        void GetNextNode()
        {

        }

        public void InitText()
        {
            //인덱스 초기화
            _index = 0;
        }
        public void GetNext()
        {
            if (_index == _stringData.Length&&StateManager._instance.nowState!=StateManager.eTreadState.SelectBox)
            {
                CreateSelectBox();

                //이단락이후로 겟넥스트는 불러오지 않는다.

                
                return;
            }
            else if (!(StateManager._instance.nowState == StateManager.eTreadState.ReadingBox)&&!(StateManager._instance.nowState == StateManager.eTreadState.None))
            {
                return;
            }

            StateManager._instance.nowState = StateManager.eTreadState.ReadingBox;
            GameObject tmpObj = CreateInitEmptyCanvas();
            tmpObj.name = "ㄴWritingCanvas"+_index;
            Text text = tmpObj.GetComponent<Text>();
            text.text = _stringData[_index];
            text.font = ResourcePoolManager._instance.GetObj<Font>("KATURI");
            text.fontSize = 55;
            text.lineSpacing = 1.2f;
            int j=0;//공백크기
            string charArray = " .,";
            for(int i=0; i< text.text.Length;i++)
            {
                if (charArray.Contains(text.text[i].ToString()))
                {
                    j++;
                }
                //if(_contentText.text[i]==' ')
                //{
                //    j++;
                //}
            }
            
            int tempWidth = (int)(Camera.main.pixelWidth * 0.8f);
            float tempheight=Mathf.Ceil(((text.text.Length - j) + (float)(j / 3)) * (text.fontSize - 3f) / tempWidth) * (text.fontSize+ text.lineSpacing*15)+100f;

            RectTransform rectTransform = tmpObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(tempWidth,tempheight);

            _parentObjRect.offsetMin += -Vector2.up * (rectTransform.sizeDelta.y + _spacing);



            //크기기준: 폰트크기, 줄바꿈있으면 무조건 하나 칸수넘어가도 하나 줄바꿈 단위로 이미 띄어져 있으므로 글자길이로 비교 공백은 1/4 로 취급
            //


            ++_index;
        }

        void Shetterd(string @string)
        {
            //분리해서 리스트에 저장하기
            char[] splitter = { '\n' };
            _stringData = @string.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

        }
        
        protected override GameObject CreateInitEmptyCanvas()
        {
            GameObject tmpobject = new GameObject("NULL");
            tmpobject.transform.parent = _contentParent.transform;
            tmpobject.AddComponent<RectTransform>();
            tmpobject.AddComponent<Text>();
            _contentObjs.Add(tmpobject);
            Thread._instance._createThreadCallback();

            //_contentRect.localScale = Vector3.one;
            return tmpobject;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("선택지클릭");

        }
    }
    public class Illustration : ThreadContainer
    {
        //삽화내용
        //생성시 콘텐트아래 붙는다.
        //텍스트 중간에 등장할 수 있어야 한다.
        //중간을 어떻게 판단할것인가?
        //1.그래프상에서 택스쳐를 별도로 추가한다.
        


        public Illustration(string key)
        {
            GameObject tmpObj=CreateInitEmptyCanvas();

            Sprite tempImg = ResourcePoolManager._instance.GetObj<Sprite>(key);
            //참조할 이미지경로
            tmpObj.name = "ㄴContent:Illustration";
            tmpObj.transform.SetParent(_contentParent.transform);//콘텐트아래 삽입
            //보여줄 이미지의 크기를 조정한다. 레이아웃으로 인해 포지션은 고정되므로 너비와 높이로 조정한다.
            Vector2 tempv2 = tempImg.rect.size;
            RectTransform tmpRTransfrom = tmpObj.GetComponent<RectTransform>();
            tmpRTransfrom.sizeDelta = tempv2;
            _parentObjRect.offsetMin += -Vector2.up * (tempv2.y + _spacing);
            Image temimg = tmpObj.AddComponent<Image>();
            temimg.sprite = tempImg;


        }

    }

}
interface IOnClickAction
{
    public void Onclick();
}
public class ChoiceAnswer:ThreadContainer
{
    //선택지만 가진조건
}



