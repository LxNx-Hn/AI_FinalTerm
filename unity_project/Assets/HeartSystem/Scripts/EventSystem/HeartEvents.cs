using UnityEngine;

namespace DreamNoms.HeartSystem.EventSystem
{
    public class HeartEvents : MonoBehaviour
    {
        /// <summary>
        /// Called when maximum health changes (eg player gained or lost a heart)
        /// int1: previous number of hearts
        /// int2: new number of hearts
        /// </summary>
        public event System.Action<int, int> OnMaxHealthChanged;
        public void RaiseMaxHealthChanged(int oldMaxHealth, int newMaxHealth) { OnMaxHealthChanged?.Invoke(oldMaxHealth, newMaxHealth); }

        /// <summary>
        /// Called when current health is increased or decreased.
        /// </summary>
        public event System.Action<float, float> OnHealthChanged;
        public void RaiseHealthChanged(float oldHealth, float newHealth) { OnHealthChanged?.Invoke(oldHealth, newHealth); }

        /// <summary>
        /// Called when this object has died
        /// </summary>
        public event System.Action OnDeath;
        public void RaiseDeath() { OnDeath?.Invoke(); }

        /// <summary>
        /// Called when this object has come back from the dead
        /// </summary>
        public event System.Action OnRevive;
        public void RaiseRevive() { OnRevive?.Invoke(); }

        /// <summary>
        /// Called when it goes in low health state
        /// </summary>
        public event System.Action OnEnteredLowHealth;
        public void RaiseEnteredLowHealth() { OnEnteredLowHealth?.Invoke(); }

        /// <summary>
        /// Called when it exits a low health state
        /// </summary>
        public event System.Action OnExitLowHealth;
        public void RaiseExitLowHealth() { OnExitLowHealth?.Invoke(); }

        /// <summary>
        /// Called when this object goes into full health
        /// </summary>
        public event System.Action OnEnteredFullHealth;
        public void RaiseEnteredFullHealth() { OnEnteredFullHealth?.Invoke(); }

        /// <summary>
        /// Called when this object becomes no longer in full health
        /// </summary>
        public event System.Action OnExitFullHealth;
        public void RaiseExitFullHealth() { OnExitFullHealth?.Invoke(); }

    }


}
