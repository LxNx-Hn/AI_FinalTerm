using UnityEngine;
using System.Collections.Generic;
using DreamNoms.HeartSystem.EventSystem;

namespace DreamNoms.HeartSystem
{
    /// <summary>
    /// Handles instantiating/destroying hearts
    /// </summary>
    public class HeartContainer : MonoBehaviour
    {
        #region private inspector variables
        [SerializeField]
        private HeartEvents heartEvents;

        [SerializeField]
        private HealthController healthController;

        [SerializeField]
        private GameObject _heartPrefab;
        #endregion

        [Header("Debug")]
        [Tooltip("The heart children in this container")]
        private List<Heart> _hearts;

        #region public properties
        /// <summary>
        /// All hearts that this container holds
        /// </summary>
        public List<Heart> Hearts
        {
            get { return _hearts; }
        }
        #endregion

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            if (heartEvents == null)
            {
                Debug.LogError("NullReferenceException: HeartEvent is null on HeartContainer script", this);
            }
            if (healthController == null)
            {
                Debug.LogError("NullReferenceException: HealthController is null on HeartContainer script", this);
            }

            _hearts = new List<Heart>();
            _hearts.AddRange(gameObject.GetComponentsInChildren<Heart>());
        }

        private void Start()
        {
            //sets the correct number of hearts at the start
            UpdateNumHearts(_hearts.Count, healthController.MaxHealth);
        }

        private void OnEnable()
        {
            heartEvents.OnMaxHealthChanged+=(UpdateNumHearts);
            heartEvents.OnHealthChanged+=((oldHealth, newHealth) => UpdateHeartFillAmount());
        }

        private void OnDisable()
        {
            heartEvents.OnMaxHealthChanged-=(UpdateNumHearts);
            heartEvents.OnHealthChanged-=((oldHealth, newHealth) => UpdateHeartFillAmount());
        }

        #region private void functions

        /// <summary>
        /// Updates the fill amount on all the hearts to reflect the current health
        /// </summary>
        private void UpdateHeartFillAmount()
        {
            for (int i = 0; i < Hearts.Count; i++)
            {
                Hearts[i].SetFillAmount(healthController.Health - i);
            }
        }

        /// <summary>
        /// Creates/Destroys Heart children so number of heart children is equal to NumHearts.
        /// </summary>
        private void UpdateNumHearts(int oldValue, int newValue)
        {            
            int numHeartsToAdd = newValue - oldValue;
            int numHeartsToDelete = -numHeartsToAdd;

            //if positive it should be adding hearts
            if (numHeartsToAdd > 0)
            {
                InstantiateHearts(numHeartsToAdd);
            }
            else if (numHeartsToDelete > 0)
            {
                DeleteHearts(numHeartsToDelete);
            }

            //make the fill of the newly added heart be correct
            UpdateHeartFillAmount();
        }

        /// <summary>
        /// Instantiates num more heartPrefabs to the end of this container
        /// </summary>
        /// <param name="num">The number of new hearts to add</param>
        private void InstantiateHearts(int num)
        {
            for (int i=0; i<num; i++)
            {
                GameObject newHeart = Instantiate(_heartPrefab,this.transform);
                _hearts.Add(newHeart.GetComponent<Heart>());
            }
        }

        /// <summary>
        /// Deletes num hearts starting from last child (LIFO)
        /// </summary>
        /// <param name="num">The number of hearts to delete</param>
        private void DeleteHearts(int num)
        {
            for (int i=0; i<num; i++)
            {
                Heart lastHeart = _hearts[_hearts.Count - 1];
                _hearts.RemoveAt(_hearts.Count - 1);
                Destroy(lastHeart.gameObject);
            }
        }
        #endregion
    }

}
