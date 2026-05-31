using UnityEngine;
using UnityEngine.UI;

namespace DreamNoms.HeartSystem
{
    /// <summary>
    /// Controls the default appearance of a single heart
    /// </summary>
    public class Heart : MonoBehaviour
    {
        #region private inspector variables
        [Header("References")]
        [SerializeField]
        private Image _backgroundObj;

        [SerializeField]
        private Image _fillObj;

        [SerializeField]
        private Image _outlineObj;

        [Header("Customization")]
        [SerializeField]
        private Sprite _main;

        [SerializeField]
        private Sprite _outlines;

        [SerializeField]
        private Color _bgColor = Color.gray3;

        [SerializeField]
        private Color _fillColor =Color.red; 
        
        /// <summary>
        /// The current color that the heart is currently with all effects applied
        /// </summary>
        public Color CurrentColor
        {
            get
            {
                return _fillObj.color;
            }
        }

        [SerializeField]
        private Color _outlineColor=Color.black;

        [Space]
        [SerializeField]
        [Tooltip("Click to make graphics of heart match inspector values")]
        private bool refreshAppearance;
        #endregion

        private void OnValidate()
        {
            if (refreshAppearance)
            {
                UpdateGraphics();
                refreshAppearance = false;
                Debug.Log("Appearance refreshed!");
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            UpdateGraphics();
        }

        /// <summary>
        /// Sets the visual fill amount for a heart
        /// </summary>
        /// <param name="value"></param>
        public void SetFillAmount(float value)
        {
            _fillObj.fillAmount = value;
        }

        /// <summary>
        /// Resets the fill color of the heart to match with its default fill color
        /// </summary>
        public void ResetFillColor()
        {
            _fillObj.color = _fillColor;
        }

        /// <summary>
        /// Sets the color of the heart to match with the passed in color
        /// </summary>
        /// <param name="color"></param>
        public void SetFillColor(Color color)
        {
            _fillObj.color = color;
        }

        #region private void functions
        /// <summary>
        /// Update the BG, fill, and outline to match with colors and sprites in inspector
        /// </summary>
        private void UpdateGraphics()
        {
            if (_backgroundObj!=null)
            {
                UpdateBgGraphic();
            }

            if (_fillObj!=null)
            {
                UpdateFillGraphic();
            }

            if (_outlineObj!=null)
            {
                UpdateOutlineGraphic();
            }
        }

        /// <summary>
        /// Update the color and sprite of the backgroundObj to match with inspector values
        /// </summary>
        private void UpdateBgGraphic()
        {
            _backgroundObj.sprite = _main;
            _backgroundObj.color = _bgColor;
        }

        /// <summary>
        /// Update the color and sprite of the fillObj to match with inspector values
        /// </summary>
        private void UpdateFillGraphic()
        {
            _fillObj.sprite = _main;
            _fillObj.color = _fillColor;
        }

        /// <summary>
        /// Update the color and sprite of the outlineObj to match with inspector values
        /// </summary>
        private void UpdateOutlineGraphic()
        {
            _outlineObj.sprite = _outlines;
            _outlineObj.color = _outlineColor;
        }
        #endregion

    }


}
