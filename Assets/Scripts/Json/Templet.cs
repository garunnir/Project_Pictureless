using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class JsonHandler
{
    readonly string jsonBasePath = Application.persistentDataPath;
    //저장될 장소
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
    //인풋, 주 대상 비교대상 아웃풋, 주대상이 보고있는 비교대상의 호감도.
    //2차원 배열을 생성해야한다.
    //필요하다면 상속으로 분리
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
    public void ReadCsv()
    {
        //스트림 쓰면 이점은 한줄씩 읽을수있다 였지.
        StreamReader reader = new StreamReader(csvBasePath + "/" + fileName);

        while (!reader.EndOfStream)
            Debug.Log(reader.ReadLine());
        reader.Close();
    }
    public void TestFirstLineSplit()//확인결과 스플릿은 정상적으로 빈곳은 비어서 나온다.
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
        //csv 시트데이터의 목적: 엑셀뷰어등으로 보는 데이터의 직관성 따라서 맨윗줄과 맨옆줄에는 캐릭터이름은 반드시 있어야 구분할 수 있을것.. 성이나 종족 등까지는 포함하면
        //더 읽기 어려울것이 예상되므로 별명등으로 한다 형식은 제일 중요한 구분 아이디가 앞에나오고 그 다음

        StreamReader reader = new StreamReader(csvBasePath + "/" + fileName);
        Dictionary<string, Dictionary<string, string>> dic = new Dictionary<string, Dictionary<string, string>>();
        int lineCount = -1;
        List<string> getDicKey=new List<string>();
        {
            //첫째 줄은 스플릿으로 분할해야한다.
            lineCount++;
            string str = reader.ReadLine();
            str.Trim('\n', ' ');
            string[] splitedStr = str.Split(',');
            foreach (var item in splitedStr)
            {
                string strID=item.Split('/')[0];
                getDicKey.Add(strID);//순서대로 삽입되는것이 전재 이 순서로 키값을 알아낼것이기때문.
                dic.Add(strID, new Dictionary<string, string>());//맨 윗줄 칸 생성
            }
        }
        while (!reader.EndOfStream)
        {
            lineCount++;
            string str = reader.ReadLine();
            str.Trim('\n', ' ');
            string[] splitedStr = str.Split(',');

            //List<int> list = splitedStr.Select(item => int.Parse(item)).ToList();
            //줄의 처음 부분은 새로운 키가된다.
            string first=splitedStr[0].Split(divider)[0];
            //dic[getDicKey[0]].Add(first,first);//키생성
            for (int i = 1; i < splitedStr.Length; i++)
            {
                string item = splitedStr[i];
                dic[getDicKey[i]].Add(first, item);

                //dic[getDicKey[splitedStr[i]]]
            }
            //list.RemoveAt(0);

            //int[] intar = System.Array.ConvertAll(splitedStr, item => int.Parse(item));
            //int[] cointar = new int[intar.Length - 1];
            //System.Array.Copy(intar,1,cointar,0,cointar.Length);

        }

        reader.Close();

        return dic;
    }

}

public class Templet : MonoBehaviour
{
    private void Start()
    {
        CsvHandler handler = new CsvHandler("testfile.csv");
        //handler.TestFirstLineSplit();
        Dictionary<string,Dictionary<string,string>> instancedDic=handler.InstanceReadableDic();
        print(instancedDic["1"]["3"]);
        print(instancedDic["2"]["4"]);
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