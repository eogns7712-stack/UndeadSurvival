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

    public void Show()  // 레벨업 창을 보여주고 일시정지하는 함수 작성
    {
        Next();
        isSelecting = true;
        rect.localScale = Vector3.one;  // 크기를 1로 조절하여 화면에 표시
        GameManager.instance.Stop();    // 게임정지 함수 호출
        AudioManager.instance.EffectBgm(true);  // 하이패스 효과 켜기

        // 단축키 비주얼 배지 활성화
        ToggleKeyBadges(true);
    }

    public void Hide()  // 레벨업창을 숨기고 일시정지를 해제하는 함수 작성
    {
        isSelecting = false;
        rect.localScale = Vector3.zero; // 크기를 0으로 만들어 숨김처리
        GameManager.instance.Resume();  // 일시정지 해제 함수 호출
        AudioManager.instance.EffectBgm(false); // 하이패스 효과 끄기

        // 단축키 비주얼 배지 비활성화
        ToggleKeyBadges(false);
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

    // [버그 수정 완료] 중복 인덱스 대체 계산 오류를 완전 제거하여, 항상 깨끗하게 3개 가용 슬롯이 출현하도록 수정!
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

        // 3. 중복 없이 랜덤 3개 인덱스 안전 추출
        List<int> selectedList = new List<int>();
        int targetCount = Mathf.Min(3, validIndices.Count);

        while (selectedList.Count < targetCount && validIndices.Count > 0)
        {
            int randPos = Random.Range(0, validIndices.Count);
            int selectedItemIdx = validIndices[randPos];
            validIndices.RemoveAt(randPos);
            selectedList.Add(selectedItemIdx);
        }

        // 4. 만렙 도달 아이템의 대체 여부를 설정
        bool hasHealBeenAdded = false;
        for (int i = 0; i < selectedList.Count; i++)
        {
            Item ranItem = items[selectedList[i]];

            if (ranItem.level >= ranItem.data.damages.Length)
            {
                // 패시브 기어(장갑, 신발)도 무기처럼 한계 한도 없이 무한히 초월할 수 있도록 분기를 완전히 개방했습니다!
                // 단, 무작위로 40%의 확률로 물약(Heal) 대체를 거쳐서 등장합니다.
                bool shouldReplaceWithHeal = (Random.value < 0.4f);

                if (shouldReplaceWithHeal)
                {
                    // [중요] Heal 카드가 중복하여 추가되지 않은 깨끗한 상태에서만 단 1회 대체 승인!
                    if (!hasHealBeenAdded)
                    {
                        selectedList[i] = items.Length - 1; // items 리스트 맨 마지막 Heal 카드로 안전 우회 변경
                        hasHealBeenAdded = true;
                    }
                    // 이미 Heal 카드가 출력되어 있는 상태라면, 카드 소실(동일 인스턴스 중첩)을 막기 위해 
                    // 해당 만렙 장비의 초월 성장 카드가 화면에 정상 노출되도록 안전을 수호합니다.
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

        // [완료] currentChoices 원본 정렬 추가!
        // 이 정렬을 통해 1, 2, 3 선택 슬롯과 키보드 입력 매칭이 언제나 일치합니다.
        System.Array.Sort(currentChoices, 0, selectedList.Count);

        // 6. UI 카드 게임오브젝트 동적 활성화
        for (int index = 0; index < selectedList.Count; index++)
        {
            if (currentChoices[index] != -1)
            {
                items[currentChoices[index]].gameObject.SetActive(true);
            }
        }
    }
}