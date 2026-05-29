using System.Collections; // 코루틴(IEnumerator)을 사용하기 위해 필요합니다.
using UnityEngine;

public class BoxOpen : MonoBehaviour
{
    private Animator animator;
    private bool isOpened = false; // 상자가 이미 열렸는지 체크하는 변수

    [Header("설정")]
    [Tooltip("상자가 열린 후 몇 초 뒤에 비활성화할지 설정합니다.")]
    [SerializeField] private float delayBeforeDisable = 1.2f; // 지연 시간 최적화

    void Awake()
    {
        // 오브젝트에 부착된 Animator 컴포넌트를 자동으로 가져옵니다.
        animator = GetComponent<Animator>();
    }

    // [버그 수정 완료] 오브젝트 풀에서 상자가 재활용되어 다시 필드에 활성화(OnEnable)될 때 열림 상태를 리셋해줍니다.
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
        // 부딪힌 오브젝트의 태그가 "Player"이고, 상자가 아직 열리지 않은 상태라면
        if (collision.CompareTag("Player") && !isOpened)
        {
            OpenChest();
        }
    }

    void OpenChest()
    {
        isOpened = true; // 다시 열리지 않도록 상태를 true로 변경
        
        if (animator != null)
        {
            // 애니메이터에 설정한 IsOpen 트리거를 작동
            animator.SetTrigger("IsOpen");
        }

        // 지연 후 비활성화를 처리할 코루틴 함수를 시작
        StartCoroutine(DisableAfterDelay());
    }

    IEnumerator DisableAfterDelay()
    {
        // 지정된 딜레이 시간만큼 대기합니다.
        yield return new WaitForSeconds(delayBeforeDisable);

        // 상자 내부 수집물 및 효과 트리거링을 위해 ItemPickup 컴포넌트에 수집 호출 명령
        ItemPickup pickup = GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.CollectRewardDirectly();
        }

        // 대기 후 이 오브젝트를 비활성화합니다. (오브젝트 풀 반환)
        gameObject.SetActive(false);
    }
}