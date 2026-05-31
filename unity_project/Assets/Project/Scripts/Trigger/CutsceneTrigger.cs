using System.Collections;
using TMPro;
using UnityEngine;

public class CutsceneTrigger : GridZoneTriggerBase
{
    [Header("Cutscene")]
    [SerializeField] private GameObject panelToShow;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string message;
    [SerializeField] private float showDuration = 3f;
    [SerializeField] private bool lockPlayerDuringCutscene = true;
    [SerializeField] private AudioClip sfx;

    protected override bool OnTriggered()
    {
        StartCoroutine(PlayCutscene());
        return true;
    }

    private IEnumerator PlayCutscene()
    {
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }

        if (messageText != null)
        {
            messageText.text = message;
        }

        PlayerController pc = null;
        if (lockPlayerDuringCutscene && player != null)
        {
            pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.enabled = false;
            }
        }

        if (sfx != null)
        {
            AudioSource.PlayClipAtPoint(sfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }

        yield return new WaitForSeconds(showDuration);

        if (panelToShow != null)
        {
            panelToShow.SetActive(false);
        }

        if (pc != null)
        {
            pc.enabled = true;
        }
    }
}
