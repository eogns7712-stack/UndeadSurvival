using UnityEngine;
using UnityEngine.UI;

/// 메인 화면의 게임 설명 패널을 열고, 인스펙터에 작성한 페이지 내용을 넘겨 보여주는 스크립트.

public class GameGuideUI : MonoBehaviour
{
    // 가이드 한 페이지에 들어갈 제목, 본문, 이미지를 묶어두는 데이터 클래스.
    // 인스펙터의 Pages 배열에 Element를 추가하면 이 구조대로 페이지가 생성된다.
    [System.Serializable]
    public class GuidePage
    {
        public string title;    // 페이지 제목.
        [TextArea(3, 8)] public string body;   // 페이지 본문. 유니티 인스펙터에서 작성.
        public Sprite image;    // 페이지별로 보여줄 설명 이미지. 없으면 이미지 영역을 숨긴다.
    }

    [Header("# UI")]
    public GameObject guidePanel;   // 실제로 켜고 끌 설명창 패널.
    public Text titleText;  // 현재 페이지 제목 표시.
    public Text bodyText;   // 현재 페이지 본문 표시.
    public Image pageImage; // 현재 페이지 이미지 표시.
    public Text pageIndicatorText;  // 1 / 3 같은 페이지 표시.
    public Button prevButton;   // 이전 페이지 버튼.
    public Button nextButton;   // 다음 페이지 버튼.

    [Header("# Guide Pages")]
    public GuidePage[] pages;   // 인스펙터에서 작성할 설명 페이지 목록.

    int currentPage;    // 현재 표시 중인 페이지 인덱스. 배열 번호이므로 실제 표시는 +1 해서 보여준다.

    void Awake()
    {
        Hide(); // 시작할 때 설명창이 열려 있지 않도록 초기화.
    }

    // 설명 버튼에서 호출. 패널을 열고 첫 페이지부터 표시.
    public void OpenGuide()
    {
        currentPage = 0;    // 설명창을 새로 열 때는 항상 첫 페이지부터 시작.
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }
        ShowPage(currentPage);  // 패널을 켠 뒤 현재 페이지의 텍스트, 이미지, 버튼 상태 갱신.
    }

    // 닫기 버튼에서 호출. 설명창을 숨긴다.
    public void CloseGuide()
    {
        Hide();
    }

    // 외부 시스템에서 설명창을 강제로 닫을 때 사용.
    public void Hide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);    // 패널 전체를 비활성화해서 하위 UI도 함께 숨긴다.
        }
    }

    // 다음 페이지 버튼에서 호출.
    public void NextPage()
    {
        ShowPage(currentPage + 1);  // 실제 범위 제한은 ShowPage 내부에서 처리.
    }

    // 이전 페이지 버튼에서 호출.
    public void PrevPage()
    {
        ShowPage(currentPage - 1);  // 실제 범위 제한은 ShowPage 내부에서 처리.
    }

    // 지정한 페이지를 표시하고 버튼 상태를 갱신.
    void ShowPage(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
        {
            ClearPage();    // 인스펙터에 페이지가 없을 때 빈 UI 상태로 안전하게 정리.
            return;
        }

        currentPage = Mathf.Clamp(pageIndex, 0, pages.Length - 1);  // 첫 페이지보다 작거나 마지막 페이지를 넘지 않게 제한.
        GuidePage page = pages[currentPage];    // 제한된 인덱스로 실제 표시할 페이지 데이터 선택.

        if (titleText != null)
        {
            titleText.text = page.title;    // 선택된 페이지 제목 출력.
        }

        if (bodyText != null)
        {
            bodyText.text = page.body;  // 선택된 페이지 본문 출력.
        }

        if (pageImage != null)
        {
            pageImage.sprite = page.image;  // 선택된 페이지 이미지 적용.
            pageImage.gameObject.SetActive(page.image != null);    // 이미지가 없는 페이지에서는 이미지 오브젝트를 숨긴다.
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = string.Format("{0} / {1}", currentPage + 1, pages.Length); // 현재 페이지 / 전체 페이지 표시.
        }

        if (prevButton != null)
        {
            prevButton.interactable = currentPage > 0; // 첫 페이지에서는 이전 버튼 비활성화.
        }

        if (nextButton != null)
        {
            nextButton.interactable = currentPage < pages.Length - 1;   // 마지막 페이지에서는 다음 버튼 비활성화.
        }
    }

    // 페이지가 비어 있을 때 안전하게 UI를 초기화.
    void ClearPage()
    {
        if (titleText != null)
        {
            titleText.text = "";   // 표시할 페이지가 없으므로 제목 비우기.
        }

        if (bodyText != null)
        {
            bodyText.text = "";    // 표시할 페이지가 없으므로 본문 비우기.
        }

        if (pageImage != null)
        {
            pageImage.sprite = null;    // 이전 페이지 이미지가 남지 않도록 제거.
            pageImage.gameObject.SetActive(false); // 이미지 오브젝트 숨기기.
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = "0 / 0";  // 페이지가 없는 상태 표시.
        }

        if (prevButton != null)
        {
            prevButton.interactable = false;   // 이동할 페이지가 없으므로 이전 버튼 비활성화.
        }

        if (nextButton != null)
        {
            nextButton.interactable = false;   // 이동할 페이지가 없으므로 다음 버튼 비활성화.
        }
    }
}
