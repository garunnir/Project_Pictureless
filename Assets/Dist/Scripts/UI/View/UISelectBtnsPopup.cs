using DG.Tweening;
using Lean.Pool;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ContentSizeFitter))]
public class UISelectBtnsPopup : UI
{
    //선택지 팝업한다.
    //버튼 크기에 따라 플랙시블하게 
    Button[] originalbtns;
    ContentSizeFitter contentSizeFitter;
    [SerializeField]Button btnTemplet;
    List<Button> activatedbuttons=new List<Button>();
    private void Awake()
    {
        contentSizeFitter = GetComponent<ContentSizeFitter>();
        originalbtns=GetComponentsInChildren<Button>();
    }

    /// <summary>
    /// 문자 입력하면 그것이 화면에 표기됨
    /// </summary>
    /// <param name="selections"></param>
    public List<Button> CreateBtns(params string[] selections)
    {
        foreach (string selection in selections)
        {
            var obj=LeanPool.Spawn(btnTemplet,transform.parent);
            obj.name = "선택"+selection;
            activatedbuttons.Add(obj);
        }
        return activatedbuttons;
    }
    public Button CreateBtns(string selection)
    {
        var obj = LeanPool.Spawn(btnTemplet, transform);
        obj.name = "선택" + selection;
        activatedbuttons.Add(obj);
        var tmp =obj.GetComponentInChildren<TextMeshProUGUI>();
        if(tmp != null) { tmp.text = selection; }
        return obj;
    }
    public void ClearAll()
    {
        while (activatedbuttons.Count>0)
        {
            InfiniteLoopDetector.Run();
            var item = activatedbuttons[0];
            activatedbuttons.Remove(item);
            LeanPool.Despawn(item);
        }
    }

    public override IEnumerator AddHideAct()
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator AddShowAct()
    {
        throw new System.NotImplementedException();
    }


}
