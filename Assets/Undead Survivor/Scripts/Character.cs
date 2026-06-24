using UnityEngine;

// 캐릭터 선택 UI의 능력치와 해금 상태를 표시하고 선택 입력을 전달하는 스크립트.

public class Character : MonoBehaviour
{
    public static float Speed   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 0 ? 1.1f : 1f) * (1f + GameManager.instance.ShopMoveSpeedRate); } // 0번 캐릭터 보너스와 상점 이동속도 보너스 적용.
    }

    public static float WeaponSpeed   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 1 ? 1.1f : 1f) * (1f + GameManager.instance.ShopAttackSpeedRate); } // 회전형 무기 속도에 캐릭터/상점 공속 보너스 적용.
    }

    public static float WeaponRate   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 1 ? 0.9f : 1f) * (1f - GameManager.instance.ShopAttackSpeedRate); } // 발사 쿨타임에 곱해지는 값. 낮을수록 공격속도가 빨라짐.
    }


    public static float Damage   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 2 ? 1.2f : 1f) * (1f + GameManager.instance.ShopDamageRate); } // 2번 캐릭터 보너스와 상점 공격력 보너스 적용.
    }

    public static int Count   // 함수가 아닌 속성작성
    {
        get { return GameManager.instance.playerId == 3 ? 1 : 0; } // 3번 캐릭터는 무기 개수/관통 보너스 1 추가.
    }
}
