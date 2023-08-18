using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICharWindow : MonoBehaviour
{
    //�������ͽ� â�� ǥ�����ش�.
    //ĳ������ ������ ���� ǥ�ó����� �ٸ� �� �ְ� �Ѵ�.
    // Start is called before the first frame update
    public GameObject prf_component;
    public GameObject prf_text;
    public GameObject prf_img;
    public GameObject prf_bar;
    public VerticalLayoutGroup target;
    UIComponent ui_component;
    enum ComponentType
    {
        img,text,bar
    }
    void Start()
    {
        ShowUp(ComponentType.img,ComponentType.img,ComponentType.text);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void ShowUp(params ComponentType[] uIComponents)
    {
        //�̰��� ����Ǹ� �ش� �����͸� �����������Ѵ�.
        foreach (var uIComponent in uIComponents)
        {
            CreateComponent(uIComponent);
        }
    }
    void CreateComponent(ComponentType component)
    {
        GameObject obj=null;
        switch (component)
        {
            case ComponentType.img:
                obj = Instantiate(prf_img);
                break;
            case ComponentType.text:
                obj = Instantiate(prf_text);
                break;
            case ComponentType.bar:
                obj = Instantiate(prf_bar);
                break;
            default:
                break;
        }
        obj.transform.SetParent(target.transform);
    }
}