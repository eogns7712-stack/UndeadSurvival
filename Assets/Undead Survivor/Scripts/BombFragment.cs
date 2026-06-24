using UnityEngine;

// 파편 수류탄(1단계 이상 초월된 수류탄)에서 생성되는 풀링 투사체의 이동, 충돌 및 반환을 처리하는 스크립트.

public class BombFragment : MonoBehaviour
{
    float damage;  // 파편이 적에게 줄 데미지.
    int remainingHits; // 파편이 몇 번 적중한 뒤 사라질지 저장하는 값.
    float maxRange;    // 파편이 시작 위치에서 얼마나 멀리 날아갈 수 있는지 제한하는 값.
    float hitDelayTimer;   // 생성 직후 바로 충돌하는 것을 막기 위한 짧은 지연 시간.
    Vector3 startPosition; // 파편이 생성된 시작 위치. 최대 사거리 계산에 사용.
    Rigidbody2D rigid; // 파편 이동을 담당하는 Rigidbody2D.

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();    // 프리팹에 붙어있는 Rigidbody2D 가져오기.
        if (rigid == null)  // 혹시 프리팹에 Rigidbody2D가 없으면 런타임에 추가해 오류 방지.
        {
            rigid = gameObject.AddComponent<Rigidbody2D>();
        }

        rigid.gravityScale = 0f;    // 탑다운 게임이므로 중력 영향 제거.
    }

    // 수류탄 폭발 시 풀에서 꺼낸 파편의 데미지, 적중 횟수, 사거리, 이동방향을 초기화하는 함수.
    public void Init(float damage, int remainingHits, float maxRange, float hitDelay, float speed, Vector3 dir)
    {
        this.damage = damage;  // 파편 데미지 저장.
        this.remainingHits = remainingHits;    // 남은 적중 횟수 저장.
        this.maxRange = maxRange;  // 최대 사거리 저장.
        hitDelayTimer = hitDelay;  // 생성 직후 충돌 지연 시간 저장.
        startPosition = transform.position; // 현재 위치를 시작 위치로 저장.

        if (rigid == null)  // 풀에서 재사용될 때 rigid 참조가 비어 있으면 다시 가져오기.
        {
            rigid = GetComponent<Rigidbody2D>();
        }

        if (rigid != null)  // Rigidbody2D가 있을 때만 이동 속도 적용.
        {
            rigid.simulated = true; // 물리 시뮬레이션 활성화.
            rigid.velocity = dir.normalized * speed;   // 지정한 방향과 속도로 파편 발사.
        }
    }

    void Update()
    {
        if (!GameManager.instance.isLive)   // 게임이 진행 중이 아니면 이동거리 검사 중단.
            return;

        if (hitDelayTimer > 0f) // 충돌 지연 시간이 남아 있으면 감소.
        {
            hitDelayTimer -= Time.deltaTime;
        }

        if (Vector3.Distance(startPosition, transform.position) >= maxRange)    // 시작 위치에서 최대 사거리 이상 멀어지면 풀로 반환.
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitDelayTimer > 0f) // 생성 직후 자기 폭발 범위나 주변 콜라이더에 바로 맞는 것 방지.
            return;

        if (!collision.CompareTag("Enemy")) // 적이 아닌 오브젝트와 충돌하면 무시.
            return;

        Enemy enemy = collision.GetComponent<Enemy>();  // 충돌한 적의 Enemy 컴포넌트 가져오기.
        if (enemy != null)  // Enemy 컴포넌트가 있으면 데미지 적용.
        {
            enemy.TakeDamage(damage);
        }

        remainingHits--;   // 적중 횟수 차감.
        if (remainingHits <= 0) // 남은 적중 횟수가 없으면 풀로 반환.
        {
            ReturnToPool();
        }
    }

    // 파편 이동을 멈추고 오브젝트 풀로 반환하는 함수.
    void ReturnToPool()
    {
        if (rigid != null)  // 재사용 시 이전 속도가 남지 않도록 정지.
        {
            rigid.velocity = Vector2.zero;
        }

        gameObject.SetActive(false);   // 비활성화해서 PoolManager가 다시 재사용할 수 있게 반환.
    }
}
