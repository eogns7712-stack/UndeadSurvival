using System.Collections;
using UnityEngine;

// 일반 몬스터와 보스의 추적, 피격, 사망, 보상 드롭 및 재배치를 처리하는 스크립트.

public class Enemy : MonoBehaviour
{
    public float speed;
    public float healthPoint;   // 현재 체력 변수 추가
    public float maxhealthPoint;    // 최대 체력 변수 추가
    public bool isBoss; // 보스 몬스터인지 구분. 보스는 넉백/리스폰/사망 보상이 다르게 처리된다.
    public RuntimeAnimatorController[] animCon; // Animator Controller를 여러개 사용하기 위함, 적 종류마다 다른 애니메이션을 적용하기 위함
    public Rigidbody2D target;  // 속도, 목표, 생존여부를 위한 변수
    
    // [추가] 최적화를 위한 플레이어와의 비활성 한계 거리 선언
    public float despawnDistance = 22f; // 플레이어로부터 이 거리 이상 멀어지면 스폰 포인트로 복귀
    public float knockBackPower = 1.5f; // 일반 몬스터가 피격될 때 밀려나는 힘.
    public float respawnMinRadius = 10f;    // 멀어진 일반 몬스터를 플레이어 주변에 재배치할 최소 거리.
    public float respawnMaxRadius = 14f;    // 멀어진 일반 몬스터를 플레이어 주변에 재배치할 최대 거리.

    // [추가] 드롭할 보상 오브젝트 프리팹 ID 번호 (PoolManager 인덱스 매칭)
    public int expGemPrefabId = 5;       // 경험치 보석 프리팹 풀 인덱스
    public int randomBoxPrefabId = 6;    // 랜덤 기프트 상자 프리팹 풀 인덱스
    [Range(0f, 1f)] public float boxDropChance = 0.05f; // 랜덤 상자 드랍 확률 (5%)
    public float boxSpawnOffset = 0.2f; // 랜덤박스가 적 사망 위치에서 살짝 벗어나 생성되는 범위.
    public float boxBounceMinDistance = 0.8f;   // 랜덤박스 튕김 연출 최소 거리.
    public float boxBounceMaxDistance = 1.3f;   // 랜덤박스 튕김 연출 최대 거리.
    public float boxBounceDuration = 0.45f; // 랜덤박스 튕김 연출 시간.
    public Color bossHitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);  // 보스 피격 시 이동 애니메이션을 끊지 않고 깜빡일 색상.
    public float bossHitFlashDuration = 0.05f;  // 보스 피격 색상 유지 시간.

    bool isLive;    // 현재 Enemy가 살아 있는지 확인하는 내부 상태값.
    
    Rigidbody2D rigid;
    Collider2D coll;    // Collider2D 변수 생성, Capsule Collider 2D이지만 Collider 2D로 통일 가능
    SpriteRenderer spriter;
    Animator anim;
    WaitForFixedUpdate wait;    // 다음 FixedUpdate가 될 때 까지 기다리는 변수 선언
    Color defaultColor; // 피격 깜빡임 후 되돌릴 기본 색상.
    Coroutine bossHitFlashRoutine; // 보스 피격 깜빡임 코루틴 중복 실행 방지용 참조.

    void Awake()
    {   // 변수 초기화
        rigid = GetComponent<Rigidbody2D>();    
        coll = GetComponent<Collider2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        wait = new WaitForFixedUpdate();
        defaultColor = spriter.color;
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.isLive)   // 게임이 진행 중이 아니면 이동 중단.
            return;

        if (!isLive || (!isBoss && anim.GetCurrentAnimatorStateInfo(0).IsName("Hit")))    // GetCurrentAnimatorStateInfo : 현재 재생되는 애니메이션 상태 정보를 가져오는 함수, IsName() : 해당 상태의 이름이 지정된 것과 같은지 확인하는 함수, Enemy가 살아있지 않다면 or 현재 재생중인 애니메이션 상태의 이름이 'Hit'라면 return;
            return;
        
        Vector2 dirVec = target.position - rigid.position;   // 위치차이 = 타겟(플레이어)의 위치 - 나(Enemy)의 위치
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime; // 방향 = 위치차이 정규화 normalized
        rigid.MovePosition(rigid.position + nextVec);   // 현재 위치에서 플레이어의 키입력 값을 더한 이동
        rigid.velocity = Vector2.zero;    //물리 속도가 이동에 영향을 주지않도록 물리속도 제거

        // 몬스터가 플레이어와 너무 멀어지면 플레이어 가시영역 밖 근처로 재생성 및 리스폰 위치 재배치
        float distanceToPlayer = Vector2.Distance(transform.position, GameManager.instance.player.transform.position);
        if (!isBoss && distanceToPlayer > despawnDistance) // 일반 몬스터만 멀리 떨어졌을 때 재배치. 보스는 강제이동하지 않음.
        {
            RepositionNearPlayer();
        }
    }

    // 화면에서 지나치게 멀어진 일반 몬스터를 플레이어 주변(spawner의 위치)으로 재배치.
    void RepositionNearPlayer()
    {
        Vector3 playerPos = GameManager.instance.player.transform.position; // 재배치 기준이 되는 플레이어 위치.
        float angle = Random.Range(0f, Mathf.PI * 2f);  // 360도 중 랜덤 방향 선택.
        float radius = Random.Range(respawnMinRadius, respawnMaxRadius); // 화면 밖 스폰 거리 유지
        
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        transform.position = playerPos + spawnOffset;
    }

    // 외부 총알 타격 등에 데미지를 전달하기 위한 타격용 Public 함수
    public void TakeDamage(float damage)
    {
        healthPoint -= damage; // 현재 체력에서 받은 데미지 차감.
        if (isBoss) // 보스는 Hit 애니메이션 대신 색상 깜빡임만 처리해 이동이 끊기지 않게 함.
        {
            if (bossHitFlashRoutine != null)
            {
                StopCoroutine(bossHitFlashRoutine);
                spriter.color = defaultColor;
            }
            bossHitFlashRoutine = StartCoroutine(BossHitFlash());
        }
        else    // 일반 몬스터는 피격 시 넉백 적용.
        {
            StartCoroutine(knockBack());
        }

        if (healthPoint > 0)
        {
            if (!isBoss)
            {
                anim.SetTrigger("Hit");
            }
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Hit); // Enemy 피격시 효과음 재생
        }
        else
        {
            Die();
        }
    }

    public void Dead()
    {
        gameObject.SetActive(false); // 애니메이션이 끝나 비활성화 처리될 때 풀로 안전하게 반환
    }

    void Die()
    {
        isLive = false; // 여러 로직을 결정하는 isLive 변수를 false로 변경
        spriter.color = defaultColor;
        coll.enabled = false;   // 컴포넌트 비활성화는 enabled = false
        rigid.simulated = false;    // Rigidbody의 물리적 비활성화는 rigid.simulated = false
        spriter.sortingOrder = 1;   // Dead상태 Enemy의 Order in Layer 1로 변경
        anim.SetBool("Dead",true);  // Animator의 트리거가 bool로 되어있기 때문에 SetBool을 통해 Dead 상태로 변환
        GameManager.instance.kill++;

        if (isBoss) // 보스 사망 보상과 보스 클리어 연출 실행.
        {
            GameManager.instance.AddShopCurrency(GameManager.instance.bossShopCurrencyReward);
            GameManager.instance.BossDead(transform.position);
        }
        else    // 일반 몬스터는 1재화와 경험치/랜덤박스 드롭 처리.
        {
            GameManager.instance.AddShopCurrency(1);
            DropRewards();
        }

        if (GameManager.instance.isLive)    // Enemy 사망 사운드는 게임종료시에는 나지 않도록 조건추가
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead); // Enemy 사망시 효과음 재생
    }

    // [추가] 드롭 시스템 구현
    // 처치 보상과 확률형 랜덤박스를 풀에서 꺼내 배치.
    void DropRewards()
    {
        // 1. 경험치 보석 드랍
        GameObject gem = GameManager.instance.pool.Get(expGemPrefabId);    // 경험치 보석을 오브젝트 풀에서 꺼내기.
        if (gem != null)
        {
            gem.transform.position = transform.position;   // 적이 죽은 위치에 경험치 보석 배치.
            ItemPickup pickup = gem.GetComponent<ItemPickup>();    // 획득 처리를 담당하는 ItemPickup 가져오기.
            if (pickup != null)
            {
                pickup.InitPickup(ItemPickup.PickupType.Exp, 1); // 1 경험치 보석
            }
        }

        // 2. 적은 확률로 Random Box 드롭
        if (Random.value < Mathf.Clamp01(boxDropChance + GameManager.instance.ShopBoxDropChanceBonus)) // 기본 확률 + 상점 보너스 확률로 랜덤박스 드롭.
        {
            GameObject box = GameManager.instance.pool.Get(randomBoxPrefabId);
            if (box != null)
            {
                box.transform.position = transform.position + new Vector3(Random.Range(-boxSpawnOffset, boxSpawnOffset), Random.Range(-boxSpawnOffset, boxSpawnOffset), 0);
                
                // ItemPickup 초기화
                ItemPickup pickup = box.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    pickup.InitPickup(ItemPickup.PickupType.RandomBox, 0);
                    Vector3 randomOffset = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.up * Random.Range(boxBounceMinDistance, boxBounceMaxDistance); // 랜덤 방향과 거리 계산.
                    pickup.StartBounce(transform.position, transform.position + randomOffset, boxBounceDuration);
                }

                // BoxOpen 컴포넌트 강제 상태 리셋
                BoxOpen boxOpen = box.GetComponent<BoxOpen>();
                if (boxOpen != null)
                {
                    boxOpen.ResetBoxState();
                }
            }
        }
    }

    void LateUpdate()
    {
        if (!GameManager.instance.isLive)
            return;
            
        if (!isLive)    // isLive의 반환값이 flase면
            return;

        spriter.flipX = target.position.x < rigid.position.x;    // 목표의 X축값과 자신의 X축 값을 비교해서 작으면 true가 되도록 설정
    }

    void OnEnable() //Unity에서 제공하는, 스크립트가 활성화 될 때 호출되는 이벤트함수
    {
        target = GameManager.instance.player.GetComponent<Rigidbody2D>();   
        // Enemy가 생성될 때 target 초기화, player의 Rigidbody2D를 따라가도록
        isLive = true; // 여러 로직을 결정하는 isLive 변수를 false로 변경
        coll.enabled = true;   // 컴포넌트 비활성화는 enabled = false
        rigid.simulated = true;    // Rigidbody의 물리적 비활성화는 rigid.simulated = false
        spriter.sortingOrder = 2;   // Dead상태 Enemy의 Order in Layer 1로 변경
        spriter.color = defaultColor;
        anim.SetBool("Dead",false);  // Animator의 트리거가 bool로 되어있기 때문에 SetBool을 통해 Dead 상태로 변환  // enemy가 생성될 때 isLive 활성화
        healthPoint = maxhealthPoint;   // 현재체력을 최대체력값으로 변경
    }

    public void Init(SpawnData data)  // 매개변수로 소환데이터 하나 지정
    {   // 적 종류 (spriteType)에 따라 애니메이션 컨트롤러를 바꾸는 코드
        anim.runtimeAnimatorController = animCon[data.spriteType];    // 매개변수의 속성을 본스터 속성 변경에 활용
        speed = data.speed;
        maxhealthPoint = data.healthPoint;
        healthPoint = data.healthPoint;
        isBoss = data.isBoss;  // SpawnData에 설정된 보스 여부 적용.
        transform.localScale = Vector3.one * (data.scale > 0f ? data.scale : 1f);  // 보스/몬스터 크기 적용. 0 이하 값은 기본 1로 보정.

        isLive = true;
        coll.enabled = true;
        rigid.simulated = true;
        spriter.sortingOrder = 2;
        spriter.color = defaultColor;
        anim.SetBool("Dead", false);
    }

    void OnTriggerEnter2D(Collider2D collision) // Unity에서 제공하는 이벤트함수, Trigger Collider에 다른 Collider가 들어왔을 때 자동실행
    {
        if (!collision.CompareTag("Bullet") || !isLive) // OnTriggerEnter2D 매개변수의 태그를 조건으로 활용
            // 사망로직이 연달아 실행되는 것을 방지하기 위한 조건 추가(영상09 27:58)
            return; // 충돌한 오브젝트의 태그가 'Bullet'이 아니면 즉시 리턴
        
        // Player가 직접 체력을 깎고 사망 처리를 중복 실행하던 구조에서 -> 일관적으로 TakeDamage를 관통하여 호출하도록 수정.
        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet != null)
        {
            TakeDamage(bullet.damage);
        }
    }

    // 코루틴(Coroutine) : 생명주기와 비동기처럼 실행되는 함수
    IEnumerator knockBack() // IEnumerator : 코루틴만의 반환형 인터페이스, I가 붙으면 인터페이스라 부름
    {
        yield return wait;    // yield : 코루틴의 반환 키워드
        Vector3 playerPos = GameManager.instance.player.transform.position; // playerPos 변수에 GameManager에 있는 Player의 위치 저장
        Vector3 dirVec = transform.position - playerPos;    // 플레이어 기준의 반대방향 : Enemy의 현재위치 - 플레이어의 위치
        rigid.AddForce(dirVec.normalized * knockBackPower, ForceMode2D.Impulse);  // AddForce 함수로 dirVec 방향으로 힘 가하기, 순간적인 힘이므로 ForceMode2D.Impulse 추가
    }

    IEnumerator BossHitFlash()  // 보스의 피격 깜빡임을 출력하는 코루틴.
    {
        spriter.color = bossHitFlashColor; // 피격 색상으로 잠시 변경.
        yield return new WaitForSeconds(bossHitFlashDuration); // 설정된 시간만큼 유지.
        spriter.color = defaultColor;  // 원래 색상으로 복구.
        bossHitFlashRoutine = null;    // 다음 피격 때 새 코루틴을 시작할 수 있게 참조 초기화.
    }
}
