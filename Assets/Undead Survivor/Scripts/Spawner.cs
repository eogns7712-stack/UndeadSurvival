using UnityEngine;

/// 시간대별 일반 몬스터와 보스의 생성 위치 및 능력치를 관리하는 스크립트.

public class Spawner : MonoBehaviour
{
    public Transform[] spawnPoint;    // 자식 오브젝트의 트랜스폼을 담을 배열변수 선언
    public SpawnData[] spawnData;    // 만든 클래스를 그대로 타입으로 활용해 배열변수 선언
    public SpawnData bossData; // 보스 전용 생성 데이터.
    public int bossPoolId; // PoolManager에서 보스 프리팹이 들어있는 인덱스.
    public float bossSpawnDistance = 8f;    // 보스가 플레이어 오른쪽 바깥에 등장할 거리.
    public float levelTime; // 소환 레벨 구간을 결정하는 변수 선언
    int level;  // 레벨 담당 변수 선언
    float timer;    // 소환 타이머를 위한 변수 선언
    bool hasBossSpawned = false;    // 한 판에서 보스가 중복 생성되지 않도록 막는 값.

    void Awake()
    {
        spawnPoint = GetComponentsInChildren<Transform>();  // 배열(다수)를 가져오는 것이기때문에 GetComponentsInChildren로 배열 초기화, 자기 자신도 포함
        levelTime = GameManager.instance.maxGameTime / spawnData.Length;    // 최대 시간에 몬스터 데이터 크기로 나누어 자동으로 구간시간 계산
    }

    void Update()
    {
        if (!CanSpawnNormalEnemies())   // 게임 종료, 보스전, 생존시간 종료 상태에서는 일반 몬스터 생성 중단.
        {
            timer = 0f; // 생성 조건이 아닐 때 타이머를 초기화해 재개 시 즉시 생성되는 것 방지.
            return;
        }
            
        timer += Time.deltaTime;    // 타이머 변수에 deltaTime 계속 더하기
        level = Mathf.Min(Mathf.FloorToInt(GameManager.instance.gameTime / levelTime), spawnData.Length - 1);
        // Mathf.Min() 함수를 사용해 인덱스 에러 방지
        // Mathf.FloorToInt : 소수점 아래는 버리고 Int형으로 바꾸는 함수
        // Mathf.CeilToInt : 소수점 아래를 올리고 Int형으로 바꾸는 함수

         if (timer > spawnData[level].spawnTime * GameManager.instance.ShopEnemySpawnRate) // 현재 레벨에 맞는 spawnData의 spawnTime값 사용
        {
            timer = 0;  // 소환 후 timer=0 으로 초기화
            SpawnNormalEnemy(); // 현재 시간 구간에 맞는 일반 몬스터 생성.
        }
    }

    // 일반 몬스터를 생성해도 되는 상태인지 확인하는 함수.
    bool CanSpawnNormalEnemies()
    {
        if (!GameManager.instance.isLive || GameManager.instance.isBossBattle)  // 게임이 멈췄거나 보스전이면 잡몹 생성 중단.
            return false;

        // 현재 플레이 경과 시간이 설정된 Max Game Time을 넘어섰을 때
        if (GameManager.instance.gameTime >= GameManager.instance.maxGameTime)
        {
            // 1. 일반 몬스터의 소출 타이머 연산을 강제 봉쇄하여 잡몹 출현 중단
            return false;
        }

        return true;
    }

    void SpawnNormalEnemy()
    {
        GameObject enemy = GameManager.instance.pool.Get(0);    // 게임매니저의 인스턴스에 접근해 풀링함수 호출
        enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position; // 랜덤 스폰위치 선택
        // 자식 오브젝트에서만 선택되도록 랜덤 시작은 1부터 (적이 플레이어 바로 위에 생성되는경우 방지)
        enemy.GetComponent<Enemy>().Init(spawnData[level]);  // 오브젝트 풀에서 가져온 오브젝트에서 Enemy컴포넌트로 접근
        // 새로 작성한 함수를 호출하고 소환데이터 인자값 전달
    }
    // Spawner.cs 혹은 GameManager.cs 소환 제어부의 예시 흐름

    // 보스전 시작 시 보스 몬스터를 한 번만 생성하고 초기화하는 함수.
    public Enemy SpawnBossMonster()
    {
        if (hasBossSpawned) // 이미 보스가 생성된 상태라면 중복 생성 방지.
            return null;

        hasBossSpawned = true;  // 이후 호출에서는 보스가 다시 생성되지 않도록 표시.

        // 1. 오브젝트 풀러에서 보스 외형 애니메이터가 들어갈 수 있는 Enemy 인스턴스를 하나 사용.
        GameObject boss = GameManager.instance.pool.Get(bossPoolId); // 보스 전용 풀 ID에서 보스 오브젝트 가져오기.
        if (boss == null) return null;

        // 2. 캐릭터의 가시 바깥 근처(약 12유닛 지점)로 소환 위치 연산
        Vector3 playerPos = GameManager.instance.player.transform.position; // 보스 소환 기준이 되는 플레이어 위치.
        boss.transform.position = playerPos + new Vector3(bossSpawnDistance, 0f, 0f);   // 플레이어 오른쪽 일정 거리 밖에 보스 배치.

        Enemy enemyComponent = boss.GetComponent<Enemy>(); // 보스 능력치 초기화를 위해 Enemy 컴포넌트 가져오기.
        if (enemyComponent != null)
        {   // 3. 보스 전용 스펙 데이터 초기화 전달
            if (bossData == null)
            {
                bossData = new SpawnData();    // 인스펙터에 BossData가 비어 있으면 기본 데이터 생성.
                bossData.spriteType = 0;
                bossData.healthPoint = 10000f;
                bossData.speed = 1.0f;
            }

            bossData.isBoss = true; // 보스 전용 피격/사망/리스폰 예외 처리를 위해 true 고정.
            if (bossData.scale <= 0f)  // 보스 크기가 0 이하로 잘못 설정되면 기본 3배 크기로 보정.
            {
                bossData.scale = 3.0f;
            }

            enemyComponent.Init(bossData); // Enemy 스크립트에 보스 데이터 전달.

            boss.transform.localScale = Vector3.one * bossData.scale;   // 4. 보스 몬스터의 몸체 크기(Scale)를 3배 키우기.
        }

        return enemyComponent;  // GameManager가 BossHP UI와 줌인 연출에 사용할 Enemy 반환.
    }
}

/// <summary>
/// 몬스터 한 종류의 생성 주기, 외형, 능력치와 보스 여부를 보관한다.
/// </summary>
[System.Serializable]   // Serializable : 직렬화, 개체를 저장 또는 전송하기위해 변환
public class SpawnData  // 새로운 클래스 선언
{
    public float spawnTime; // 소환시간
    public int spriteType;  // 속성 추가 : 스프라이트 타입
    public float healthPoint;   // 체력
    public float speed; // 속도
    public bool isBoss; // 보스 여부.
    public float scale = 1f;    // 몬스터 크기 배율.
}
