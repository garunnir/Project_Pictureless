using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharCreater : MonoBehaviour
{
    public DialogueDatabase dialogueDatabase;
    
    int cacheLastIdNum = DataConfig.createStart;
    // Start is called before the first frame update
    void Start()
    {
        Create();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void Create()
    {

        //�߰��� ����������..?
    }
    int EditDuplicated(int checkId)
    {
        //�����ؾ� �ϴ� ��. �ߺ�üũ�Ҷ� ������ȣ�� ì�ܾ� �ϳ�?
        //���������� ��ϵ� ��ȣ�� ã�� ������ȣ�� �ִ°� �� ����Ʈ �ѰͰ�����..
        //������ ��쵵 ����ؾ� �ϴµ�..
        //���̵�� ������ȣ��
        //�¾�� ���ó���� �������� �� ��ȣ�� ������ �� ���� ��?�����̴�.
        //������ �ִ��� �������� �ο��Ǿ�� �Ѵ� ������ int�� ���������� �Ѿ�� �����÷��Ұ��̴�.
        //������ �� ������ �ʰ��Ϸ��� �����濵�ùķ��̼��� �Ǿ��Ұ��̴� ����
        //Ȯ�强�� �����Ѵٸ� ����°� ������..
        //�װ� ���߿� �����ص� �����ʴ� ������ �ż��ϰ� �ϼ��ϴ°� �켱�̴ϱ�.
        //�ٸ� ������� ���� �����Ҷ� � ����� Ȱ���ϴ°ɱ�?
        //����Ʈ�� ���̵� �߰��Ҷ� ��ȣ�� �� �˻��ؾ߸� �ϴ°ǰ�?
        //���� npc 0~9999 ���� 10000~;
        
        if (dialogueDatabase.actors.Find(x => x.id == checkId) == null)
        {
            cacheLastIdNum=checkId;
            return checkId;
        }
        else
        {
            return EditDuplicated(checkId++);
        }
    }
}
