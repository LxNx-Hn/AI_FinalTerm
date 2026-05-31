using UnityEngine;
using DreamNoms.HeartSystem.EventSystem;

namespace DreamNoms.HeartSystem.Example
{
    /// <summary>
    /// An example script for different events on health system
    /// </summary>
    public class ExampleEventSubscriber : MonoBehaviour
    {
        //Include a reference to the heart event you want to listen for
        [SerializeField]
        private HeartEvents heartEvents;

        //Subscribe to the events you want to listen to in OnEnable
        private void OnEnable()
        {
            //All possible events to subscribe to.
            heartEvents.OnMaxHealthChanged += OnMaxHealthChanged;
            heartEvents.OnHealthChanged += OnHealthChanged;
            heartEvents.OnDeath += OnDeath;
            heartEvents.OnRevive += OnRevive;
            heartEvents.OnEnteredLowHealth += OnEnteredLowHealth;
            heartEvents.OnExitLowHealth += OnExitLowHealth;
            heartEvents.OnEnteredFullHealth += OnEnteredFullHealth;
            heartEvents.OnExitFullHealth += OnExitFullHealth;
        }

        //Unsubscribe from the events you have subscribed to in OnDisable
        private void OnDisable()
        {
            heartEvents.OnMaxHealthChanged -= OnMaxHealthChanged;
            heartEvents.OnHealthChanged -= OnHealthChanged;
            heartEvents.OnDeath -= OnDeath;
            heartEvents.OnRevive -= OnRevive;
            heartEvents.OnEnteredLowHealth -= OnEnteredLowHealth;
            heartEvents.OnExitLowHealth -= OnExitLowHealth;
            heartEvents.OnEnteredFullHealth -= OnEnteredFullHealth;
            heartEvents.OnExitFullHealth -= OnExitFullHealth;
        }

        public void OnMaxHealthChanged(int oldMaxHealth, int newMaxHealth)
        {
            Debug.Log("OnMaxHealthChanged: Max Health has changed from " + oldMaxHealth + " to " + newMaxHealth);
        }

        public void OnHealthChanged(float oldHealth, float newHealth)
        {
            Debug.Log("OnHealthChanged: Health has changed from " + oldHealth + " to " + newHealth);
        }

        public void OnDeath()
        {
            Debug.Log("OnDeath: The player has died. You can do something like show a death canvas or play an animation");
        }

        public void OnRevive()
        {
            Debug.Log("OnRevive: The player has gained health after being dead.");
        }

        public void OnEnteredLowHealth()
        {
            Debug.Log("OnEnteredLowHealth: The player is low on health. Perhaps make player blink or apply blink effect to heart");
        }

        public void OnExitLowHealth()
        {
            Debug.Log("OnExitLowHealth: The player is no longer low on health.");
        }

        public void OnEnteredFullHealth()
        {
            Debug.Log("OnEnteredFullHealth: The player is full on health. Perhaps make them more powerful");
        }

        public void OnExitFullHealth()
        {
            Debug.Log("OnExitFullHealth: The player is no longer at maximum health. Perhaps remove their power");
        }

    }


}
