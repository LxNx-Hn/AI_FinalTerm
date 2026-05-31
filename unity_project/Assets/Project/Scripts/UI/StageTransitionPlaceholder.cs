using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageTransitionPlaceholder : MonoBehaviour
{
    [SerializeField] private string continueSceneName = "Hospital";
    [SerializeField] private string returnSceneName = "Entry";
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float inputDelay = 0.35f;

    private float enterAllowedAt;

    private void Start()
    {
        enterAllowedAt = Time.unscaledTime + inputDelay;

        if (promptText != null)
        {
            promptText.text = "Stage 2 is ready.\nPress Enter to continue.";
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < enterAllowedAt)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(continueSceneName);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(returnSceneName);
        }
    }
}
