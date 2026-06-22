using System.Collections;
using UnityEngine;

/// 보스의 탄막 패턴과 페이즈별 공격 강화, 공격 예고선을 관리하는 스크립트.

public class BossAttack : MonoBehaviour
{
    public int bossBulletPrefabId = 10;
    public float bulletDamage = 10f;
    public float bulletSpeed = 5f;
    public float bulletMaxRange = 18f;
    public float patternInterval = 2.5f;
    public float firstAttackDelay = 1f;
    public float telegraphDelay = 0.5f;
    public float warningLineLength = 18f;
    public float warningLineWidth = 0.08f;
    public Color warningLineColor = new Color(1f, 0f, 0f, 0.45f);

    [Header("# Pattern 1")]
    public int circleBulletCount = 16;

    [Header("# Pattern 2")]
    public int aimedBurstCount = 4;
    public int aimedBulletCount = 3;
    public float aimedBurstInterval = 0.25f;
    public float aimedSpreadAngle = 12f;

    [Header("# Pattern 3")]
    public Transform[] patternSpawnPoints;
    public float spawnPointBulletSpeed = 5f;

    [Header("# Phase")]
    public float phase2HealthRate = 0.7f;
    public float phase3HealthRate = 0.4f;
    public float phase2PatternInterval = 2.0f;
    public float phase3PatternInterval = 1.4f;
    public int phase2ExtraCircleBulletCount = 8;
    public int phase3ExtraCircleBulletCount = 16;
    public int phase2ExtraAimedBulletCount = 1;
    public int phase3ExtraAimedBulletCount = 2;

    Enemy enemy;
    Coroutine attackRoutine;
    LineRenderer[] warningLines;
    Material warningMaterial;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        warningMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    void OnEnable()
    {
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        HideWarningLines();
    }

    // 현재 페이즈의 공격 간격을 적용하며 세 패턴 중 하나를 무작위로 실행.
    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(firstAttackDelay);

        while (true)
        {
            if (GameManager.instance != null && GameManager.instance.isLive && GameManager.instance.isBossBattle && enemy != null && enemy.isBoss)
            {
                int patternIndex = Random.Range(0, 3);

                if (patternIndex == 0)
                {
                    yield return StartCoroutine(FireCircle());
                }
                else if (patternIndex == 1)
                {
                    yield return StartCoroutine(FireAimedBurst());
                }
                else
                {
                    yield return StartCoroutine(FireFromSpawnPoints());
                }
            }

            yield return new WaitForSeconds(GetCurrentPatternInterval());
        }
    }

    IEnumerator FireCircle()
    {
        int count = Mathf.Max(1, GetCurrentCircleBulletCount());
        float angleStep = 360f / count;
        Vector3[] dirs = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i;
            dirs[i] = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
        }

        ShowWarningLines(transform.position, dirs);
        yield return new WaitForSeconds(telegraphDelay);
        HideWarningLines();

        for (int i = 0; i < dirs.Length; i++)
        {
            FireBullet(transform.position, dirs[i]);
        }
    }

    IEnumerator FireAimedBurst()
    {
        for (int i = 0; i < aimedBurstCount; i++)
        {
            Vector3 targetPos = GetPlayerPosition();
            Vector3 baseDir = targetPos - transform.position;
            Vector3[] dirs = GetSpreadDirs(baseDir, GetCurrentAimedBulletCount(), aimedSpreadAngle);
            ShowWarningLines(transform.position, dirs);
            yield return new WaitForSeconds(telegraphDelay);
            HideWarningLines();

            for (int dirIndex = 0; dirIndex < dirs.Length; dirIndex++)
            {
                FireBullet(transform.position, dirs[dirIndex]);
            }

            yield return new WaitForSeconds(aimedBurstInterval);
        }
    }

    IEnumerator FireFromSpawnPoints()
    {
        Transform[] points = GetPatternSpawnPoints(out int startIndex);
        if (points == null || points.Length <= startIndex)
            yield break;

        Vector3 targetPos = GetPlayerPosition();
        Vector3[] origins = new Vector3[points.Length - startIndex];
        Vector3[] dirs = new Vector3[points.Length - startIndex];
        int warningCount = 0;

        for (int pointIndex = startIndex; pointIndex < points.Length; pointIndex++)
        {
            Transform point = points[pointIndex];
            if (point == null)
                continue;

            origins[warningCount] = point.position;
            dirs[warningCount] = targetPos - point.position;
            warningCount++;
        }

        ShowWarningLines(origins, dirs, warningCount);
        yield return new WaitForSeconds(telegraphDelay);
        HideWarningLines();

        for (int i = 0; i < warningCount; i++)
        {
            FireBullet(origins[i], dirs[i], spawnPointBulletSpeed);
        }
    }

    void FireSpread(Vector3 origin, Vector3 baseDir, int count, float spreadAngle)
    {
        Vector3[] dirs = GetSpreadDirs(baseDir, count, spreadAngle);

        for (int i = 0; i < dirs.Length; i++)
        {
            FireBullet(origin, dirs[i]);
        }
    }

    Vector3[] GetSpreadDirs(Vector3 baseDir, int count, float spreadAngle)
    {
        count = Mathf.Max(1, count);

        Vector3[] dirs = new Vector3[count];

        if (count == 1)
        {
            dirs[0] = baseDir;
            return dirs;
        }

        float startAngle = -spreadAngle * 0.5f;
        float angleStep = spreadAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            dirs[i] = Quaternion.Euler(0f, 0f, startAngle + angleStep * i) * baseDir.normalized;
        }

        return dirs;
    }

    void FireBullet(Vector3 origin, Vector3 dir)
    {
        FireBullet(origin, dir, bulletSpeed);
    }

    void FireBullet(Vector3 origin, Vector3 dir, float speed)
    {
        if (GameManager.instance == null || GameManager.instance.pool == null || dir == Vector3.zero)
            return;

        GameObject bullet = GameManager.instance.pool.Get(bossBulletPrefabId);
        if (bullet == null)
            return;

        bullet.transform.position = origin;

        BossBullet bossBullet = bullet.GetComponent<BossBullet>();
        if (bossBullet != null)
        {
            bossBullet.Init(dir, speed, bulletDamage, bulletMaxRange);
        }
    }

    Vector3 GetPlayerPosition()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            return GameManager.instance.player.transform.position;
        }

        return transform.position;
    }

    // 일반 스폰 지점 중 보스 자신을 제외한 탄막 발사 지점만 반환.
    Transform[] GetPatternSpawnPoints(out int startIndex)
    {
        startIndex = 0;

        if (patternSpawnPoints != null && patternSpawnPoints.Length > 0)
        {
            return patternSpawnPoints;
        }

        if (GameManager.instance != null && GameManager.instance.spawner != null && GameManager.instance.spawner.spawnPoint != null && GameManager.instance.spawner.spawnPoint.Length > 1)
        {
            startIndex = 1;
            return GameManager.instance.spawner.spawnPoint;
        }

        return null;
    }

    // 남은 체력 비율을 기준으로 1~3페이즈를 결정.
    int GetCurrentPhase()
    {
        if (enemy == null || enemy.maxhealthPoint <= 0f)
            return 1;

        float rate = enemy.healthPoint / enemy.maxhealthPoint;
        if (rate <= phase3HealthRate)
            return 3;

        if (rate <= phase2HealthRate)
            return 2;

        return 1;
    }

    float GetCurrentPatternInterval()
    {
        int phase = GetCurrentPhase();
        if (phase >= 3)
            return phase3PatternInterval;

        if (phase >= 2)
            return phase2PatternInterval;

        return patternInterval;
    }

    int GetCurrentCircleBulletCount()
    {
        int phase = GetCurrentPhase();
        if (phase >= 3)
            return circleBulletCount + phase3ExtraCircleBulletCount;

        if (phase >= 2)
            return circleBulletCount + phase2ExtraCircleBulletCount;

        return circleBulletCount;
    }

    int GetCurrentAimedBulletCount()
    {
        int phase = GetCurrentPhase();
        if (phase >= 3)
            return aimedBulletCount + phase3ExtraAimedBulletCount;

        if (phase >= 2)
            return aimedBulletCount + phase2ExtraAimedBulletCount;

        return aimedBulletCount;
    }

    void ShowWarningLines(Vector3 origin, Vector3[] dirs)
    {
        Vector3[] origins = new Vector3[dirs.Length];
        for (int i = 0; i < origins.Length; i++)
        {
            origins[i] = origin;
        }

        ShowWarningLines(origins, dirs, dirs.Length);
    }

    void ShowWarningLines(Vector3[] origins, Vector3[] dirs, int count)
    {
        EnsureWarningLines(count);

        for (int i = 0; i < warningLines.Length; i++)
        {
            bool active = i < count && dirs[i] != Vector3.zero;
            warningLines[i].gameObject.SetActive(active);
            if (!active)
                continue;

            warningLines[i].startColor = warningLineColor;
            warningLines[i].endColor = warningLineColor;
            warningLines[i].startWidth = warningLineWidth;
            warningLines[i].endWidth = warningLineWidth;
            warningLines[i].SetPosition(0, origins[i]);
            warningLines[i].SetPosition(1, origins[i] + dirs[i].normalized * warningLineLength);
        }
    }

    // 필요한 수만큼만 예고선 오브젝트를 생성해 이후 패턴에서 재사용하는 오브젝트 풀링 구조.
    void EnsureWarningLines(int count)
    {
        if (warningLines != null && warningLines.Length >= count)
            return;

        LineRenderer[] newLines = new LineRenderer[count];
        if (warningLines != null)
        {
            for (int i = 0; i < warningLines.Length; i++)
            {
                newLines[i] = warningLines[i];
            }
        }

        for (int i = warningLines != null ? warningLines.Length : 0; i < count; i++)
        {
            GameObject lineObject = new GameObject("BossWarningLine");
            lineObject.transform.SetParent(transform);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.material = warningMaterial;
            line.sortingOrder = 20;
            lineObject.SetActive(false);
            newLines[i] = line;
        }

        warningLines = newLines;
    }

    void HideWarningLines()
    {
        if (warningLines == null)
            return;

        for (int i = 0; i < warningLines.Length; i++)
        {
            if (warningLines[i] != null)
            {
                warningLines[i].gameObject.SetActive(false);
            }
        }
    }
}
