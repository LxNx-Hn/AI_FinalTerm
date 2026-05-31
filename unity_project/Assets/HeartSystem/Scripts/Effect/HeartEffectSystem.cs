using UnityEngine;

namespace DreamNoms.HeartSystem.Effect
{
    public class HeartEffectSystem : MonoBehaviour
    {
        [SerializeField]
        private HeartContainer heartContainer;

        public void BeginEffectSingle(HeartEffectSO effect)
        {
            BeginEffect(effect, BeginEffectMode.Single);
        }

        public void BeginEffectAdditive(HeartEffectSO effect)
        {
            BeginEffect(effect, BeginEffectMode.Additive);
        }

        /// <summary>
        /// Make all hearts do the specified effect.
        /// </summary>
        /// <param name="effect">The effect to apply</param>
        /// <param name="mode">The mode determines whether to stack the effects. Not all effects can be stackable</param>
        public void BeginEffect(HeartEffectSO effect, BeginEffectMode mode=BeginEffectMode.Single)
        {    
            foreach (Heart h in heartContainer.Hearts)
            {
                EffectOnHeart effectOnHeart = h.gameObject.GetComponent<EffectOnHeart>();
                if (mode == BeginEffectMode.Single)
                {
                    effectOnHeart.StopAllEffects();
                }
                //cannot stack effects of same type (eg two Blink effects) 
                else if (mode == BeginEffectMode.Additive)
                {
                    effectOnHeart.StopEffectsOfType(effect.GetType());
                }

                effectOnHeart.BeginEffect(effect);
            }
        }

        /// <summary>
        /// Stops the specified effect on all hearts
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void StopEffect(HeartEffectSO effect)
        {
            foreach (Heart h in heartContainer.Hearts)
            {
                h.gameObject.GetComponent<EffectOnHeart>().StopEffect(effect);
            }
        }

        /// <summary>
        /// Stops all effects on all hearts
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void StopAllEffects()
        {
            foreach (Heart h in heartContainer.Hearts)
            {
                h.gameObject.GetComponent<EffectOnHeart>().StopAllEffects();
            }
        }

    }


}
