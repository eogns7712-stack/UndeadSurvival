using UnityEngine;

/// 플레이어 방향에 맞춰 근접무기, 총기 및 수류탄 손 스프라이트를 배치하는 스크립트.

public class Hand : MonoBehaviour
{
    public bool isLeft; // 근접무기를 들고 있는 왼손인지 구분.
    public bool isBombHand;    // 수류탄 전용 손 오브젝트인지 구분.
    public SpriteRenderer spriter;  // 손에 표시되는 무기 스프라이트 렌더러.
    
    SpriteRenderer player;
    // Player의 스프라이트렌더러 변수 선언 및 초기화
    
    Vector3 rightPos = new Vector3(0.35f, -0.15f, 0);
    Vector3 rightPosReverse = new Vector3(-0.15f, -0.15f, 0);
    Vector3 bombPos = new Vector3(-0.15f, -0.4f, 0);    // 수류탄 전용 위치 Vector
    Quaternion leftRot = Quaternion.Euler(0, 0, -35);  // 왼손의 각 형태를 Quaternion으로 저장
    Quaternion leftRotReverse = Quaternion.Euler(0, 0, -135);

    void Awake()
    {
        player = GetComponentsInParent<SpriteRenderer>()[1];    // [0]은 자기자신(hand), [1]이 Player
    }

    void LateUpdate()
    {
        bool isReverse = player.flipX;  // Player의 반전상태를 지역변수에 저장

        if (isLeft) // 근접무기의 경우
        {
            transform.localRotation = isReverse ? leftRotReverse : leftRot; // 왼손회전은 localRotation 사용
            spriter.flipY = isReverse;   // 플레이어 방향에 맞춰 근접무기 스프라이트 뒤집기.
            spriter.sortingOrder = isReverse ? 4 : 6;  // 플레이어가 바라보는 방향에 따라 몸 앞/뒤 레이어 조정.
        }
        else if (isBombHand)   // 수류탄 무기
        {
            transform.localPosition = isReverse ? bombPos : rightPos;  // 좌측을 볼 때는 수류탄 전용 위치 사용.
            transform.localRotation = isReverse ? leftRotReverse : leftRot;    // 수류탄 손도 근접무기 회전값을 따라감.
            spriter.flipY = isReverse;   // 방향에 맞춰 수류탄 스프라이트 반전.
            spriter.sortingOrder = 4;    // 수류탄 손은 항상 근접무기 뒤쪽 레이어와 같은 값으로 고정.
        }
        else    // 원거리 무기
        {
            transform.localPosition = isReverse ? rightPosReverse : rightPos;  // 총은 바라보는 방향에 따라 손 위치를 좌우 이동.
            spriter.flipX = isReverse;   // 총 스프라이트 좌우 반전.
            spriter.sortingOrder = isReverse ? 6 : 4;  // 총이 플레이어 앞/뒤에 자연스럽게 보이도록 레이어 조정.
        }
    }

    // 수류탄 무기를 획득하거나 초기화할 때 Hand Bomb 오브젝트를 켜고 끄는 함수.
    public static void SetBombHandActive(Player owner, bool active)
    {
        if (owner == null)  // Player 참조가 없으면 실행 중단.
            return;

        Hand[] hands = owner.GetComponentsInChildren<Hand>(true);  // 비활성화된 Hand Bomb까지 포함해 Player 하위 Hand 검색.
        foreach (Hand hand in hands)    // Player 하위 Hand 중 수류탄 전용 Hand만 찾아 활성화 상태 변경.
        {
            if (hand != null && hand.isBombHand)
            {
                hand.gameObject.SetActive(active);
            }
        }
    }

}
