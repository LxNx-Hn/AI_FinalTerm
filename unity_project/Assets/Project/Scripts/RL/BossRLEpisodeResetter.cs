using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BossRLEpisodeResetter : MonoBehaviour
{
    private bool reloadQueued;
    private PlayerHealth playerHealth;
    private BossHealth bossHealth;
    private BossPlayerAgent agent;

    public bool ReloadQueued => reloadQueued;

    private void Awake()
    {
        agent = GetComponent<BossPlayerAgent>();
        playerHealth = GetComponent<PlayerHealth>();
        bossHealth = FindFirstObjectByType<BossHealth>();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    public void QueueSceneReload()
    {
        if (reloadQueued)
        {
            return;
        }

        reloadQueued = true;
        StartCoroutine(ReloadSceneRoutine());
    }

    private void SubscribeEvents()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (bossHealth == null)
        {
            bossHealth = FindFirstObjectByType<BossHealth>();
        }

        if (agent == null)
        {
            agent = GetComponent<BossPlayerAgent>();
        }

        if (playerHealth != null)
        {
            playerHealth.onDead.RemoveListener(HandlePlayerDead);
            playerHealth.onDead.AddListener(HandlePlayerDead);
        }

        if (bossHealth != null)
        {
            bossHealth.onDead.RemoveListener(HandleBossDead);
            bossHealth.onDead.AddListener(HandleBossDead);
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.onDead.RemoveListener(HandlePlayerDead);
        }

        if (bossHealth != null)
        {
            bossHealth.onDead.RemoveListener(HandleBossDead);
        }
    }

    private void HandlePlayerDead()
    {
        HandleTerminalEvent("player_dead");
    }

    private void HandleBossDead()
    {
        HandleTerminalEvent("boss_dead");
    }

    private void HandleTerminalEvent(string reason)
    {
        if (reloadQueued)
        {
            return;
        }

        agent?.HandleTerminalEvent(reason);
        QueueSceneReload();
    }

    private IEnumerator ReloadSceneRoutine()
    {
        yield return null;
        CutsceneFreezeManager.ForceUnlock();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
