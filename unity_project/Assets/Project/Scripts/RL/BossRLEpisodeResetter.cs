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

    private static int reloadCount = 0;
    private float queueTime = -1f;

    public bool ReloadQueued => reloadQueued;

    private void Awake()
    {
        // Log when the scene has finished loading (after a reload)
        if (reloadCount > 0)
        {
            Debug.Log($"[BossRLReset] after_scene_loaded reload_count={reloadCount} time={Time.realtimeSinceStartup:F3}");
        }
    }

    private void Start()
    {
        agent = GetComponent<BossPlayerAgent>();
        playerHealth = GetComponent<PlayerHealth>();
        bossHealth = FindFirstObjectByType<BossHealth>();
        // Re-subscribe after scene reload (SubscribeEvents is called in OnEnable before Start,
        // but references may not be resolved yet; ensure they are resolved here)
        SubscribeEvents();
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
        queueTime = Time.realtimeSinceStartup;
        Debug.Log($"[BossRLReset] queue_reload reload_count={reloadCount} time={queueTime:F3}");
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

        if (agent != null && agent.IsProcessingAction)
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
        float beforeTime = Time.realtimeSinceStartup;
        float elapsed = beforeTime - (queueTime >= 0f ? queueTime : beforeTime);
        Debug.Log($"[BossRLReset] before_load_scene reload_count={reloadCount} time={beforeTime:F3} elapsed_since_queue={elapsed:F3}s");
        reloadCount++;
        // LoadSceneAsync keeps the game loop responsive during load (no blocking main thread),
        // so ML-Agents communicator stays alive and avoids environment timeout.
        string sceneName = SceneManager.GetActiveScene().name;
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (op != null && !op.isDone)
            yield return null;
    }
}
