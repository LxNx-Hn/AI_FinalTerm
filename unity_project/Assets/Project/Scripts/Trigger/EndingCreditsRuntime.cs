using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EndingCreditsRuntime : MonoBehaviour
{
    private const string CreditsSceneName = "EndingCredits";
    private const string FirstSceneName   = "Entry";
    private const float  CreditLineHeight  = 56f;

    // ── 크레딧 엔트리 타입 ────────────────────────────────────────────────────
    private enum EntryType { Text, FlickerImage }

    private class Entry
    {
        public EntryType type        = EntryType.Text;
        public string    text        = "";
        public string    horrorPath  = "";   // Resources/Credits/...
        public string    normalPath  = "";
        public float     imageHeight = 340f;
    }

    // 단축 생성자
    private static Entry T(string s)               => new Entry { text = s };
    private static Entry B()                        => new Entry { text = "" };
    private static Entry Img(string hr, string nm) => new Entry
        { type = EntryType.FlickerImage, horrorPath = hr, normalPath = nm, imageHeight = 340f };

    // ── 크레딧 데이터 ─────────────────────────────────────────────────────────
    private static readonly Entry[] Entries =
    {
        T("CODE BLUE"),
        B(), B(),
        T("A Game By"),
        T("Team CODE BLUE"),
        B(), B(),
        T("총괄 디렉터"),
        T("KiKi"),
        B(), B(),
        T("개발 기간"),
        T("2026-03-30 ~ 2026-05-26"),
        B(), B(), B(), B(),

        // ── 공통 · 전체 제작 ──────────────────────────────────────────────────
        T("공통 · 전체 제작"),
        B(), B(),
        T("총괄 기획"),
        T("KiKi"),
        B(), B(),
        T("총괄 프로그래밍"),
        T("breezemin1009"),
        B(), B(),
        T("게임 시스템 설계"),
        T("KiKi"),
        B(), B(),
        T("밸런스 · 난이도 조정"),
        T("lh7721004"),
        B(), B(),
        T("시나리오 · 스토리"),
        T("공동 제작"),
        B(), B(),
        T("전체 아트 디렉션"),
        T("breezemin1009"),
        B(), B(),
        T("전체 UI / UX"),
        T("lh7721004"),
        B(), B(),
        T("전체 사운드 디렉션"),
        T("KiKi"),
        B(), B(), B(), B(),

        // ── 공통 시스템 ───────────────────────────────────────────────────────
        T("공통 시스템"),
        B(), B(),
        T("그리드 시스템 제작"),
        T("KiKi"),
        B(), B(),
        T("이동 알고리즘 제작"),
        T("KiKi"),
        B(), B(),
        T("플레이어 컨트롤러 제작"),
        T("breezemin1009"),
        B(), B(),
        T("전투 시스템 제작"),
        T("breezemin1009"),
        B(), B(),
        T("무적 시스템 제작"),
        T("breezemin1009"),
        B(), B(),
        T("버프 시스템 제작"),
        T("lh7721004"),
        B(), B(),
        T("적 AI · 경로탐색 제작"),
        T("KiKi"),
        B(), B(),
        T("아이템 · 드롭 시스템 제작"),
        T("lh7721004"),
        B(), B(),
        T("씬 전환 · 로딩 시스템 제작"),
        T("KiKi"),
        B(), B(),
        T("카메라 추적 시스템 제작"),
        T("breezemin1009"),
        B(), B(),
        T("대화 시스템 제작"),
        T("KiKi"),
        B(), B(),
        T("사망 화면 시스템 제작"),
        T("KiKi"),
        B(), B(),
        T("크레딧 연출 시스템 제작"),
        T("KiKi"),
        B(), B(), B(), B(),

        // ── 공통 캐릭터 · 프리팹 ──────────────────────────────────────────────
        T("공통 캐릭터 · 프리팹"),
        B(), B(),
        T("캐릭터 디자인"),
        T("KiKi"),
        B(), B(),
        T("플레이어 스프라이트 제작"),
        T("breezemin1009"),
        B(), B(),
        T("플레이어 애니메이션 기준 제작"),
        T("breezemin1009"),
        B(), B(),
        T("적 스프라이트 제작"),
        T("lh7721004"),
        B(), B(),
        T("플레이어 프리팹 구성"),
        T("breezemin1009"),
        B(), B(),
        T("플레이어 애니메이션 연결"),
        T("breezemin1009"),
        B(), B(),
        T("적 프리팹 구성"),
        T("lh7721004"),
        B(), B(),
        T("아이템 프리팹 구성"),
        T("lh7721004"),
        B(), B(),
        T("보스 프리팹 구성"),
        T("KiKi"),
        B(), B(), B(), B(),

        // ── 공통 UI / UX ──────────────────────────────────────────────────────
        T("공통 UI / UX"),
        B(), B(),
        T("시작 화면 제작"),
        T("breezemin1009"),
        B(), B(),
        T("타이틀 메뉴 레이아웃 제작"),
        T("lh7721004"),
        B(), B(),
        T("HUD 레이아웃 제작"),
        T("lh7721004"),
        B(), B(),
        T("보스 체력바 UI 제작"),
        T("KiKi"),
        B(), B(),
        T("키카드 획득 UI 제작"),
        T("lh7721004"),
        B(), B(),
        T("버프 아이콘 UI 제작"),
        T("lh7721004"),
        B(), B(),
        T("대화창 UI 제작"),
        T("KiKi"),
        B(), B(),
        T("사망 화면 UI 제작"),
        T("KiKi"),
        B(), B(),
        T("다시하기 · 타이틀 메뉴 제작"),
        T("lh7721004"),
        B(), B(),
        T("화면 전환 연출 제작"),
        T("lh7721004"),
        B(), B(),
        T("피격 플래시 연출 제작"),
        T("breezemin1009"),
        B(), B(),
        T("스킵 확인창 제작"),
        T("breezemin1009"),
        B(), B(),
        T("엔딩 메뉴 제작"),
        T("lh7721004"),
        B(), B(),
        T("게임 경험 조정"),
        T("lh7721004"),
        B(), B(),
        T("카메라 조작감 조정"),
        T("breezemin1009"),
        B(), B(), B(), B(),

        // ── 공통 사운드 ───────────────────────────────────────────────────────
        T("공통 사운드"),
        B(), B(),
        T("오프닝 BGM 제작"),
        T("KiKi"),
        B(), B(),
        T("타격음 시스템 제작"),
        T("breezemin1009"),
        B(), B(),
        T("발소리 시스템 제작"),
        T("breezemin1009"),
        B(), B(),
        T("보스 공격 SFX 제작"),
        T("KiKi"),
        B(), B(),
        T("보스 착지 SFX 제작"),
        T("KiKi"),
        B(), B(),
        T("보스 경고음 제작"),
        T("KiKi"),
        B(), B(),
        T("보스 사망 SFX 제작"),
        T("KiKi"),
        B(), B(), B(), B(),

        // ── STAGE 1 ───────────────────────────────────────────────────────────
        T("STAGE 1 — 병원 주차장"),
        B(),
        Img("Credits/S1_HR", "Credits/S1_N"),
        B(),
        T("By breezemin1009"),
        B(), B(),
        T("스테이지 기획"),
        B(),
        T("레벨 디자인"),
        B(),
        T("주차장 동선 구성"),
        B(),
        T("적 배치 · 웨이브 설계"),
        B(),
        T("타일 · 배경 아트 제작"),
        B(),
        T("소품 · 데칼 제작"),
        B(),
        T("시작 씬 이미지 제작"),
        B(),
        T("사망 씬 이미지 제작"),
        B(),
        T("발소리 SFX 제작"),
        B(), B(), B(), B(),

        // ── STAGE 2 ───────────────────────────────────────────────────────────
        T("STAGE 2 — 병동"),
        B(),
        Img("Credits/S2_HR", "Credits/S2_N"),
        B(),
        T("By lh7721004"),
        B(), B(),
        T("스테이지 기획"),
        B(),
        T("레벨 디자인"),
        B(),
        T("병동 동선 구성"),
        B(),
        T("아이템 배치"),
        B(),
        T("키카드 획득 연출"),
        B(),
        T("게임 경험 조정"),
        B(),
        T("카메라 조작감 조정"),
        B(),
        T("사망 화면 — Stage 2 제작"),
        B(), B(),
        T("병원 소품 · 장식 제작"),
        B(),
        T("보조 스프라이트 제작"),
        B(),
        T("사망 씬 이미지 제작"),
        B(), B(),
        T("병동 타일셋"),
        T("Hospital Tiles"),
        T("Darby Machin  (PixelJustice)"),
        B(), B(),
        T("캐릭터 · 오브젝트"),
        T("Horror City – Frankenstein"),
        T("Darby Machin  (PixelJustice)"),
        B(), B(), B(), B(),

        // ── STAGE 3 ───────────────────────────────────────────────────────────
        T("STAGE 3 — 엘리베이터 보스"),
        B(),
        Img("Credits/BOSS_HR", "Credits/BOSS_N"),
        B(),
        T("By KiKi"),
        B(), B(),
        T("스테이지 기획"),
        B(),
        T("레벨 디자인"),
        B(),
        T("보스 설계"),
        B(),
        T("보스 패턴 설계"),
        B(),
        T("보스 패턴 시스템 제작"),
        B(),
        T("보스 밸런스 조정"),
        B(),
        T("보스 인트로 연출 제작"),
        B(),
        T("보스 클리어 연출 제작"),
        B(), B(),
        T("보스 전투 BGM 제작"),
        B(),
        T("보스 공격 SFX 제작"),
        B(),
        T("보스 포효 · 착지음 제작"),
        B(),
        T("보스 경고 SFX 제작"),
        B(),
        T("보스 사망 SFX 제작"),
        B(),
        T("사망 씬 이미지 제작"),
        B(), B(),
        T("보스 스프라이트"),
        T("Banshee Spritesheet — ITCH.io"),
        T("베이스라인 편집 및 변환"),
        B(), B(),

        T("피격 이펙트 VFX"),
        T("VFXPACK IMPACT"),
        T("WALLCOEUR  (Free Version)"),
        B(), B(),
        T("빔 · 에너지 VFX"),
        T("Free Game VFX"),
        T("Eric Wang"),
        B(), B(),
        T("전투 타격음 팩"),
        T("Blades & Bludgeonings Free"),
        T("IdiaSoftware"),
        B(), B(),
        T("HP 시스템 UI"),
        T("HeartSystem"),
        T("DreamNoms"),
        B(), B(),
        T("보스 보이스 — 공포 음성 1"),
        T("alesiadavina"),
        T("freesound.org"),
        B(), B(),
        T("보스 보이스 — 공포 음성 2"),
        T("dragon-studio"),
        T("freesound.org"),
        B(), B(),
        T("보스 보이스 — 공포 음성 3"),
        T("neo_panda_25"),
        T("freesound.org"),
        B(), B(),
        T("보스 보이스 — 환경음"),
        T("imagne_impossible"),
        T("freesound.org"),
        B(), B(), B(), B(),

        // ── ENDING ────────────────────────────────────────────────────────────
        T("ENDING — 옥상"),
        B(),
        T("By KiKi"),
        B(), B(),
        T("엔딩 연출 설계"),
        B(),
        T("시나리오 최종 편집"),
        B(),
        T("옥상 배경 아트 제작"),
        B(),
        T("워크 애니메이션 제작"),
        B(),
        T("옥상 회상 시스템 제작"),
        B(),
        T("이름 변화 연출 제작"),
        B(),
        T("플레이어 페이드아웃 연출 제작"),
        B(),
        T("크레딧 배경 아트 제작"),
        B(), B(), B(), B(),

        // ── MUSIC ─────────────────────────────────────────────────────────────
        T("MUSIC"),
        B(), B(),
        T("Original Songs"),
        T("「Old Doll」,  「メモリー (Memory)」"),
        T("Eve"),
        B(), B(),
        T("편곡 · 재연주"),
        T("KiKi"),
        B(), B(),
        T("보스 인트로 BGM"),
        T("「Old Doll」  —  Eve"),
        T("편곡 · 재연주  KiKi"),
        B(), B(),
        T("옥상 회상 BGM"),
        T("「メモリー (Memory)」  —  Eve"),
        T("편곡 · 재연주  KiKi"),
        B(), B(), B(), B(),

        // ── EXTERNAL SOUND CREDITS ────────────────────────────────────────────
        T("EXTERNAL SOUND CREDITS"),
        B(), B(),
        T("alesiadavina"),
        T("freesound.org"),
        B(), B(),
        T("dragon-studio"),
        T("freesound.org"),
        B(), B(),
        T("neo_panda_25"),
        T("freesound.org"),
        B(), B(),
        T("imagne_impossible"),
        T("freesound.org"),
        B(), B(),
        T("Blades & Bludgeonings Free"),
        T("IdiaSoftware"),
        B(), B(), B(), B(),

        // ── EXTERNAL GRAPHIC CREDITS ──────────────────────────────────────────
        T("EXTERNAL GRAPHIC CREDITS"),
        B(), B(),
        T("Hospital Tiles"),
        T("Darby Machin  (PixelJustice)"),
        B(), B(),
        T("Horror City – Frankenstein"),
        T("Darby Machin  (PixelJustice)"),
        B(), B(),
        T("Banshee Spritesheet"),
        T("ITCH.io"),
        B(), B(),
        T("Cinematic Explosions FREE"),
        T("Mirza Beig"),
        B(), B(),
        T("Free Game VFX"),
        T("Eric Wang"),
        B(), B(),
        T("VFXPACK IMPACT"),
        T("WALLCOEUR"),
        B(), B(),
        T("HeartSystem"),
        T("DreamNoms"),
        B(), B(), B(), B(),

        // ── TECHNOLOGY ────────────────────────────────────────────────────────
        T("TECHNOLOGY"),
        B(), B(),
        T("Unity 2022"),
        B(), B(),
        T("Universal Render Pipeline 17.3.0"),
        B(), B(),
        T("TextMesh Pro"),
        T("Unity Technologies"),
        B(), B(),
        T("Liberation Sans Font"),
        T("SIL Open Font License"),
        B(), B(),
        T("Aseprite"),
        B(), B(),
        T("Cinemachine 2.10.5"),
        B(), B(),
        T("Unity Input System"),
        B(), B(),
        T("Unity 2D Tilemap"),
        B(), B(), B(), B(),

        // ── SPECIAL THANKS ────────────────────────────────────────────────────
        T("SPECIAL THANKS"),
        B(), B(),
        T("이 게임을 끝까지 플레이해 주신 모든 분들에게"),
        B(), B(),
        T("감사합니다"),
        B(), B(), B(), B(),
        T("채하민은"),
        T("도망치지 않았다"),
        B(), B(), B(), B(),
        T("CODE BLUE"),
        T("2026"),
        B(), B(), B(), B(),
        T("Thanks for Playing"),
    };

    // ── Inspector ─────────────────────────────────────────────────────────────
    [SerializeField] private Sprite creditBgSprite;
    [SerializeField] private Sprite creditTitleSprite;

    [Header("Route Credit Backgrounds")]
    [SerializeField] private Sprite normalCreditBgSprite;
    [SerializeField] private Sprite pacifistCreditBgSprite;
    [SerializeField] private Sprite massacreCreditBgSprite;

    [Header("Testing Route Override")]
    [SerializeField] private bool forcePacifistCreditsForTesting;
    [SerializeField] private bool forceMassacreCreditsForTesting;

    [Tooltip("크레딧 스크롤 총 시간(초). 길수록 천천히 올라갑니다.")]
    [SerializeField] private float  creditsDuration = 150f;

    // ── 부트스트랩 ─────────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveScene()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != CreditsSceneName) return;
        EnsureInScene();
    }

    private static EndingCreditsRuntime EnsureInScene()
    {
        EndingCreditsRuntime ex = FindFirstObjectByType<EndingCreditsRuntime>();
        if (ex != null) return ex;
        return new GameObject("EndingCreditsRuntime").AddComponent<EndingCreditsRuntime>();
    }

    // ── 런타임 상태 ───────────────────────────────────────────────────────────
    private bool         skipAllowed;
    private bool         skipConfirmShowing;
    private Canvas       mainCanvas;
    private Image        creditBgImage;
    private Image        creditTitleImage;
    private Text         titleText;
    private RectTransform scrollContainer;
    private GameObject   skipConfirmPanel;
    private GameObject   endMenuPanel;
    private float        _totalH;
    private Sprite       autoPacifistCreditBgSprite;
    private Sprite       autoMassacreCreditBgSprite;

    // ── 라이프사이클 ──────────────────────────────────────────────────────────
    private void Start()   => StartCoroutine(CreditsSequence());

    private void Update()
    {
        if (!skipAllowed || skipConfirmShowing) return;
        if (Input.GetMouseButtonDown(0)     ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return)||
            Input.GetKeyDown(KeyCode.Escape))
            ShowSkipConfirm();
    }

    // ── 크레딧 시퀀스 ─────────────────────────────────────────────────────────
    private IEnumerator CreditsSequence()
    {
        Time.timeScale = 1f;
        BuildUi();

        if (BossBattleMusicRuntime.Instance != null)
            BossBattleMusicRuntime.Instance.SetMainVolume(0.18f);

        StartCoroutine(FadeFromBlack(0.8f));
        yield return FadeGraphic(creditBgImage, 0f, 1f, 1f);

        yield return new WaitForSecondsRealtime(2f);
        yield return PlayTitleImageSequence();

        scrollContainer.gameObject.SetActive(true);
        yield return ScrollCredits(creditsDuration);

        skipAllowed = false;
        yield return FadeToBlack(1.5f);
        ShowEndMenu();
    }

    // ── UI 구성 ───────────────────────────────────────────────────────────────
    private void BuildUi()
    {
        mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            GameObject cgo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            mainCanvas = cgo.GetComponent<Canvas>();
        }

        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            mainCanvas.gameObject.AddComponent<GraphicRaycaster>();

        Font font = GetKoreanFont();
        Transform t = mainCanvas.transform;
        EndingRoute creditsRoute = GetCreditsRoute();
        Entry[] routeEntries = BuildEntriesForRoute(creditsRoute);

        for (int i = 0; i < t.childCount; i++)
        {
            Transform child = t.GetChild(i);
            if (child != null) child.gameObject.SetActive(false);
        }

        // 배경
        GameObject bgGo = new GameObject("CreditBg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(t, false);
        FullStretch(bgGo.GetComponent<RectTransform>());
        creditBgImage = bgGo.GetComponent<Image>();
        Sprite routeBackground = GetRouteCreditBackground();
        creditBgImage.sprite       = routeBackground;
        creditBgImage.color        = routeBackground != null
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0f, 0f, 0f, 0f);
        creditBgImage.raycastTarget = false;

        // 타이틀 이미지
        GameObject titleImageGo = new GameObject("CreditTitleImage", typeof(RectTransform), typeof(Image));
        titleImageGo.transform.SetParent(t, false);
        RectTransform titleImageRect = titleImageGo.GetComponent<RectTransform>();
        titleImageRect.anchorMin = new Vector2(0.14f, 0.50f);
        titleImageRect.anchorMax = new Vector2(0.86f, 0.98f);
        titleImageRect.offsetMin = Vector2.zero;
        titleImageRect.offsetMax = Vector2.zero;
        creditTitleImage = titleImageGo.GetComponent<Image>();
        creditTitleImage.sprite        = creditTitleSprite;
        creditTitleImage.preserveAspect= true;
        creditTitleImage.color         = new Color(1f, 1f, 1f, 0f);
        creditTitleImage.raycastTarget = false;
        titleImageGo.SetActive(creditTitleSprite != null);

        // 타이틀 텍스트 fallback
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(t, false);
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.35f);
        titleRect.anchorMax = new Vector2(0.9f, 0.65f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText           = titleGo.GetComponent<Text>();
        titleText.text      = "CODE - BLUE";
        titleText.font      = font;
        titleText.fontSize  = 72;
        titleText.color     = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.raycastTarget = false;
        titleGo.SetActive(false);

        // ── 스크롤 컨테이너 ──────────────────────────────────────────────────
        GameObject scrollGo = new GameObject("ScrollContainer", typeof(RectTransform));
        scrollGo.transform.SetParent(t, false);
        scrollContainer           = scrollGo.GetComponent<RectTransform>();
        scrollContainer.anchorMin = new Vector2(0.1f, 0f);
        scrollContainer.anchorMax = new Vector2(0.9f, 0f);
        scrollContainer.pivot     = new Vector2(0.5f, 0f);

        // 총 높이 계산
        float totalH = 0f;
        foreach (Entry e in routeEntries)
            totalH += e.type == EntryType.FlickerImage ? e.imageHeight : CreditLineHeight;
        _totalH = totalH;
        scrollContainer.sizeDelta = new Vector2(0f, totalH);

        // 엔트리 빌드
        float cumY = 0f;
        for (int i = 0; i < routeEntries.Length; i++)
        {
            Entry  entry   = routeEntries[i];
            float  entryH  = entry.type == EntryType.FlickerImage ? entry.imageHeight : CreditLineHeight;

            GameObject go = new GameObject("E" + i, typeof(RectTransform));
            go.transform.SetParent(scrollContainer, false);
            RectTransform lr = go.GetComponent<RectTransform>();
            lr.anchorMin        = new Vector2(0f, 1f);
            lr.anchorMax        = new Vector2(1f, 1f);
            lr.pivot            = new Vector2(0.5f, 1f);
            lr.anchoredPosition = new Vector2(0f, -cumY);
            lr.sizeDelta        = new Vector2(0f, entryH);

            if (entry.type == EntryType.FlickerImage)
            {
                Image img = go.AddComponent<Image>();
                Texture2D tex = Resources.Load<Texture2D>(entry.horrorPath);
                if (tex != null)
                    img.sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.preserveAspect  = true;
                img.raycastTarget   = false;

                // 이 이미지가 화면 하단에 도달하는 scroll 임계값
                // 이미지 상단이 화면 중앙(canvas y=0)에 도달할 때 트리거
                float threshold = cumY - totalH + 540f;
                StartCoroutine(FlickerWhenVisible(img, entry, threshold));
            }
            else
            {
                go.AddComponent<Text>();   // Text가 없으면 AddComponent
                Text lineText    = go.GetComponent<Text>();
                lineText.text    = entry.text;
                lineText.font    = font;
                lineText.color   = new Color(1f, 1f, 1f, 0.92f);
                lineText.alignment    = TextAnchor.MiddleCenter;
                lineText.raycastTarget = false;

                Shadow shadow = go.AddComponent<Shadow>();
                shadow.effectColor    = new Color(0f, 0f, 0f, 0.55f);
                shadow.effectDistance = new Vector2(1f, -1f);

                // 헤더 판별: 빈 줄 또는 이미지 엔트리 뒤에 오는 비어있지 않은 텍스트
                bool prevIsBlankOrImage = i == 0
                    || (routeEntries[i - 1].type == EntryType.Text  && routeEntries[i - 1].text == "")
                    || routeEntries[i - 1].type == EntryType.FlickerImage;
                bool isHeader = entry.text.Length > 0
                    && !entry.text.StartsWith(" ")
                    && prevIsBlankOrImage;

                lineText.fontSize  = isHeader ? 38 : 30;
                lineText.fontStyle = FontStyle.Bold;
                lineText.color     = isHeader
                    ? new Color(0.75f, 0.88f, 1f, 1f)   // 연 하늘색 (헤더)
                    : new Color(1f,    1f,    1f, 0.92f);
            }

            cumY += entryH;
        }

        // 스크롤 시작 위치 (컨테이너 상단이 화면 하단에서 출발)
        scrollContainer.anchoredPosition = new Vector2(0f, -totalH);
        scrollContainer.gameObject.SetActive(false);

        // 페이드 패널
        GameObject fadeGo = new GameObject("FadePanel", typeof(RectTransform), typeof(Image));
        fadeGo.transform.SetParent(t, false);
        FullStretch(fadeGo.GetComponent<RectTransform>());
        Image fadeImg = fadeGo.GetComponent<Image>();
        fadeImg.color         = new Color(0f, 0f, 0f, 1f);
        fadeImg.raycastTarget = false;
        fadeGo.transform.SetAsLastSibling();

        BuildSkipConfirmPanel(font);
        BuildEndMenuPanel(font);
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private Sprite GetRouteCreditBackground()
    {
        EndingRoute route = GetCreditsRoute();
        Sprite normal = normalCreditBgSprite != null ? normalCreditBgSprite : creditBgSprite;

        switch (route)
        {
            case EndingRoute.Pacifist:
                return pacifistCreditBgSprite != null
                    ? pacifistCreditBgSprite
                    : (GetAutoPacifistCreditBackground() ?? normal);
            case EndingRoute.Massacre:
                return massacreCreditBgSprite != null
                    ? massacreCreditBgSprite
                    : (GetAutoMassacreCreditBackground() ?? normal);
            default:
                return normal;
        }
    }

    private EndingRoute GetCreditsRoute()
    {
        if (forceMassacreCreditsForTesting)
            return EndingRoute.Massacre;

        if (forcePacifistCreditsForTesting)
            return EndingRoute.Pacifist;

        return RunRouteTracker.GetEndingRoute();
    }

    private Sprite GetAutoPacifistCreditBackground()
    {
        if (autoPacifistCreditBgSprite == null)
            autoPacifistCreditBgSprite = LoadCutsceneSprite("Pacifist.png");

        return autoPacifistCreditBgSprite;
    }

    private Sprite GetAutoMassacreCreditBackground()
    {
        if (autoMassacreCreditBgSprite == null)
            autoMassacreCreditBgSprite = LoadCutsceneSprite("KILLER.png");

        return autoMassacreCreditBgSprite;
    }

    private static Sprite LoadCutsceneSprite(string fileName)
    {
        string assetPath = "Assets/Project/CUTSCENE/" + fileName;

#if UNITY_EDITOR
        Sprite editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (editorSprite != null)
            return editorSprite;
#endif

        string fullPath = Path.Combine(Application.streamingAssetsPath, "CUTSCENE", fileName);
        if (!File.Exists(fullPath))
            return null;

        byte[] data = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(data))
            return null;

        texture.name = Path.GetFileNameWithoutExtension(fileName);
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private static Entry[] BuildEntriesForRoute(EndingRoute route)
    {
        List<Entry> entries = new List<Entry>(Entries);

        // ── 플레이 기록 삽입: "공통 · 전체 제작" 바로 앞 ───────────────────────
        int recordIndex = FindInsertIndexBefore(entries, "공통 · 전체 제작");
        if (recordIndex >= 0)
            entries.InsertRange(recordIndex, BuildPlayRecordEntries());

        // ── 엔딩 문구 + 보디카운트 삽입 ──────────────────────────────────────
        int phraseIndex = FindEndingPhraseIndex(entries);
        if (phraseIndex < 0 || phraseIndex + 1 >= entries.Count)
            return entries.ToArray();

        entries.InsertRange(phraseIndex, BuildBodyCountRevealEntries(route));
        phraseIndex += 4;

        switch (route)
        {
            case EndingRoute.Pacifist:
                entries[phraseIndex]     = T("채하민은");
                entries[phraseIndex + 1] = T("다시 안으로 들어갔다");
                break;
            case EndingRoute.Massacre:
                entries[phraseIndex]     = T("아직 남아있는");
                entries[phraseIndex + 1] = T("괴물이 있을 거야");
                break;
            default:
                entries[phraseIndex]     = T("채하민은");
                entries[phraseIndex + 1] = T("도망치지 않았다");
                break;
        }

        return entries.ToArray();
    }

    /// <summary>entries 안에서 anchorText를 가진 첫 번째 텍스트 엔트리 인덱스를 반환한다.</summary>
    private static int FindInsertIndexBefore(IList<Entry> entries, string anchorText)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].type == EntryType.Text && entries[i].text == anchorText)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// RunRouteTracker의 스테이지별 사망 횟수를 읽어 크레딧용 Entry 배열을 만든다.
    /// 타이틀(Entry씬)로 돌아갈 때만 초기화되므로 플레이 전체 사망 합산값이 기록된다.
    /// </summary>
    private static Entry[] BuildPlayRecordEntries()
    {
        int s1    = RunRouteTracker.Stage1Retries;
        int s2    = RunRouteTracker.Stage2Retries;
        int boss  = RunRouteTracker.BossRetries;
        int total = s1 + s2 + boss;

        return new[]
        {
            T("이번 플레이 기록"),
            B(),
            T($"Stage 1   {s1} 사망"),
            T($"Stage 2   {s2} 사망"),
            T($"Boss      {boss} 사망"),
            T($"합계   {total} 사망"),
            B(), B(), B(), B(),
        };
    }

    private static Entry[] BuildBodyCountRevealEntries(EndingRoute route)
    {
        switch (route)
        {
            case EndingRoute.Pacifist:
                return new[] { T("Body Count : 0"), B(), B(), B() };
            default:
                int kills = RunRouteTracker.Stage1Kills + RunRouteTracker.Stage2Kills;
                return new[] { T("Body Count : " + kills), B(), B(), B() };
        }
    }

    private static int FindEndingPhraseIndex(IList<Entry> entries)
    {
        for (int i = 0; i < entries.Count - 1; i++)
        {
            if (entries[i].type == EntryType.Text
                && entries[i + 1].type == EntryType.Text
                && entries[i].text == "채하민은"
                && entries[i + 1].text == "도망치지 않았다")
            {
                return i;
            }
        }

        return -1;
    }

    // ── 이미지 깜빡임 코루틴 ──────────────────────────────────────────────────
    private IEnumerator FlickerWhenVisible(Image img, Entry entry, float threshold)
    {
        // BuildUi()가 anchoredPosition을 -totalH로 설정할 때까지 한 프레임 대기
        yield return null;

        // 스크롤이 이 이미지 위치에 도달할 때까지 대기
        yield return new WaitUntil(() =>
            scrollContainer != null &&
            scrollContainer.anchoredPosition.y >= threshold);

        if (img == null) yield break;

        Texture2D horrorTex = Resources.Load<Texture2D>(entry.horrorPath);
        Texture2D normalTex = Resources.Load<Texture2D>(entry.normalPath);
        if (horrorTex == null || normalTex == null) yield break;

        Sprite horror = Sprite.Create(horrorTex,
            new Rect(0, 0, horrorTex.width, horrorTex.height), new Vector2(0.5f, 0.5f));
        Sprite normal = Sprite.Create(normalTex,
            new Rect(0, 0, normalTex.width, normalTex.height), new Vector2(0.5f, 0.5f));

        // 호러 이미지로 시작, 잠시 유지
        img.sprite = horror;
        yield return new WaitForSecondsRealtime(0.30f);

        // 4회 빠른 깜빡임 (normal→horror→normal→horror)
        for (int i = 0; i < 4; i++)
        {
            if (img == null) yield break;
            img.sprite = (i % 2 == 0) ? normal : horror;
            yield return new WaitForSecondsRealtime(0.08f);
        }
        // 4회 후 현재 상태: horror (i=3 → horror)

        // 2회 느린 깜빡임, 정상으로 마무리
        if (img == null) yield break;
        img.sprite = normal;
        yield return new WaitForSecondsRealtime(0.28f);
        if (img == null) yield break;
        img.sprite = horror;
        yield return new WaitForSecondsRealtime(0.28f);

        // 정상 이미지로 안착
        if (img != null) img.sprite = normal;
    }

    // ── 스크롤 ────────────────────────────────────────────────────────────────
    private IEnumerator ScrollCredits(float duration)
    {
        float totalH = _totalH;
        float startY = -totalH;
        float endY   = 1180f;

        float skipEnableAt = Time.unscaledTime + 5f;
        float elapsed      = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            scrollContainer.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, endY, t));

            if (!skipAllowed && Time.unscaledTime >= skipEnableAt)
                skipAllowed = true;

            yield return null;
        }

        scrollContainer.anchoredPosition = new Vector2(0f, endY);
    }

    // ── 타이틀 이미지 시퀀스 ──────────────────────────────────────────────────
    private IEnumerator PlayTitleImageSequence()
    {
        if (creditTitleImage != null && creditTitleSprite != null)
        {
            creditTitleImage.gameObject.SetActive(true);
            yield return FadeGraphic(creditTitleImage, 0f, 1f, 1f);
            yield return new WaitForSecondsRealtime(1.6f);
            yield return FadeGraphic(creditTitleImage, 1f, 0f, 1f);
            creditTitleImage.gameObject.SetActive(false);
            yield break;
        }

        titleText.gameObject.SetActive(true);
        yield return FadeGraphic(titleText, 0f, 1f, 0.8f);
        yield return new WaitForSecondsRealtime(1.6f);
        yield return FadeGraphic(titleText, 1f, 0f, 1f);
        titleText.gameObject.SetActive(false);
    }

    // ── 스킵 확인 ─────────────────────────────────────────────────────────────
    private void BuildSkipConfirmPanel(Font font)
    {
        skipConfirmPanel = new GameObject("SkipConfirm",
            typeof(RectTransform), typeof(Image));
        skipConfirmPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = skipConfirmPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.35f);
        rect.anchorMax = new Vector2(0.7f, 0.65f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        skipConfirmPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

        AddText(skipConfirmPanel.transform, "크레딧을 건너뛸까요?", font, 28,
            new Vector2(0f, 0.6f), new Vector2(1f, 1f));
        AddButton(skipConfirmPanel.transform, "예", font, 26,
            new Vector2(0.1f, 0.05f), new Vector2(0.45f, 0.45f),
            () => { StopAllCoroutines(); ShowEndMenu(); });
        AddButton(skipConfirmPanel.transform, "아니오", font, 26,
            new Vector2(0.55f, 0.05f), new Vector2(0.9f, 0.45f),
            () => { skipConfirmShowing = false; skipConfirmPanel.SetActive(false); });

        skipConfirmPanel.SetActive(false);
        skipConfirmPanel.transform.SetAsLastSibling();
    }

    private void ShowSkipConfirm()
    {
        skipConfirmShowing = true;
        skipConfirmPanel.SetActive(true);
    }

    // ── 엔딩 메뉴 ─────────────────────────────────────────────────────────────
    private void BuildEndMenuPanel(Font font)
    {
        endMenuPanel = new GameObject("EndMenu",
            typeof(RectTransform), typeof(Image));
        endMenuPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = endMenuPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.3f);
        rect.anchorMax = new Vector2(0.7f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        endMenuPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        AddButton(endMenuPanel.transform, "메인 메뉴", font, 28,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f), ReturnToMain);
        AddButton(endMenuPanel.transform, "종료", font, 28,
            new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.45f),
            ApplicationQuitHelper.Quit);

        endMenuPanel.SetActive(false);
        endMenuPanel.transform.SetAsLastSibling();
    }

    private void ShowEndMenu()
    {
        skipAllowed = false;
        if (skipConfirmPanel != null) skipConfirmPanel.SetActive(false);
        if (endMenuPanel     != null) endMenuPanel.SetActive(true);
    }

    private static void ReturnToMain()
    {
        if (BossBattleMusicRuntime.Instance != null)
            Destroy(BossBattleMusicRuntime.Instance.gameObject);
        Time.timeScale = 1f;
        SceneManager.LoadScene(FirstSceneName);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────
    private IEnumerator FadeGraphic(Graphic g, float from, float to, float duration)
    {
        if (g == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            Color c = g.color; c.a = Mathf.Lerp(from, to, t); g.color = c;
            yield return null;
        }
        Color fc = g.color; fc.a = to; g.color = fc;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        Transform fadeT = mainCanvas?.transform.Find("FadePanel");
        if (fadeT == null) yield break;
        yield return FadeGraphic(fadeT.GetComponent<Image>(), 1f, 0f, duration);
    }

    private IEnumerator FadeToBlack(float duration)
    {
        Transform fadeT = mainCanvas?.transform.Find("FadePanel");
        if (fadeT == null) yield break;
        Image fi = fadeT.GetComponent<Image>();
        yield return FadeGraphic(fi, fi.color.a, 1f, duration);
    }

    private static void AddText(Transform parent, string text, Font font, int size,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(8f, 0f); r.offsetMax = new Vector2(-8f, 0f);
        Text tx = go.GetComponent<Text>();
        tx.text = text; tx.font = font; tx.fontSize = size;
        tx.color = Color.white; tx.alignment = TextAnchor.MiddleCenter;
    }

    private static void AddButton(Transform parent, string label, Font font, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(label + "Btn",
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(4f, 4f); r.offsetMax = new Vector2(-4f, -4f);
        go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        AddText(go.transform, label, font, fontSize, Vector2.zero, Vector2.one);
        go.GetComponent<Button>().onClick.AddListener(action);
    }

    private static void FullStretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    private static Font GetKoreanFont()
    {
        Font f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Arial Unicode MS", "Noto Sans CJK KR" }, 30);
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
