using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHp = 3;
    public int currentHp = 3;

    [Header("Invincible")]
    public float invincibleDuration = 1f;
    public float blinkInterval = 0.08f;
    public float damageTintDuration = 0.36f;
    public Color damageTintColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;

    [Header("Damage Feedback")]
    public AudioClip hitSfx;
    public float hitSfxVolume = 0.85f;

    public bool IsInvincible { get; private set; }
    public bool IsDead { get; private set; }

    public UnityEvent<int, int> onHpChanged;
    public UnityEvent onDead;

    private Coroutine invincibleRoutine;
    private bool invincibleShowsDamageFeedback = true;

    private void OnValidate()
    {
        // maxHp를 바꾸면 currentHp도 맞춰서 업데이트 (에디터 Inspector 편의)
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        IsDead = false;
        currentHp = maxHp;
        IsInvincible = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }
        onHpChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(int damage)
    {
        if (IsInvincible || IsDead)
        {
            return;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        onHpChanged?.Invoke(currentHp, maxHp);
        PlayHitSfx();

        if (currentHp <= 0)
        {
            if (invincibleRoutine != null)
            {
                StopCoroutine(invincibleRoutine);
                invincibleRoutine = null;
            }

            IsInvincible = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = Color.white;
            }

            IsDead = true;
            onDead?.Invoke();
            return;
        }

        StartInvincible(invincibleDuration, true);
    }

    private void PlayHitSfx()
    {
        if (hitSfx == null)
        {
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(hitSfx, hitSfxVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(hitSfx, Camera.main != null ? Camera.main.transform.position : transform.position, hitSfxVolume);
        }
    }

    public void Heal(int amount)
    {
        if (IsDead)
        {
            return;
        }

        currentHp = Mathf.Min(maxHp, currentHp + amount);
        onHpChanged?.Invoke(currentHp, maxHp);
    }

    public void SetInvincibleFor(float duration, bool showDamageFeedback = false)
    {
        StartInvincible(duration, showDamageFeedback);
    }

    private void StartInvincible(float duration, bool showDamageFeedback)
    {
        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
        }

        invincibleShowsDamageFeedback = showDamageFeedback;
        invincibleRoutine = StartCoroutine(InvincibleRoutine(duration));
    }

    private IEnumerator InvincibleRoutine(float duration)
    {
        IsInvincible = true;
        float timer = 0f;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (timer < duration)
        {
            if (spriteRenderer != null)
            {
                if (invincibleShowsDamageFeedback && timer < damageTintDuration)
                {
                    spriteRenderer.enabled = true;
                    float t = Mathf.Clamp01(timer / Mathf.Max(0.0001f, damageTintDuration));
                    spriteRenderer.color = Color.Lerp(damageTintColor, originalColor, t);
                }
                else if (invincibleShowsDamageFeedback)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                    spriteRenderer.color = originalColor;
                }
                else
                {
                    spriteRenderer.enabled = true;
                    spriteRenderer.color = originalColor;
                }
            }

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        IsInvincible = false;
        invincibleRoutine = null;
    }
}
