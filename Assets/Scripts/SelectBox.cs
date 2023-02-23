using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectBox : MonoBehaviour,IPointerClickHandler
{
    [SerializeField]
    string _guid;

    public void OnPointerClick(PointerEventData eventData)
    {
        
    }
    public void SetGuid(string guid)
    {
        _guid = guid;
    }
    // Start is called before the first frame update
    public void GetDialogue()
    {
        //버튼액션
        Thread._instance.ClickSelectBox(_guid);
        
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
