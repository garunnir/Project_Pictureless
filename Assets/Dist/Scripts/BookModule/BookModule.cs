using Garunnir;
using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookModule : MonoBehaviour
{

    [SerializeField] private RectTransform m_window;
    [SerializeField] private RectTransform m_descriptionBody;
    [SerializeField] private RectTransform m_elementTemplet;
    [SerializeField] private RectTransform m_keywordsParentLeft;
    [SerializeField] private RectTransform m_keywordsParentRight;
    [Header("Buttons")]
    [SerializeField] private Button m_btn_open;
    [SerializeField] private Button m_btn_close;
    TMP_Text text_description;
    List<(string, string)> keywords =new List<(string, string)>();
    
    List<KeywordBox> keywordsSlot = new List<KeywordBox>();

    int currentPage = 0;
    class KeywordBox
    {
        public RectTransform mother;
        public TMP_Text keyword;
        public TMP_Text description;
        public Button btn_opendescription;
        public void SetDescription(string value)
        {
            btn_opendescription.onClick.RemoveAllListeners();
            btn_opendescription.onClick.AddListener(() => 
            {
                if (mother.gameObject.activeSelf)
                {
                    mother.gameObject.SetActive(true); description.text = value;
                }
                else
                {
                    mother.gameObject.SetActive(false); description.text = "null";
                }
                });
        }
    }
    private void Awake()
    {
        m_btn_close?.onClick.AddListener(Close);
        m_btn_open?.onClick.AddListener(Open);
    }
    private void Start()
    {
        print("d");
        //tmp.font=textStyle.font; 
        //tmp.fontSize=textStyle.fontSize;
        //int maxCharacterCount = CalculateMaxCharacterCount(text, textStyle, rect);

        //GUILayout.BeginArea(rect);
        //GUILayout.Label("Max Characters: " + maxCharacterCount, textStyle);
        //GUILayout.EndArea();
        ////
        //Rect fittingRect = CalculateFittingRect(text, textStyle, new Rect(10, 10, 200, 200));
        //discriptionBody.offsetMax = new Vector2( fittingRect.x+fittingRect.width,fittingRect.y+fittingRect.height);
        //discriptionBody.offsetMin = new Vector2(fittingRect.x,fittingRect.y);
        //GUI.Label(fittingRect, text, textStyle);
        //구현할 것. 텍스트가 쓰여지고 머리에 해당하는 부분에 
        //설명을 넣을 공간이 좀 애매한데........
        //대화칸 옆에 화살표를 만들어서 상세 디스크립션을 나타내도록 하는게 좋을것같음!
        //디스크립션 표시를 위해서는 자신의 부모렉트의 영향을 벗어나는
        //절대랙트값을 가져올수 있어야 한다.
        //어떻게 개별로 디스크립션을 지정할 것인가?
        text_description=m_descriptionBody.GetComponentInChildren<TMP_Text>();
        InitBookKeys(20);
        AddKeyWord("ddddd","AAaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        AddKeyWord("dsdfdddd","AAaaaaaaaaaaaaaaaaaaaaaaaaaasdfaaaaaaaaaaaaaaaaaaaa");
        AddKeyWord("1","1");
        AddKeyWord("2","2");
        AddKeyWord("13","3");
        AddKeyWord("13","14");
        AddKeyWord("dsdffdffddd","AAaaaaaaaaaaaafaaffaaaaaaaaaaaasdfaaaaaaaaaaaaaaaaaaaa");
        AddKeyWord("12","1123");
        AddKeyWord("11","1123");
        AddKeyWord("132","231");
        AddKeyWord("1123213","1231231");
        AddKeyWord("1123","1123");
        AddKeyWord("1123","1123");
        AddKeyWord("1123","1123");
        AddKeyWord("dsdffdddd","AAaaaaaaaaaaaafaaaaaaaaaaaaaasdfaaaaaaaaaaaaaaaaaaaa");
        AddKeyWord("1123","1123");
        AddKeyWord("1123","1123");
        AddKeyWord("1123213","1231231");
        AddKeyWord("1123","1123");
        AddKeyWord("1123","1123");
        AddKeyWord("1123","1123");
        AddKeyWord("11","131");
        AddKeyWord("31","13");
        ShowPage(0);
        m_window.gameObject.SetActive(false);
    }
    public void NextPage()
    {
        currentPage++;
        if(currentPage*keywordsSlot.Count>keywords.Count)
        {
            currentPage = 0;
        }
        ShowPage(currentPage);
    }
    public void PrevPage() 
    { 
        currentPage--;
        if (currentPage < 0)
        {
            int testpage = 0;
            while(testpage * keywordsSlot.Count < keywords.Count && testpage<100)
            {
                testpage++;
            }
            currentPage = Mathf.Max(0, testpage-1);
        }
        ShowPage(currentPage);
    }

    public void ShowPage(int page)
    {
        int slotcount=keywordsSlot.Count;

        for (int i = 0; i < slotcount; i++)
        {
            if(keywords.Count> i + slotcount * page)
            {
                keywordsSlot[i].mother.gameObject.SetActive(true);
                keywordsSlot[i].keyword.text = keywords[i + slotcount * page].Item1;
                keywordsSlot[i].SetDescription(keywords[i + slotcount * page].Item2);
            }
            else
            {
                keywordsSlot[i].mother.gameObject.SetActive(false);
            }
        }

    }
    public void ToggleBook()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
    public void AddKeyWord(string keyWord,string description)
    {
        //if (keywords.ContainsKey(keyWord))
        //{
        //    keywords[keyWord]=description;
        //    return;
        //}
        keywords.Add((keyWord, description));
        //RectTransform item = Instantiate(headTemplet);
        //item.gameObject.SetActive(true);
        //Button btn=item.GetComponentInChildren<Button>();
        //TMP_Text text= item.GetComponentInChildren<TMP_Text>();
        //text.text = keyWord;
        //btn?.onClick.AddListener(() => OpenDiscription(description));

        //if (headTemplet.rect.height * keywordsParentLeft.childCount > keywordsParentLeft.rect.height)
        //{
        //    if(headTemplet.rect.height * keywordsParentLeft.childCount > keywordsParentLeft.rect.height)
        //    {

        //    }
        //    else
        //    {
        //        item.transform.parent = keywordsParentRight;
        //        item.transform.localScale = Vector3.one;
        //    }

        //}
        //else
        //{
        //    item.transform.parent = keywordsParentLeft;
        //    item.transform.localScale = Vector3.one;
        //}
        //keywordsSlot.Add(new KeywordBox { mother = item, btn_opendescription = btn, keyword = text, description = text_description });
    }
    private void InitBookKeys(int count)
    {
        RectTransform CreateItem(RectTransform parent)
        {
            RectTransform item = Instantiate(m_elementTemplet);
            item.gameObject.SetActive(true);
            Button btn = item.GetComponentInChildren<Button>();
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            text.text = "null";
            btn.onClick.AddListener(() => OpenDiscription("null"));
            item.transform.parent = parent;
            item.transform.localScale = Vector3.one;

            keywordsSlot.Add(new KeywordBox { mother = item, btn_opendescription = btn, keyword = text, description = text_description });
            return item;
        }
        for (int i = 0; i < count; i++)
        {
            //bool stop=(headTemplet.rect.height * keywordsParentLeft.childCount > keywordsParentLeft.rect.height) 
            //    ? ((headTemplet.rect.height * keywordsParentLeft.childCount > keywordsParentLeft.rect.height)? true: CreateItem(keywordsParentLeft)) : CreateItem(keywordsParentLeft);

            //if (stop) return;
            if (m_elementTemplet.rect.height * m_keywordsParentLeft.childCount > m_keywordsParentLeft.rect.height)
            {
                if (m_elementTemplet.rect.height * m_keywordsParentRight.childCount > m_keywordsParentRight.rect.height)
                {
                    Debug.Log("CreateFailed");
                    return;
                }
                else
                {
                    CreateItem(m_keywordsParentRight);
                }
            }
            else
            {
                CreateItem(m_keywordsParentLeft);
            }
        }
    }
    void OpenDiscription(string discription)
    {
        m_descriptionBody.gameObject.SetActive(true);
        RectTransform rect = GameManager.Instance.GetUpperRect();
        // Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(transform.parent.GetComponent<RectTransform>(),rect );
        //Rect r=RectTransformUtility.(rect, GetComponentInParent<Canvas>());
        //discriptionBody.rect.Set(r.x,r.y,r.width,r.height);
        //Utillity.CopyDifParentRect(rect,discriptionBody);
        Utillity.CopyValuesCover(m_descriptionBody, rect);
        text_description.text = discription;
        text_description.font = UILocalizationManager.instance.localizedFonts.GetTextMeshProFont(Localization.language);
    }
    private int CalculateMaxCharacterCount(string text, GUIStyle style, Rect rect)
    {
        GUIContent content = new GUIContent(text);
        float textHeight = style.CalcHeight(content, rect.width);
        int maxCharacterCount = Mathf.FloorToInt(text.Length * (rect.height / textHeight));
        return maxCharacterCount;
    }

    private Rect CalculateFittingRect(string text, GUIStyle style, Rect baseRect)
    {
        GUIContent content = new GUIContent(text);
        float textHeight = style.CalcHeight(content, baseRect.width);
        int lines = Mathf.CeilToInt(textHeight / style.lineHeight);

        Rect fittingRect = new Rect(baseRect.x, baseRect.y, baseRect.width, style.lineHeight * lines);
        return fittingRect;
    }
    public void Open()
    {
        m_window.gameObject.SetActive(true);
        m_btn_open?.gameObject.SetActive(false);
        m_btn_close?.gameObject.SetActive(true);
    }
    public void Close()
    {
        m_window.gameObject.SetActive(false);
        m_btn_open?.gameObject.SetActive(true);
        m_btn_close?.gameObject.SetActive(false);
    }
}
