using UnityEngine;

/// 보스 탄막의 이동, 플레이어 충돌 및 풀 반환을 처리하는 스크립트.

public class BossBullet : MonoBehaviour
{
    public float damage = 10f;
    public float speed = 5f;
    public float maxRange = 18f;

    Rigidbody2D rigid;
    Vector3 startPosition;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector3 dir, float speed, float damage, float maxRange)
    {
        this.speed = speed;
        this.damage = damage;
        this.maxRange = maxRange;
        startPosition = transform.position;

        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody2D>();
        }

        rigid.velocity = dir.normalized * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
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
        if (rigid != null)
        {
            rigid.velocity = Vector2.zero;
        }

        gameObject.SetActive(false);
    }
}
