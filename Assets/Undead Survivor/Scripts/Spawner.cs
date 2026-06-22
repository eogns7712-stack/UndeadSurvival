using UnityEngine;

/// 시간대별 일반 몬스터와 보스의 생성 위치 및 능력치를 관리하는 스크립트.

public class Spawner : MonoBehaviour
{
    public Transform[] spawnPoint;    // 자식 오브젝트의 트랜스폼을 담을 배열변수 선언
    public SpawnData[] spawnData;    // 만든 클래스를 그대로 타입으로 활용해 배열변수 선언
    public SpawnData bossData;
    public int bossPoolId;
    public float bossSpawnDistance = 8f;
    public float levelTime; // 소환 레벨 구간을 결정하는 변수 선언
    int level;  // 레벨 담당 변수 선언
    float timer;    // 소환 타이머를 위한 변수 선언
    bool hasBossSpawned = false;

    void Awake()
    {
        spawnPoint = GetComponentsInChildren<Transform>();  // 배열(다수)를 가져오는 것이기때문에 GetComponentsInChildren로 배열 초기화, 자기 자신도 포함
        levelTime = GameManager.instance.maxGameTime / spawnData.Length;    // 최대 시간에 몬스터 데이터 크기로 나누어 자동으로 구간시간 계산
    }

    void Update()
    {
        if (!CanSpawnNormalEnemies())
        {
            timer = 0f;
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
            SpawnNormalEnemy();
        }
    }

    bool CanSpawnNormalEnemies()
    {
        if (!GameManager.instance.isLive || GameManager.instance.isBossBattle)
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

    public Enemy SpawnBossMonster()
    {
        if (hasBossSpawned)
            return null;

        hasBossSpawned = true;

        // 1. 오브젝트 풀러에서 보스 외형 애니메이터가 들어갈 수 있는 Enemy 인스턴스를 하나 사용.
        GameObject boss = GameManager.instance.pool.Get(bossPoolId); // 0번 풀 재사용
        if (boss == null) return null;

        // 2. 캐릭터의 가시 바깥 근처(약 12유닛 지점)로 소환 위치 연산
        Vector3 playerPos = GameManager.instance.player.transform.position;
        boss.transform.position = playerPos + new Vector3(bossSpawnDistance, 0f, 0f);

        Enemy enemyComponent = boss.GetComponent<Enemy>();
        if (enemyComponent != null)
        {   // 3. 보스 전용 스펙 데이터 초기화 전달
            if (bossData == null)
            {
                bossData = new SpawnData();
                bossData.spriteType = 0;
                bossData.healthPoint = 10000f;
                bossData.speed = 1.0f;
            }

            bossData.isBoss = true;
            if (bossData.scale <= 0f)
            {
                bossData.scale = 3.0f;
            }

            enemyComponent.Init(bossData);

            boss.transform.localScale = Vector3.one * bossData.scale;   // 4. 보스 몬스터의 몸체 크기(Scale)를 3배 키우기.
        }

        return enemyComponent;
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
    public bool isBoss;
    public float scale = 1f;
}

