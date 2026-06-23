using UnityEngine;

// 캐릭터 선택 UI의 능력치와 해금 상태를 표시하고 선택 입력을 전달하는 스크립트.

public class Character : MonoBehaviour
{
    public static float Speed   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 0 ? 1.1f : 1f) * (1f + GameManager.instance.ShopMoveSpeedRate); }
    }

    public static float WeaponSpeed   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 1 ? 1.1f : 1f) * (1f + GameManager.instance.ShopAttackSpeedRate); }
    }

    public static float WeaponRate   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 1 ? 0.9f : 1f) * (1f - GameManager.instance.ShopAttackSpeedRate); }
    }


    public static float Damage   // 함수가 아닌 속성작성
    {
        get { return (GameManager.instance.playerId == 2 ? 1.2f : 1f) * (1f + GameManager.instance.ShopDamageRate); }
    }

    public static int Count   // 함수가 아닌 속성작성
    {
        get { return GameManager.instance.playerId == 3 ? 1 : 0; }
    }
}
