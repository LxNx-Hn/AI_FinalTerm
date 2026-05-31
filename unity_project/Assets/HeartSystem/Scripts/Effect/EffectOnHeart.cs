using UnityEngine;
using System.Collections.Generic;
using System.Collections;


namespace DreamNoms.HeartSystem.Effect
{
    [RequireComponent(typeof(Heart))]
    /// <summary>
    /// This class manages and runs effects on a single heart over time
    /// </summary>
    public class EffectOnHeart : MonoBehaviour
    {
        private Heart heartScript;

        private Dictionary<HeartEffectSO, HeartEffectCoroutine> heartEffects = new Dictionary<HeartEffectSO, HeartEffectCoroutine>();

        private List<HeartEffectSO> heartEffectsKeys
        {
            get
            {
                return new List<HeartEffectSO>(heartEffects.Keys);
            }
        }

        private class HeartEffectCoroutine
        {
            /// <summary>
            /// A reference to the running coroutine
            /// </summary>
            public Coroutine coroutine { get; set; }
            
            /// <summary>
            /// Number of times this routine has been run
            /// </summary>
            public int iterations { get; private set; }

            public HeartEffectCoroutine()
            {
                iterations = 0;
            }

            public void NextIteration() { iterations += 1; }
        }

        private void Awake()
        {
            heartScript = gameObject.GetComponent<Heart>();
            heartEffects = new Dictionary<HeartEffectSO, HeartEffectCoroutine>();
        }

        /// <summary>
        /// Begin the specified effect on this heart if it isn't already doing it
        /// </summary>
        public void BeginEffect(HeartEffectSO effect)
        {
            if (heartEffects.ContainsKey(effect))
            {
                Debug.Log("Heart already has the effect " + effect);
            }
            else
            {
                heartEffects.Add(effect, new HeartEffectCoroutine());
                Coroutine effectRoutine=StartCoroutine(InvokeEffectRepeating(effect));
                heartEffects[effect].coroutine = effectRoutine;
            }
        }

        /// <summary>
        /// Stop the specified effect on this heart
        /// </summary>
        public void StopEffect(HeartEffectSO effect)
        {
            if (heartEffects.ContainsKey(effect) == false)
            {
                Debug.Log("Heart is not doing the effect " + effect);
            }
            else
            {
                StopCoroutine(heartEffects[effect].coroutine);
                heartEffects.Remove(effect);
                effect.RestorePreviousState(heartScript);
            }
        }

        /// <summary>
        /// Stops all effects on this heart
        /// </summary>
        public void StopAllEffects()
        {
            StopAllCoroutines();

            //keys does not change size when effects are removed from dictionary
            List<HeartEffectSO> keys = heartEffectsKeys;

            foreach (HeartEffectSO effect in keys)
            {
                StopEffect(effect);
            }
        }

        /// <summary>
        /// Stops all effects that that match the specified type
        /// </summary>
        public void StopEffectsOfType(System.Type type)
        {
            //create a list of keys that does not change size when effects are removed from dictionary
            List<HeartEffectSO> keys = heartEffectsKeys;

            foreach (HeartEffectSO effect in keys)
            {
                if (effect.GetType() == type)
                {
                    StopEffect(effect);
                }
            }
        }

        private IEnumerator InvokeEffectRepeating(HeartEffectSO effect)
        {
            //repeat until the key is removed (or until manually halting the couroutine)
            while (heartEffects.ContainsKey(effect))
            {
                heartEffects[effect].NextIteration();

                // Call the function with the parameters
                effect.DoEffectOnHeart(heartScript, heartEffects[effect].iterations);

                // Wait for the specified repeat rate
                yield return new WaitForSeconds(effect.repeatRate);
            }
        }
    }

}

