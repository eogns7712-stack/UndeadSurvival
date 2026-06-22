using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptble Object/ItemData")]   // 커스텀 메뉴를 생성하는 속성

/// 아이템의 이름, 아이콘, 레벨별 능력치와 초월 데이터를 보관하는 스크립트.

public class ItemData : ScriptableObject
{
    public enum ItemType { Melee, Range, Glove, Shoe, Heal, Bomb }    // enum으로 아이템 타입 배열 생성

    [Header("# Main Info")]
    public ItemType itemType;
    public int itemId;
    public string itemName;
    [TextArea]  // 인스펙터에 텍스트를 여러줄 넣어줄 수 있게 TextArea 속성 부여
    public string itemDesc;
    public Sprite itemIcon;


    [Header("# Level Data")]
    public float baseDamage;    // 0레벨 데미지를 저장할 변수
    public int baseCount;   // 0레벨 관통력 or 근접무기 갯수를 저장할 변수
    public float[] damages;
    public int[] counts;
    public float[] speeds; // 레벨별 연사속도

    [Header("# Weapon")]
    public GameObject projectile;
    public Sprite hand; // 스크립트블 오브젝트에서 손 스프라이트를 담을 속성 추가

    // [초월 스프라이트 전용 추가 배열]
    [Header("# Transcendence Hand Evolutions")]
    [Tooltip("초월 차수에 맞춰 손에 장착할 캐릭터 무기 스프라이트 이미지를 순서대로 넣어주세요.")]
    public Sprite[] customEvolutions; 

    // [초월 원거리 탄환 전용 진화 배열]
    [Header("# Transcendence Projectile Evolutions")]
    [Tooltip("원거리 총의 경우, 초월 차수에 맞춰 발사될 총알(탄환) 스프라이트를 순서대로 넣어주세요.")]
    public Sprite[] customProjectileEvolutions;

    // [추가] 초월 단계별 전용 설명글
    [Header("# Transcendence Descriptions")]
    [Tooltip("초월 단계별 전용 설명글을 순서대로 기입해 주세요.")]
    [TextArea]
    public string[] transcendenceDescs;

}
