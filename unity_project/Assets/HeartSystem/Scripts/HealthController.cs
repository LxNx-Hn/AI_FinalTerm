using UnityEngine;
using DreamNoms.HeartSystem.EventSystem;

namespace DreamNoms.HeartSystem
{
    public class HealthController : MonoBehaviour
    {
        //The health remaining to be registered as dead. (an epsilon value).
        //If health is less than OR equal to DEAD_THRESHOLD then it is dead
        private const float DEAD_THRESHHOLD= 0.1f;

        [SerializeField]
        private HeartEvents heartEvents;

        [SerializeField, Min(1)]
        [Tooltip("The maximum health")]
        private int _maxHealth = 3;

        [SerializeField, Min(0)]
        [Tooltip("The current health")]
        private float _health;

        [SerializeField, Min(0)]
        [Tooltip("When health gets at this value or lower, it will call OnEnteredLowHealth event")]
        private float _lowHealth = 1;

        private bool _isLowHealth = false;
        private bool _isFullHealth = false;
        private bool _isDead = false;

        /// <summary>
        /// The maximum health
        /// </summary>
        public int MaxHealth
        {
            get { return _maxHealth; }

            set
            {
                if (value<=0)
                {
                    Debug.LogWarning("Failed to set MaxHealth to "+value+"! Setting MaxHealth to minimum (1) instead.");
                }
                int oldValue = _maxHealth;
                int newValue = Mathf.Max(1, value);
                _maxHealth = newValue;

                if (oldValue != newValue)
                {
                    //Prevent health from exceeding max health (could happen when decreasing maxHealth)
                    //Also call events if max health increased and health is no longer at maximum health
                    Health = GetClampedHealthValue(_health);

                    heartEvents.RaiseMaxHealthChanged(oldValue, newValue);    
                }

            }

        }

        /// <summary>
        /// The current health
        /// </summary>
        public float Health
        {
            get
            {
                return GetClampedHealthValue(_health);
            }

            set
            {
                float oldValue = _health;
                float newValue = GetClampedHealthValue(value);

                _health = newValue;

                if (oldValue != newValue)
                {
                    heartEvents.RaiseHealthChanged(oldValue, newValue);
                }

                //still trigger these regardless of if health changed
                //they should run in case max health changed
                LowHealthEventCheck(newValue);
                FullHealthEventCheck(newValue);
                DeadReviveEventCheck();
            }
        }

        public bool IsDead
        {
            get { return Health <= DEAD_THRESHHOLD; }
        }

        private void OnValidate()
        {
            _health = GetClampedHealthValue(_health);
        }

        private void Awake()
        {
            if (heartEvents == null)
            {
                Debug.LogError("NullReferenceException: HeartEvents is not assigned in inspector!", this);
            }

            // Set initial health, fire events correctly
            Health = GetClampedHealthValue(_health);
        }

        /// <summary>
        /// Adds additional hearts to the container. Does not change current health.
        /// </summary>
        /// <param name="amount"></param>
        public void IncreaseMaxHealth(int amount = 1) { MaxHealth += amount; }

        /// <summary>
        /// Removes hearts from the container.
        /// </summary>
        /// <param name="amount"></param>
        public void DecreaseMaxHealth(int amount = 1) { MaxHealth -= amount; }

        /// <summary>
        /// Sets current health equal to the maximum health
        /// </summary>
        public void FullHeal() { Health = MaxHealth; }

        /// <summary>
        /// Increases current health
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount) { Health += amount; }

        /// <summary>
        /// Decreases current health
        /// </summary>
        /// <param name="amount"></param>
        public void TakeDamage(float amount) { Health -= amount; }

        /// <summary>
        /// Sets health to 0
        /// </summary>
        public void Die() { Health = 0; }

        /// <summary>
        /// Clamps the value to be within a 0-MaxHealth range. Will return 0 if value is equal or below DEAD_THRESHHOLD
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float GetClampedHealthValue(float value)
        {
            if (value <= DEAD_THRESHHOLD)
            {
                return 0;
            }
            else
            {
                return Mathf.Clamp(value, 0, MaxHealth);
            }
        }

        private void LowHealthEventCheck(float newValue)
        {
            bool wasLow = _isLowHealth;
            _isLowHealth = newValue <= _lowHealth;
            if (wasLow != _isLowHealth)
            {
                if (_isLowHealth)
                {
                    heartEvents.RaiseEnteredLowHealth();
                }
                else
                {
                    heartEvents.RaiseExitLowHealth();
                }
            }
        }

        private void FullHealthEventCheck(float newValue)
        {
            bool wasFull = _isFullHealth;
            _isFullHealth = newValue >= MaxHealth;
            if (wasFull != _isFullHealth)
            {
                if (_isFullHealth)
                {
                    heartEvents.RaiseEnteredFullHealth();
                }
                else
                {
                    heartEvents.RaiseExitFullHealth();
                }
            }
        }

        private void DeadReviveEventCheck()
        {
            bool wasDead = _isDead;
            _isDead = IsDead;
            if (wasDead != IsDead)
            {
                if (_isDead)
                {
                    heartEvents.RaiseDeath();
                }
                else
                {
                    heartEvents.RaiseRevive();
                }
            }
        }

    }

}

