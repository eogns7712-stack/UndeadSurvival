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

    // [버그 수정] 카드 개수가 가끔 3개 미만으로 나타나던 정렬 및 중복 대체 연산 오작동 해결
    void Next() // 레벨업 선택창에서 아이템 3개를 랜덤으로 보여주는 함수
    {
        // 1. 모든 아이템 비활성화
        foreach (Item item in items)
        {
            item.gameObject.SetActive(false);
        }

        // 2. 가용 후보 인덱스 리스트 추출 (Heal 카드는 중복 보정용이므로 후보군에서 일단 배제)
        List<int> validIndices = new List<int>();
        for (int i = 0; i < items.Length - 1; i++)
        {
            validIndices.Add(i);
        }

        // 3. 중복 없이 랜덤 3개 인덱스 추출
        List<int> selectedList = new List<int>();
        int targetCount = Mathf.Min(3, validIndices.Count);

        while (selectedList.Count < targetCount && validIndices.Count > 0)
        {
            int randPos = Random.Range(0, validIndices.Count);
            int selectedItemIdx = validIndices[randPos];
            validIndices.RemoveAt(randPos);
            selectedList.Add(selectedItemIdx);
        }

        // 4. 만렙 도달 아이템의 중복 없는 정밀 대체 처리
        bool hasHealBeenAdded = false;

        for (int i = 0; i < selectedList.Count; i++)
        {
            Item ranItem = items[selectedList[i]];

            // 만렙 도달 장비/아이템의 럭키 소비 힐템 대체 분기
            if (ranItem.level >= ranItem.data.damages.Length)
            {
                // 패시브 기어(초월 성장이 미설계된 장비)이거나, 40% 확률로 힐팩 대체가 활성화되었을 때
                bool isPassiveGear = (ranItem.data.itemType == ItemData.ItemType.Glove || ranItem.data.itemType == ItemData.ItemType.Shoe);
                bool shouldReplaceWithHeal = isPassiveGear || (Random.value < 0.4f);

                if (shouldReplaceWithHeal)
                {
                    // [중요] Heal 카드가 중복하여 추가되지 않은 깨끗한 상태에서만 단 1회 대체 승인!
                    if (!hasHealBeenAdded)
                    {
                        selectedList[i] = items.Length - 1; // items 리스트 맨 마지막 Heal 카드로 변경
                        hasHealBeenAdded = true;
                    }
                    // 이미 Heal 카드가 출력되어 있는 상태라면, 카드 소실(동일 인스턴스 중첩)을 막기 위해 
                    // 해당 만렙 장비의 무한 초월 성장 카드 형태를 그대로 노출하여 3장 슬롯을 수호합니다.
                }
            }
        }

        // 5. currentChoices 인덱스 안전 바인딩
        for (int i = 0; i < selectedList.Count; i++)
        {
            currentChoices[i] = selectedList[i];
        }

        // 만약 예외적으로 카드가 부족해 가용 공간이 비었을 경우의 예외 방어막
        for (int i = selectedList.Count; i < 3; i++)
        {
            currentChoices[i] = -1;
        }

        // 1, 2, 3 정렬 매치 일관성을 위해 오름차순으로 완벽 정렬
        System.Array.Sort(currentChoices, 0, selectedList.Count);

        // 6. UI 카드 게임오브젝트 동적 활성화
        for (int index = 0; index < selectedList.Count; index++)
        {
            int itemIndex = currentChoices[index];
            if (itemIndex >= 0 && itemIndex < items.Length)
            {
                items[itemIndex].gameObject.SetActive(true);
            }
        }
    }
}