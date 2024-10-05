#region Includes

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

namespace Prg.Ui.PageCarousel
{
    /// <summary>
    /// This class represents a single dot indicator used for navigation in a paginated view.
    /// It provides properties for its active state and index, and events for state changes and presses.
    /// </summary>
    public class PageDot : MonoBehaviour
    {
        #region Variables

        /// <summary>
        /// Determines whether it should reflect 'dot' state changes to Image or Button component.
        /// </summary>
        [Header("Configuration")]
        [Tooltip("Determines whether it should change the Image component color on 'dot' state changes")]
        [SerializeField] private bool _useImageComponent;

        [Tooltip("Determines whether it should change the Button component interactable state on 'dot' state changes")]
        [SerializeField] private bool _useButtonComponent;

        /// <summary>
        /// Specifies the default color used when the page dot is unselected.
        /// </summary>
        [Tooltip("Specifies the default color used when the page dot is unselected")]
        [SerializeField] private Color _defaultColor;

        /// <summary>
        /// Specifies the default color used when the page dot is selected.
        /// </summary>
        [Tooltip("Specifies the default color used when the page dot is selected")]
        [SerializeField] private Color _selectedColor;

        /// <summary>
        /// UnityEvent with a boolean parameter that is invoked when the active state of the dot changes.
        /// The parameter is True if the dot becomes active, False if it becomes inactive.
        /// </summary>
        [Header("Events")]
        [Tooltip("Invoked when the active state of the dot changes: True if active, False if inactive")]
        public UnityEvent<bool> OnActiveStateChanged;

        /// <summary>
        /// UnityEvent with an integer parameter that is invoked when the dot is pressed.
        /// The parameter represents the index of the pressed dot.
        /// </summary>
        [Tooltip("Invoked when the dot is pressed with it's index")]
        public UnityEvent<int> OnPressed;

        /// <summary>
        /// Gets the active state of the page dot.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets or sets the index of the page dot within the paginated view.
        /// </summary>
        public int Index { get; set; }

        private Image _image;
        private Button _button;
        private bool _hasImage;
        private bool _hasButton;

        #endregion

        private void Awake()
        {
            if (_useImageComponent && !TryGetComponent(out _image))
            {
                UnityEngine.Debug.LogError("No Image Component found");
            }
            _hasImage = _useImageComponent && _image != null;
            if (_useButtonComponent && !TryGetComponent(out _button))
            {
                UnityEngine.Debug.LogError("No Button Component found");
            }
            _hasButton = _useButtonComponent && _button != null;
            if (_hasButton)
            {
                var pageSlider = GetComponentsInParent<PageSlider>(includeInactive: true)[0];
                var pageScroller = pageSlider.GetComponentsInChildren<PageScroller>(includeInactive: true)[0];
                var buttonTransform = _button.transform;
                _button.onClick.AddListener(() => { pageScroller.SetPage(buttonTransform.GetSiblingIndex()); });
            }
        }

        private void Start()
        {
            // HACK: Ideally the dot shouldn't change it's state.
            // But the second dot was always active and I don't know why.
            // So I'm forcing the dot to update it's state on Start.
            ChangeActiveState(IsActive);
        }

        /// <summary>
        /// Changes the active state of the page dot and invokes the OnActiveStateChanged event.
        /// </summary>
        /// <param name="active">True to set the dot active, False to set it inactive.</param>
        public virtual void ChangeActiveState(bool active)
        {
            IsActive = active;

            if (_hasImage)
            {
                _image.color = active ? _selectedColor : _defaultColor;
            }
            if (_hasButton)
            {
                _button.interactable = !active;
            }

            OnActiveStateChanged?.Invoke(active);
        }

        /// <summary>
        /// Invokes the OnPressed event with the dot's index when the dot is pressed.
        /// </summary>
        public void Press()
        {
            OnPressed?.Invoke(Index);
        }
    }
}
