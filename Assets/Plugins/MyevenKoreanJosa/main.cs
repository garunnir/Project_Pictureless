using System;
using UnityEngine;

namespace Myevan
{
    public class main:MonoBehaviour
    {
        void Start()
        {
            print(Korean.ReplaceJosa("네(이)가 잘못했어. 확률(이)가 이상해. 덫(이)가 깔렸어."));
            print(Korean.ReplaceJosa("너(와)과 함께 할게. 글(와)과 그림. 빛(와)과 어둠."));
            print(Korean.ReplaceJosa("수녀(을)를 존경했어. 남자들(을)를 입히다. 버튼(을)를 만지지 마."));
            print(Korean.ReplaceJosa("우리(은)는 끝이야. 쌀(은)는 필요없어. 갑옷(은)는 찢었다."));
            print(Korean.ReplaceJosa("진우(아)야, 그것도 몰라? 경렬(아)야, 진정해. 상현(아)야, 뭐해?"));
            print(Korean.ReplaceJosa("진우(이)여, 닥쳐라. 경렬(이)여, 이리 오라. 상현(이)여, 아무 일도 아니다."));
            print(Korean.ReplaceJosa("부두(으)로 가야 해. 대궐(으)로 가거나. 집(으)로 갈래?"));
            print(Korean.ReplaceJosa("나(이)라고 어쩔 수 있겠니? 별(이)라고 불러줘. 라면(이)라고 했잖아."));
            print(Korean.ReplaceJosa("라면(이)라면 어떨까? 밥(이)라능~"));
            print(Korean.ReplaceJosa("너(이)라면 어떨까? 나(이)라능~"));
        }
    }
}
