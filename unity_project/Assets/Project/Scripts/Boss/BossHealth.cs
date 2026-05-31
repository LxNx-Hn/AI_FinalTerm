using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    public int maxHp = 40;
    public int currentHp = 40;
    public bool canTakeDamage = true;

    public UnityEvent<int, int> onHpChanged = new UnityEvent<int, int>();
    public UnityEvent onDamaged = new UnityEvent();
    public UnityEvent onDead = new UnityEvent();

    private bool deadInvoked;

    private void Awake()
    {
        if (onHpChanged == null)
            onHpChanged = new UnityEvent<int, int>();
        if (onDamaged == null)
            onDamaged = new UnityEvent();
        if (onDead == null)
            onDead = new UnityEvent();
    }

    private void Start()
    {
        currentHp = maxHp;
        deadInvoked = false;
        onHpChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(int damage)
    {
        if (!canTakeDamage)
        {
            return;
        }

        if (currentHp <= 0)
        {
            return;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        onDamaged?.Invoke();
        onHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0 && !deadInvoked)
        {
            deadInvoked = true;
            onDead?.Invoke();
        }
    }

    public void SetDamageable(bool value)
    {
        canTakeDamage = value;
    }
}
