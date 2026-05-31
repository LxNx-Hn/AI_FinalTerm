using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalEndingTrigger : GridZoneTriggerBase
{
    [SerializeField] private string endingSceneName = "EndingCredits";
    [SerializeField] private float delayBeforeLoad = 1f;

    protected override bool OnTriggered()
    {
        StartCoroutine(LoadEnding());
        return true;
    }

    private IEnumerator LoadEnding()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(endingSceneName);
    }
}
