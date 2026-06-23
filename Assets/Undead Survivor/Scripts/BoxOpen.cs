using System.Collections; // 코루틴(IEnumerator)을 사용하기 위한 네임 스페이스 추가.
using UnityEngine;

// 랜덤박스 충돌을 감지하고 보상을 생성한 뒤 박스를 풀로 반환한다.

public class BoxOpen : MonoBehaviour
{
    private Animator animator;
    private bool isOpened = false; // 상자가 이미 열렸는지 체크하는 변수

    [Header("설정")]
    [Tooltip("상자가 열린 후 몇 초 뒤에 비활성화할지 설정합니다.")]
    [SerializeField] private float delayBeforeDisable = 1.2f; // 지연 시간 최적화

    void Awake()
    {
        animator = GetComponent<Animator>();    // 오브젝트에 부착된 Animator 컴포넌트를 자동으로 가져오기.
    }

    // [버그 수정] 오브젝트 풀에서 Box가 재활용되어 다시 필드에 활성화(OnEnable)될 때 열림 상태를 리셋.
    void OnEnable()
    {
        ResetBoxState();
    }

    public void ResetBoxState()
    {
        isOpened = false;
        if (animator != null)
        {
            animator.Rebind(); // 애니메이터 상태 및 트랙들을 초기 상태로 즉시 강제 되돌림
        }
    }

    // Box Collider 2D의 Is Trigger가 켜져 있을 때, 다른 Collider가 겹치면 실행되는 유니티 내장 함수
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isOpened)    // 부딪힌 오브젝트의 태그가 "Player"이고, 상자가 아직 열리지 않은 상태라면
        {
            ItemPickup pickup = GetComponent<ItemPickup>();
            if (pickup != null && !pickup.CanCollect())
                return;

            OpenChest();
        }
    }

    void OpenChest()
    {
        isOpened = true; // 다시 열리지 않도록 상태를 true로 변경
        
        if (animator != null)
        {
            animator.SetTrigger("IsOpen");  // 애니메이터에 설정한 IsOpen 트리거를 작동
        }
        StartCoroutine(DisableAfterDelay());    // 지연 후 비활성화를 처리할 코루틴 함수를 시작
    }

    IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeDisable);    // 지정된 딜레이 시간만큼 대기.
        ItemPickup pickup = GetComponent<ItemPickup>(); // 상자 내부 수집물 및 효과 트리거링을 위해 ItemPickup 컴포넌트에 수집 호출 명령.
        if (pickup != null)
        {
            pickup.CollectRewardDirectly();
        }
        gameObject.SetActive(false);    // 대기 후 이 오브젝트를 비활성화합니다. (오브젝트 풀 반환)
    }
}
