using UnityEngine;

// 보스 탄막의 이동, 플레이어 충돌 및 풀 반환을 처리하는 스크립트.

public class BossBullet : MonoBehaviour
{
    // 보스 탄막의 피해량, 이동속도, 최대 사거리를 저장하는 변수
    public float damage = 10f;
    public float speed = 5f;
    public float maxRange = 18f;

    Rigidbody2D rigid;

    // 풀에서 꺼낸 순간의 위치를 저장해 최대 사거리 계산에 사용.
    Vector3 startPosition;

    void Awake()
    {
        // 탄막 이동은 Rigidbody2D 속도로 처리.
        rigid = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector3 dir, float speed, float damage, float maxRange)
    {
        // 풀에서 재사용될 때마다 현재 패턴의 탄막 수치로 갱신.
        this.speed = speed;
        this.damage = damage;
        this.maxRange = maxRange;
        startPosition = transform.position;

        // 프리팹에 Rigidbody2D가 늦게 붙었거나 참조가 비어 있을 때 다시 찾는다.
        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody2D>();
        }

        rigid.velocity = dir.normalized * speed;

        // 탄막 스프라이트가 이동 방향을 바라보도록 회전.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        // 최대 사거리 이상 이동하면 화면 밖에서 계속 남지 않도록 비활성화 후 풀로 반환(게임최적화)
        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 보스 탄막은 태그가 플레이어인 오브젝트에게만 피해를 준다.
        if (!collision.CompareTag("Player"))
            return;

        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }

        ReturnToPool();
    }

    void ReturnToPool()
    {
        // 풀로 돌아가기 전 이전 속도를 제거해 다음 재사용에 섞이지 않게 한다.
        if (rigid != null)
        {
            rigid.velocity = Vector2.zero;
        }

        gameObject.SetActive(false);
    }
}
