using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    // 프리팹들을 보관할 변수
    public GameObject[] prefabs; // 프리팹도 게임오브젝트임, 프리팹을 저장할 배열 변수 선언

    // 풀 담당을 하는 리스트들
    List<GameObject>[] pools; //오브젝트 풀들을 저장할 배열 변수 선언

    void Awake()
    {
        pools = new List<GameObject>[prefabs.Length];    // pools 배열 초기화, pools는 리스트기때문에 new로 생성

        for (int index = 0; index < pools.Length; index++)  // 반복문으로 모든 오브젝트 풀리스트 초기화
        {
            pools[index] = new List<GameObject>();
        }
    }

    public GameObject Get(int index)    // 게임 오브젝트를 반환하는 함수 선언, 가져올 오브젝트의 종류를 결정하는 매개변수 int index
    {
        GameObject select = null;   // 게임오브젝트 지역변수와 리턴을 작성

        // ...선택한 풀의 놀고있는(비활성화된) 게임오브젝트 접근
        foreach (GameObject item in pools[index])    // foreach : 배열, 리스트들의 데이터를 순차적으로 접근하는 반복문
        {
            if (!item.activeSelf)    //내용물 오브젝트가 비활성화 상태인지 확인, activeSelf == false
            {   // ... 비활성화 게임오브젝트를 발견하면 select 변수에 할당
                select = item;
                select.SetActive(true); // 발견된 비활성화 오브젝트를 활성화
                break;  // 더이상 찾을 필요가 없으니 반복종료
            }
        }
        
        // ... 만약 비활성화된 오브젝트를 못찾았으면    [ 최적화 ]
        if (!select)    // if (select == null)
        {// ... 새롭게 생성해 select 변수에 할당
            select = Instantiate(prefabs[index], transform);    // 해당 종류의 새 오브젝트를 만들어라 == prefabs[index]에 있는 프리팹을 복제해 생성, transform을 부모로 설정
            pools[index].Add(select);    //생성한 오브젝트는 해당 오브젝트 풀리스트에 Add함수로 추가, 다음부터 재사용 할 수 있게 하기위함
        }

        return select;
    }   
}
