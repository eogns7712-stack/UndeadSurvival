using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombFragment : MonoBehaviour
{
    float damage;
    int remainingHits;
    float maxRange;
    float hitDelayTimer;
    Vector3 startPosition;
    Rigidbody2D rigid;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        if (rigid == null)
        {
            rigid = gameObject.AddComponent<Rigidbody2D>();
        }

        rigid.gravityScale = 0f;
    }

    public void Init(float damage, int remainingHits, float maxRange, float hitDelay, Vector3 dir)
    {
        this.damage = damage;
        this.remainingHits = remainingHits;
        this.maxRange = maxRange;
        hitDelayTimer = hitDelay;
        startPosition = transform.position;

        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody2D>();
        }

        if (rigid != null)
        {
            rigid.simulated = true;
            rigid.velocity = dir.normalized * 15f;
        }
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        if (hitDelayTimer > 0f)
        {
            hitDelayTimer -= Time.deltaTime;
        }

        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitDelayTimer > 0f)
            return;

        if (!collision.CompareTag("Enemy"))
            return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        remainingHits--;
        if (remainingHits <= 0)
        {
            ReturnToPool();
        }
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
