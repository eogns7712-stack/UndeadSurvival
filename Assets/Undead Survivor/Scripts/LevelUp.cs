using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelUp : MonoBehaviour
{
    RectTransform rect; // UI인 LevelUp 창을 관리하기위한 RectTranform 변수 생성
    Item[] items;   // 아이템의 배열 변수 선언
    bool isSelecting;   // 레벨업 창이 현재 열려있는지 확인하는 변수 선언
    int[] currentChoices = new int[3];

    // [추가] 단축키 패널 안내용 숫자 배지 게임오브젝트 배열
    public GameObject[] numpadBadges; 

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        items = GetComponentsInChildren<Item>(true);    // Item의 하위오브젝트의 컴포넌트 가져오기, 비활성화 된 오브젝트도 있기때문에 인자값 true
    }
    void Update()
    {
        if (!isSelecting)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // [추가] 레벨업 시 마우스가 아닌 키보드 숫자 키패드 / 메인 숫자키를 눌러 즉각 선택 지원
        if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame)
        {
            Select(0);
        }
        else if (keyboard.numpad2Key.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame)
        {
            Select(1);
        }
        else if (keyboard.numpad3Key.wasPressedThisFrame || keyboard.digit3Key.wasPressedThisFrame)
        {
            Select(2);
        }
    }
    public void Show()  // 창을 보이고 숨기는 함수 작성
    {
        Next(); // 창을 보이게 할 때 Next함수 호출
        isSelecting = true;
        rect.localScale = Vector3.one;  // (1,1,1)
        GameManager.instance.Stop();    // UI가 출력될 때 게임을 정지하는 함수 호출
        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 레벨업시 효과음 재생
        AudioManager.instance.EffectBgm(true);  // 레벨업UI가 나타날 때 필터를 켜고, 사라지면 끄도록 함수 호출
        ToggleKeyBadges(true);  // 단축키 비주얼 배지 활성화
    }

    public void Hide()
    {
        isSelecting = false;
        rect.localScale = Vector3.zero;
        GameManager.instance.Resume();  // UI가 사라질 때 게임을 재생하는 함수 호출
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select); // 레벨업 아이템 버튼 클릭시 효과음 재생
        AudioManager.instance.EffectBgm(false);  // 레벨업UI가 나타날 때 필터를 켜고, 사라지면 끄도록 함수 호출
        ToggleKeyBadges(false); // 단축키 비주얼 배지 비활성화
    }

    // 단축키 UI 요소들을 켰다 꺼주는 함수
    void ToggleKeyBadges(bool state)
    {
        if (numpadBadges == null) return;
        foreach (GameObject badge in numpadBadges)
        {
            if (badge != null)
                badge.SetActive(state);
        }
    }

    public void Select(int index)   // 버튼 클릭으로 호출될 아이템 선택 함수 작성
    {
        int realIndex = currentChoices[index];
        if (realIndex < 0 || realIndex >= items.Length)
            return;

        if (!items[realIndex].gameObject.activeSelf && isSelecting)
            return;

        items[realIndex].OnClick();

        if (isSelecting)
            Hide();
    }

    void Next() // 레벨업 선택창에서 아이템 3개를 랜덤으로 보여주는 함수
    {
        // 1. 모든 아이템 비활성화
        foreach (Item item in items)
        {
            item.gameObject.SetActive(false);
        }
        // 2. 그 중에서 중복 없이 랜덤 3개 아이템 인덱스 리스트 추출
        List<int> validIndices = new List<int>();
        for (int i = 0; i < items.Length; i++)
        {
            // 무기 및 아이템을 필터링하여 리스트에 등록 (Type.id 등으로 추가적인 해금 필터링 조건 기입 가능)
            validIndices.Add(i);
        }

        int activeCount = 0;
        while (activeCount < 3 && validIndices.Count > 0)
        {
            int randPos = Random.Range(0, validIndices.Count);
            int selectedItemIdx = validIndices[randPos];
            currentChoices[activeCount] = selectedItemIdx;
            validIndices.RemoveAt(randPos);
            activeCount++;
        }

        // [완료] currentChoices 원본 정렬 추가!
        // items 리스트는 GetComponentsInChildren으로 가져왔으므로 계층구조상 정렬(위에서 아래)되어 있습니다.
        // 무작위로 뽑힌 인덱스들을 오름차순으로 한 번 묶어줌으로써, 화면에 실제로 배치되는 Visual 순서(1, 2, 3번)와
        // 키보드 숫자 1, 2, 3 매핑을 언제나 완벽하게 일치시킵니다.
        System.Array.Sort(currentChoices);

        for (int index = 0; index < activeCount; index++)
        {
            Item ranItem = items[currentChoices[index]];

            // 3. 만렙 도달 아이템의 대체 여부를 설정
            // [초월 추가] 만약 한계 도달 무기의 등장을 제한하는 대신 60% 확률로 초월(한계강화) 카드를 띄우고, 40% 확률로만 소비용 물약(Heal)으로 전환되게 분기 처리하여 게임 플레이 경험을 확장시켰습니다.
            if (ranItem.level >= ranItem.data.damages.Length && Random.value < 0.4f)
            {
                currentChoices[index] = items.Length - 1; // items의 맨 마지막에 위치한 Heal 아이템으로 대체 연동
                ranItem = items[currentChoices[index]];
            }

            ranItem.gameObject.SetActive(true);
        }
    }
}