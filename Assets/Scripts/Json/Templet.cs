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
    List<List<string>> rawdata;
    Dictionary<string, int> convertIDToRawHor;
    Dictionary<string, int> convertIDToRawVer;
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
    StreamReader CallCSVFile()
    {
        return new StreamReader(csvBasePath + "/" + fileName);
    }
    public void ReadCsv()
    {
        //스트림 쓰면 이점은 한줄씩 읽을수있다 였지.
        StreamReader reader = CallCSVFile();
        rawdata = ConvertReadScvToRawData(reader);
        rawdata = ConvertReadScvToRawData(reader);
        reader.Close();
        //IReadOnlyList<>
    }
    //이 셋을 묶어야 하는데..
    string GetData(string sid,string did,List<List<string>> data)
    {

        //날데이터 번역기. 아이디를 때려넣으면 위치를 찾아간다
        //잠깐 그렇게되면 처음부터끝까지 찾아보게되지않냐..;;
        //아니지 딕셔너리를 새로 생성해서 관련데이터의 주소를 빠르게 가져오게 하면 된다.

        string answer=data[convertIDToRawHor[sid]][convertIDToRawVer[did]];

        return answer;
        //찾을때마다 모든 데이터 서치하기
        //혹은 불러오기할때 정리된데이터로 넣고 저장할때 다시 풀어서 원복하고 저장하게 하기.

        //딕셔너리를 어디에 생성해야 깔끔할까?
        //1 사용하는곳에 생성
        //맴버변수에 생성 이는 합당한 위치이다.

        //위 고민들은 순수함수로 사용하기만하면 해결되는 문제다.
        //이걸 어떻게 순수함수로 만들 수 있을까?
        //반환값으로 계속 전달한다.
        //하지만 이렇게 하면 정보를 저장할 방법이 없다. 실시간으로 문서를 수정할 수도 없는 노릇이고..
        //반환값으로 클래스 외부로 전달할까?


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
            //첫째 줄은 스플릿으로 분할해야한다.
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

        StreamReader reader = CallCSVFile();
        Dictionary<string, Dictionary<string, string>> dic = new Dictionary<string, Dictionary<string, string>>();
        int lineCount = -1;
        List<string> getDicKey=new List<string>();
        //Dictionary<string, int> revHorDickey = new Dictionary<string, int>();
        //Dictionary<string, int> revVerDickey = new Dictionary<string, int>();
        {
            //첫째 줄은 스플릿으로 분할해야한다.
            lineCount++;
            string str = reader.ReadLine();
            str.Trim('\n', ' ');
            string[] splitedStr = str.Split(',');
            for (int i = 0; i < splitedStr.Length; i++)
            {
                string item = splitedStr[i];
                string strID=item.Split(divider)[0];
                getDicKey.Add(strID);//순서대로 삽입되는것이 전재 이 순서로 키값을 알아낼것이기때문.
                //revHorDickey.Add(strID,i);
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
        //이제 읽는것은 끝났으니 쓰기를 해볼까나

    }

    public void SaveDic(Dictionary<string,Dictionary<string,string>> savetarget)
    {
        //정보를 어떻게 저장할까.
        //일단 키가 뭔지 알아야 하는데.
        //오 포이치로 꺼내진다..
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
            //우선 키는 첫번째 스트링에 들어가야한다.
            firstLine += ","+item.Key+ "/" + IdToName(item.Key);
            //두번째 키는 다음 줄 첫번째에 들어가야 한다.
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
        //여기까지오면 항목은 작성이 된 것.
        //이후부터는 값을 넣는다.
        //리스트로 정렬한다. 값을 순서대로 넣기 위해서.

        //하지만 이것은 사람의 편의성으로 값을 넣는 방식이다.
        //처음에 읽어들일때부터의 가공되지않은 원시데이터를 직접 이용하게 하면 더 효율적인 운용이 될 것 같다.
        //조회 메서드를 별도로 만들면 원시데이터 그대로 써도 될 것 같은데.
        //내 생각은 이와 같다.
        //쓰기 읽기는 가장 원시적인 방법으로 한다. 이렇게하면 굳이 다시 배열에 넣을 필요가 없다. 읽은후에 그 값만 변경하고 다시 쓰면 되니까.
        //하지만 또다른고민은 어차피 저장과 불러오기는 시간이 걸리는 작업이라는것이다. 굳이 그럴필요가 있진않다.
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
        Dictionary<string, string> idSlashName = new Dictionary<string, string>();//들어가있을때 예시

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