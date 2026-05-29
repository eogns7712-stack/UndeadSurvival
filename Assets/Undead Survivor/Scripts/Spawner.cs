using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform[] spawnPoint;    // 자식 오브젝트의 트랜스폼을 담을 배열변수 선언
    public SpawnData[] spawnData;    // 만든 클래스를 그대로 타입으로 활용해 배열변수 선언
    public float levelTime; // 소환 레벨 구간을 결정하는 변수 선언
    int level;  // 레벨 담당 변수 선언
    float timer;    // 소환 타이머를 위한 변수 선언

    void Awake()
    {
        spawnPoint = GetComponentsInChildren<Transform>();  // 배열(다수)를 가져오는 것이기때문에 GetComponentsInChildren로 배열 초기화, 자기 자신도 포함
        levelTime = GameManager.instance.maxGameTime / spawnData.Length;    // 최대 시간에 몬스터 데이터 크기로 나누어 자동으로 구간시간 계산
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;
            
        timer += Time.deltaTime;    // 타이머 변수에 deltaTime 계속 더하기
        level = Mathf.Min(Mathf.FloorToInt(GameManager.instance.gameTime / levelTime), spawnData.Length - 1);
        // Mathf.Min() 함수를 사용해 인덱스 에러 방지
        // Mathf.FloorToInt : 소수점 아래는 버리고 Int형으로 바꾸는 함수
        // Mathf.CeilToInt : 소수점 아래를 올리고 Int형으로 바꾸는 함수

         if (timer > spawnData[level].spawnTime) // 현재 레벨에 맞는 spawnData의 spawnTime값 사용
        {
            timer = 0;  // 소환 후 timer=0 으로 초기화
            Spawn();
        }
        
        void Spawn()
        {
            GameObject enemy = GameManager.instance.pool.Get(0);    //게임매니저의 인스턴스에 접근해 풀링함수 호출
            enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position; // 랜덤 스폰위치 선택
            // 자식 오브젝트에서만 선택되도록 랜덤 시작은 1부터 (적이 플레이어 바로 위에 생성되는경우 방지)
            enemy.GetComponent<Enemy>().Init(spawnData[level]);  // 오브젝트 풀에서 가져온 오브젝트에서 Enemy컴포넌트로 접근
            // 새로 작성한 함수를 호출하고 소환데이터 인자값 전달
        }
    }
}

[System.Serializable]   // Serializable : 직렬화, 개체를 저장 또는 전송하기위해 변환
public class SpawnData  // 새로운 클래스 선언
{
    public float spawnTime; // 소환시간
    public int spriteType;  // 속성 추가 : 스프라이트 타입
    public float healthPoint;   // 체력
    public float speed; // 속도
}