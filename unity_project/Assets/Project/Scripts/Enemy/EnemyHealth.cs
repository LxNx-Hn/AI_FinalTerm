using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHp = 3;
    public int currentHp = 3;

    [Tooltip("체크 시 킬 카운트(루트 추적)에서 제외됩니다. 잡을 수 없는 미니보스·연출용 적에 사용하세요.")]
    public bool excludeFromKillCount = false;

    [Header("Audio")]
    public AudioClip hitSfx;
    public float hitSfxVolume = 0.9f;
    public float minHitSfxInterval = 0.08f;

    
[Header("Hit")]
    public float stunDuration = 0.3f;
    public SpriteRenderer spriteRenderer;

    [Header("Refs")]
    public EnemyAI ai;
    public DropTable dropTable;
    public GridOccupant occupant;

    private bool dead;
    private AudioSource audioSource;
    private Coroutine stunCoroutine;
    private float nextHitSfxTime;

    private Color originalColor = Color.white;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (ai == null)
        {
            ai = GetComponent<EnemyAI>();
        }

        if (dropTable == null)
        {
            dropTable = GetComponent<DropTable>();
        }

        if (occupant == null)
        {
            occupant = GetComponent<GridOccupant>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        currentHp = maxHp;
        if (!excludeFromKillCount)
            RunRouteTracker.RegisterEnemyForCurrentScene(gameObject);
    }

    public void TakeDamage(int damage)
    {
        if (dead)
        {
            return;
        }

        currentHp -= damage;

        if (currentHp <= 0)
        {
            PlayHitSfx();
            Die();
            return;
        }

        PlayHitSfx();

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine());
    }

    private void PlayHitSfx()
    {
        if (hitSfx == null || Time.unscaledTime < nextHitSfxTime)
        {
            return;
        }

        nextHitSfxTime = Time.unscaledTime + Mathf.Max(0f, minHitSfxInterval);

        if (audioSource != null)
        {
            audioSource.PlayOneShot(hitSfx, hitSfxVolume);
        }
        else
        {
            Play2D(hitSfx, hitSfxVolume);
        }
    }

    private IEnumerator StunRoutine()
    {
        if (ai != null)
            ai.SetStunned(true);

        if (spriteRenderer != null)
        {
            // 피격 순간 흰색 플래시 (0.05 초)
            spriteRenderer.color = Color.white;
            yield return new WaitForSecondsRealtime(0.05f);
            // 이후 스턴 회색
            spriteRenderer.color = Color.gray;
        }

        yield return new WaitForSeconds(stunDuration);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (ai != null && !dead)
            ai.SetStunned(false);

        stunCoroutine = null;
    }

    private static void Play2D(AudioClip clip, float volume)
    {
        var go = new GameObject("_sfx_tmp");
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.PlayOneShot(clip, volume);
        Destroy(go, clip.length + 0.1f);
    }

    private void Die()
    {
        dead = true;
        if (!excludeFromKillCount)
            RunRouteTracker.RegisterEnemyDeathForCurrentScene(gameObject);

        if (occupant != null)
        {
            occupant.Release();
        }

        if (dropTable != null)
        {
            dropTable.TryDrop(transform.position);
        }

        gameObject.SetActive(false);
    }
}
