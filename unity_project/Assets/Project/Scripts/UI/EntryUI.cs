using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EntryUI : MonoBehaviour
{
    [Tooltip("Scene to load when Start is pressed (name or path).")]
    public string startSceneName = "Stage01_ParkingToHospital";

    [Header("Opening Music")]
    [SerializeField] private AudioClip openingMusic;
    [SerializeField] private AudioSource openingMusicSource;
    [SerializeField] private float openingMusicVolume = 0.65f;
    [SerializeField] private float openingMusicFadeOutDuration = 0.6f;

    private Button startButton;
    private Button quitButton;
    private bool isStarting;

    private void Awake()
    {
        EnsureUiCanReceiveClicks();
        BindButtons();

        if (openingMusicSource == null)
            openingMusicSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        PlayOpeningMusic();
    }

    private void Update()
    {
        if (isStarting)
            return;

        if (Input.GetMouseButtonDown(0) && IsPointerInsideButton(startButton))
        {
            OnStartClicked();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space))
        {
            OnStartClicked();
        }
    }

    public void OnStartClicked()
    {
        if (isStarting)
            return;

        isStarting = true;
        if (startButton != null)
            startButton.interactable = false;

        StartCoroutine(StartWithMusicFade());
    }

    private IEnumerator StartWithMusicFade()
    {
        yield return StartCoroutine(FadeOutOpeningMusic());
        SceneManager.LoadScene(startSceneName);
    }

    public void OnQuitClicked()
    {
        ApplicationQuitHelper.Quit();
    }

    private void PlayOpeningMusic()
    {
        if (openingMusicSource == null)
            openingMusicSource = gameObject.AddComponent<AudioSource>();

        if (openingMusicSource.clip == null)
            openingMusicSource.clip = openingMusic;

        if (openingMusicSource.clip == null)
            return;

        openingMusicSource.loop = true;
        openingMusicSource.playOnAwake = true;
        openingMusicSource.spatialBlend = 0f;
        openingMusicSource.volume = Mathf.Max(0f, openingMusicVolume);

        if (!openingMusicSource.isPlaying)
            openingMusicSource.Play();
    }

    private IEnumerator FadeOutOpeningMusic()
    {
        if (openingMusicSource == null || !openingMusicSource.isPlaying)
            yield break;

        float startVolume = openingMusicSource.volume;
        float elapsed = 0f;
        while (elapsed < openingMusicFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = openingMusicFadeOutDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / openingMusicFadeOutDuration);
            openingMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        openingMusicSource.Stop();
    }

    private void StopOpeningMusic()
    {
        if (openingMusicSource == null)
            return;

        openingMusicSource.Stop();
    }

    private void BindButtons()
    {
        startButton = FindButton("StartButton");
        if (startButton != null)
        {
            startButton.interactable = true;
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
            if (startButton.targetGraphic != null)
                startButton.targetGraphic.raycastTarget = true;
        }

        quitButton = FindButton("QuitButton");
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            if (quitButton.targetGraphic != null)
                quitButton.targetGraphic.raycastTarget = true;
        }
    }

    private static Button FindButton(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        return buttonObject == null ? null : buttonObject.GetComponent<Button>();
    }

    private static bool IsPointerInsideButton(Button button)
    {
        if (button == null || !button.interactable)
            return false;

        RectTransform rect = button.transform as RectTransform;
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);
    }

    private static void EnsureUiCanReceiveClicks()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            canvas.transform.localScale = Vector3.one;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return;
        }

        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
    }
}
