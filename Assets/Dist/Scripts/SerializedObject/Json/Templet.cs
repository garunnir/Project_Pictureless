using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class JsonHandler
{
    readonly string jsonBasePath = Application.persistentDataPath;
    //����� ���
    void Write(Object obj, string path)
    {
        string tmps = JsonUtility.ToJson(obj);
        if (string.IsNullOrEmpty(tmps)) Debug.LogError("JsonWriteError: text is empty!");
        File.WriteAllText(jsonBasePath + "/" + path, tmps);
    }
    T Read<T>(string path) where T : Object
    {
        string tmps = File.ReadAllText(jsonBasePath + "/" + path);
        if (string.IsNullOrEmpty(tmps))
        {
            Debug.LogError("JsonReaderError: text is empty!");
            return null;
        }
        else return JsonUtility.FromJson<T>(tmps);
    }

}
public class CsvHandler
{
    readonly string csvBasePath = Application.persistentDataPath;
    readonly string fileName;
    List<List<string>> rawdata;
    Dictionary<string, int> convertIDToRawHor;
    Dictionary<string, int> convertIDToRawVer;
    //��ǲ, �� ��� �񱳴�� �ƿ�ǲ, �ִ���� �����ִ� �񱳴���� ȣ����.
    //2���� �迭�� �����ؾ��Ѵ�.
    //�ʿ��ϴٸ� ������� �и�
    List<List<int>> list = new List<List<int>>();
    public CsvHandler(string fileName)
    {
        this.fileName = fileName;
    }
    public CsvHandler(string basePath, string fileName)
    {
        csvBasePath = basePath;
        this.fileName = fileName;
    }
    public void WriteCsv()
    {
        StreamWriter writer = new StreamWriter(csvBasePath + "/" + fileName);

        writer.WriteLine("1,2,3,5,6,7,8,9,10,12,13,15,16,17,18,19,110,12,13,15,16,17,18,19,110,12,13,15,16,17,18,19,1101,21,31,51,16,17,181,91,11101,121,131,151,161,171,181,191,110");
        writer.WriteLine("1,2,3,5,6,7");
        writer.WriteLine("4");
        writer.Close();
    }
    StreamReader CallCSVFile()
    {
        return new StreamReader(csvBasePath + "/" + fileName);
    }
    public void ReadCsv()
    {
        //��Ʈ�� ���� ������ ���پ� �������ִ� ����.
        StreamReader reader = CallCSVFile();
        rawdata = ConvertReadScvToRawData(reader);
        rawdata = ConvertReadScvToRawData(reader);
        reader.Close();
        //IReadOnlyList<>
    }
    //�� ���� ����� �ϴµ�..
    string GetData(string sid,string did,List<List<string>> data)
    {

        //�������� ������. ���̵� ���������� ��ġ�� ã�ư���
        //��� �׷��ԵǸ� ó�����ͳ����� ã�ƺ��Ե����ʳ�..;;
        //�ƴ��� ��ųʸ��� ���� �����ؼ� ���õ������� �ּҸ� ������ �������� �ϸ� �ȴ�.

        string answer=data[convertIDToRawHor[sid]][convertIDToRawVer[did]];

        return answer;
        //ã�������� ��� ������ ��ġ�ϱ�
        //Ȥ�� �ҷ������Ҷ� �����ȵ����ͷ� �ְ� �����Ҷ� �ٽ� Ǯ� �����ϰ� �����ϰ� �ϱ�.

        //��ųʸ��� ��� �����ؾ� ����ұ�?
        //1 ����ϴ°��� ����
        //�ɹ������� ���� �̴� �մ��� ��ġ�̴�.

        //�� ��ε��� �����Լ��� ����ϱ⸸�ϸ� �ذ�Ǵ� ������.
        //�̰� ��� �����Լ��� ���� �� ������?
        //��ȯ������ ��� �����Ѵ�.
        //������ �̷��� �ϸ� ������ ������ ����� ����. �ǽð����� ������ ������ ���� ���� �븩�̰�..
        //��ȯ������ Ŭ���� �ܺη� �����ұ�?


    }
    List<List<string>> ConvertReadScvToRawData(StreamReader reader)
    {
        List<List<string>> tmpListing = new List<List<string>>();

        while (!reader.EndOfStream)
        {
            string[] strs= reader.ReadLine().Split(',');
            List<string> tm = new List<string>();
            for (int i = 0; i < strs.Length; i++)
            {
                tm.Add(strs[i]);
                Debug.Log(strs[i]);
            }
            tmpListing.Add(tm);
        }
        return tmpListing;
    }
    public void SetRawDataKey(StreamReader reader, out Dictionary<string,int> horDickey,out Dictionary<string,int> verDickey)
    {
        int lineCount = -1;
        Dictionary<string, int> revHorDickey = new Dictionary<string, int>();
        Dictionary<string, int> revVerDickey = new Dictionary<string, int>();
        char divider = '/';
        {
            //ù° ���� ���ø����� �����ؾ��Ѵ�.
            lineCount++;
            string str = reader.ReadLine();
            str.Trim('\n', ' ');
            string[] splitedStr = str.Split(',');
            for (int i = 0; i < splitedStr.Length; i++)
            {
                string item = splitedStr[i];
                string strID = item.Split(divider)[0];
                revHorDickey.Add(strID, i);
            }
        }
        while (!reader.EndOfStream)
        {
            lineCount++;
            string str = reader.ReadLine();
            str.Trim('\n', ' ');
            string[] splitedStr = str.Split(',');
            string first = splitedStr[0].Split(divider)[0];
            revVerDickey.Add(first, lineCount);
        }
        horDickey = revHorDickey;
        verDickey = revVerDickey;
    }

    void CreateRawData()
    {

    }

    public void TestFirstLineSplit()//Ȯ�ΰ�� ���ø��� ���������� ����� �� ���´�.
    {
        StreamReader reader = new StreamReader(csvBasePath + "/" + fileName);

        string[] checkblank = reader.ReadLine().Split(',');
        foreach (var item in checkblank)
        {
            Debug.Log(item);
        }
    }
    public Dictionary<string,Dictionary<string,string>> InstanceReadableDic()
    {
        char divider = '/';
        //csv ��Ʈ�������� ����: ������������ ���� �������� ������ ���� �����ٰ� �ǿ��ٿ��� ĳ�����̸��� �ݵ�� �־�� ������ �� ������.. ���̳� ���� ������� �����ϸ�
        //�� �б� �������� ����ǹǷ� ��������� �Ѵ� ������ ���� �߿��� ���� ���̵� �տ������� �� ����

        StreamReader reader = CallCSVFile();
        Dictionary<string, Dictionary<string, string>> dic = new Dictionary<string, Dictionary<string, string>>();
        int lineCount = -1;
        List<string> getDicKey=new List<string>();
        //Dictionary<string, int> revHorDickey = new Dictionary<string, int>();
        //Dictionary<string, int> revVerDickey = new Dictionary<string, int>();
        {
            //ù° ���� ���ø����� �����ؾ��Ѵ�.
            lineCount++;
            string str = reader.ReadLine();
            str.Trim('\n', ' ');
            string[] splitedStr = str.Split(',');
            for (int i = 0; i < splitedStr.Length; i++)
            {
                string item = splitedStr[i];
                string strID=item.Split(divider)[0];
                getDicKey.Add(strID);//������� ���ԵǴ°��� ���� �� ������ Ű���� �˾Ƴ����̱⶧��.
                //revHorDickey.Add(strID,i);
                dic.Add(strID, new Dictionary<string, string>());//�� ���� ĭ ����
            }
        }
        while (!reader.EndOfStream)
        {
            lineCount++;
            string str = reader.ReadLine();
            str.Trim('\n', ' ');
            string[] splitedStr = str.Split(',');

            //List<int> list = splitedStr.Select(item => int.Parse(item)).ToList();
            //���� ó�� �κ��� ���ο� Ű���ȴ�.
            string first=splitedStr[0].Split(divider)[0];
            //dic[getDicKey[0]].Add(first,first);//Ű����
            for (int i = 1; i < splitedStr.Length; i++)
            {
                string item = splitedStr[i];
                dic[getDicKey[i]].Add(first, item);
                //revVerDickey.Add(first, lineCount);
                //dic[getDicKey[splitedStr[i]]]
            }
            //list.RemoveAt(0);

            //int[] intar = System.Array.ConvertAll(splitedStr, item => int.Parse(item));
            //int[] cointar = new int[intar.Length - 1];
            //System.Array.Copy(intar,1,cointar,0,cointar.Length);

        }

        reader.Close();
        //convertIDToRawHor = revHorDickey;
        //convertIDToRawVer = revVerDickey;
        return dic;
        //���� �д°��� �������� ���⸦ �غ��

    }

    public void SaveDic(Dictionary<string,Dictionary<string,string>> savetarget)
    {
        //������ ��� �����ұ�.
        //�ϴ� Ű�� ���� �˾ƾ� �ϴµ�.
        //�� ����ġ�� ��������..
        string firstLine = "";
        string[] verticalLine = new string[savetarget.Count+1];

        //string[][] tmpListing = new string[savetarget.Count+1][];
        //for (int i = 0; i < tmpListing.Length; i++)
        //{
        //    tmpListing[i] = new string[savetarget.Count+1];
        //}

        bool onceFlag = false;
        foreach (var item in savetarget)
        {
            //�켱 Ű�� ù��° ��Ʈ���� �����Ѵ�.
            firstLine += ","+item.Key+ "/" + IdToName(item.Key);
            //�ι�° Ű�� ���� �� ù��°�� ���� �Ѵ�.
            if (!onceFlag)
            {
                int count = 0;
                foreach (var item2 in savetarget[item.Key])
                {
                    count++;
                    verticalLine[count] = item2.Key+"/"+IdToName(item2.Key);
                }
            }
        }
        verticalLine[0] = firstLine;
        //����������� �׸��� �ۼ��� �� ��.
        //���ĺ��ʹ� ���� �ִ´�.
        //����Ʈ�� �����Ѵ�. ���� ������� �ֱ� ���ؼ�.

        //������ �̰��� ����� ���Ǽ����� ���� �ִ� ����̴�.
        //ó���� �о���϶������� ������������ ���õ����͸� ���� �̿��ϰ� �ϸ� �� ȿ������ ����� �� �� ����.
        //��ȸ �޼��带 ������ ����� ���õ����� �״�� �ᵵ �� �� ������.
        //�� ������ �̿� ����.
        //���� �б�� ���� �������� ������� �Ѵ�. �̷����ϸ� ���� �ٽ� �迭�� ���� �ʿ䰡 ����. �����Ŀ� �� ���� �����ϰ� �ٽ� ���� �Ǵϱ�.
        //������ �Ǵٸ������ ������ ����� �ҷ������ �ð��� �ɸ��� �۾��̶�°��̴�. ���� �׷��ʿ䰡 �����ʴ�.
        for (int i = 1; i < verticalLine.Length; i++)
        {
            //verticalLine[i]+=savetarget[i][]
        }
    }
    void LoadRawCSV()
    {
        //StreamReader
    }
    string IdToName(string id)
    {
        Dictionary<string, string> idSlashName = new Dictionary<string, string>();//�������� ����

        return idSlashName[id];
    }
}

public class Templet : MonoBehaviour
{
    private void Start()
    {
        CsvHandler handler = new CsvHandler("testfile.csv");
        handler.ReadCsv();
        //handler.TestFirstLineSplit();
        //Dictionary<string,Dictionary<string,string>> instancedDic=handler.InstanceReadableDic();
        //print(instancedDic["1"]["3"]);
        //print(instancedDic["2"]["4"]);
        //handler.WriteCsv();
        //handler.ReadCsv();
        //handler.InstanceList();
    }
    public static void TestBigO(System.Action action, int trycount, string msg)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < trycount; i++)
        {
            action();
        }
        stopwatch.Stop();
        Debug.LogError(msg + ": " + stopwatch.ElapsedMilliseconds + "ms");
    }

}