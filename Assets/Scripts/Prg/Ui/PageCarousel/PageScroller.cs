#region Includes

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

#endregion

namespace Prg.Ui.PageCarousel
{
    /// <summary>
    /// The PageScroller class manages scrolling within a PageSlider component. 
    /// It handles user interaction for swiping between pages and snapping to the closest page on release.
    /// </summary>
    /// <remarks>
    /// Set (increase) <c>EventSystem.pixelDragThreshold</c> value if you have buttons etc. that
    /// do not behave well if pixelDragThreshold value is too low.<br />
    /// Increasing it helps the UI to work more intuitively.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [RequireComponent(typeof(ScrollView))]
    public class PageScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        #region Button over ScrollView example event traces

        /*
            Pointer is pressed down over button and dragged to the left, then released.

            (1) pixelDragThreshold = 10 (UNITY default)
            Button looses its pressed down status (subjectively speaking) immediately.

            11:03:03.572 248 Button.OnPointerDown
            11:03:03.685 257 --> PageScroller.OnBeginDrag
            11:03:03.685 257 Button.OnPointerUp
            11:03:03.887 274 --> PageScroller.OnEndDrag
            11:03:04.301 313 Button.OnPointerExit

            (2) pixelDragThreshold = 57 pixels (3% of screen width 1920)
            When pointer movement (in pixels) exceeds EventSystem.pixelDragThreshold Button.OnPointerUp event is sent.
            This happens on frame 773
            Button looses its pressed down status later because the pixelDragThreshold is larger.
            While pointer is being dragged, button receives enter and exit messages but does not react to them.

            10:48:22.735 272 Button.OnPointerEnter
            10:48:24.778 635 Button.OnPointerDown

            10:48:25.550 773 Button.OnPointerExit
            10:48:25.551 773 --> PageScroller.OnBeginDrag
            10:48:25.551 773 Button.OnPointerUp

            10:48:25.577 777 Button.OnPointerEnter

            10:48:25.589 779 Button.OnPointerExit
            10:48:25.595 780 Button.OnPointerEnter

            10:48:25.670 789 Button.OnPointerExit
            10:48:25.677 790 Button.OnPointerEnter

            ...

            10:48:26.421 915 Button.OnPointerExit
            10:48:26.427 916 Button.OnPointerEnter

            10:48:26.488 927 Button.OnPointerExit
            10:48:26.494 928 Button.OnPointerEnter

            10:48:26.614 949 Button.OnPointerExit

            10:48:26.621 950 Button.OnPointerEnter
            10:48:26.728 969 --> PageScroller.OnEndDrag
            10:48:26.746 972 Button.OnPointerExit
         */

        #endregion

        #region Variables

        /// <summary>
        /// Minimum delta drag required to consider a page change (normalized value between 0 and 1).
        /// </summary>
        [Header("Configuration")]
        [Tooltip("Minimum delta drag required to consider a page change (normalized value between 0 and 1)")]
        [SerializeField] private float _minDeltaDrag = 0.1f;

        /// <summary>
        /// Duration (in seconds) for the page snapping animation.
        /// </summary>
        [Tooltip("Duration (in seconds) for the page snapping animation")]
        [SerializeField] private float _snapDuration = 0.3f;

        /// <summary>
        /// Teleport immediately or use animation for SetPage() transition.
        /// </summary>
        [Tooltip("Teleport immediately to new page instead of using animation when SetPage() is called")]
        [SerializeField] private bool _teleportOnSetPage;

        /// <summary>
        /// Event triggered when a page change starts. 
        /// The event arguments are the index of the current page and the index of the target page.
        /// </summary>
        [Header("Events")]
        [Tooltip("Event triggered when a page change starts: index current page, index of target page")]
        public UnityEvent<int, int> OnPageChangeStarted;

        /// <summary>
        /// Event triggered when a page change ends. 
        /// The event arguments are the index of the current page and the index of the new active page.
        /// </summary>
        [Tooltip("Event triggered when a page change ends: index of the current page, index of the new active page")]
        public UnityEvent<int, int> OnPageChangeEnded;

        /// <summary>
        /// Gets the rectangle of the ScrollRect component used for scrolling.
        /// </summary>
        public Rect Rect
        {
            get
            {
#if UNITY_EDITOR
                if (_scrollRect == null)
                {
                    _scrollRect = FindScrollRect();
                }
#endif
                return ((RectTransform)_scrollRect.transform).rect;
            }
        }

        /// <summary>
        /// Gets the RectTransform of the content being scrolled within the ScrollRect.
        /// </summary>
        public RectTransform Content
        {
            get
            {
#if UNITY_EDITOR
                if (_scrollRect == null)
                {
                    _scrollRect = FindScrollRect();
                }
#endif
                return _scrollRect.content;
            }
        }

        private ScrollRect _scrollRect;

        [Header("Live Data")]
        [SerializeField] private int _currentPage; // Index of the currently active page.

        [SerializeField] private int _targetPage; // Index of the target page during a page change animation.

        [SerializeField]
        private float _startNormalizedPosition; // Normalized position of the scroll bar when drag begins.

        [SerializeField]
        private float _targetNormalizedPosition; // Normalized position of the scroll bar for the target page.

        [SerializeField] private float _moveSpeed; // Speed of the scroll bar animation (normalized units per second).

        private Coroutine _moveCoroutine;

        #endregion

        private void Awake()
        {
            _scrollRect = FindScrollRect();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// Moves <c>scrollRect</c> to the left or right depending of <c>moveSpeed</c> sign (+/-)
        /// until it reaches its target position.
        /// </summary>
        [SuppressMessage("ReSharper", "TooWideLocalVariableScope")]
        private IEnumerator MoveCoroutine()
        {
            while (_moveSpeed != 0)
            {
                // Get the current normalized position of the scroll rect (between 0 and 1).
                // Update the current position based on the move speed and deltaTime.
                var position = _scrollRect.horizontalNormalizedPosition;
                position += _moveSpeed * Time.deltaTime;

                // Determine the minimum and maximum allowed positions based on the move direction:
                //  - If moving forward (positive moveSpeed): current position is the minimum, target position is the maximum.
                //  - If moving backward (negative moveSpeed): current position is the maximum, target position is the minimum.
                // Clamp the current position to stay within the valid range (between min and max).
                position = _moveSpeed > 0
                    ? Mathf.Clamp(position, position, _targetNormalizedPosition)
                    : Mathf.Clamp(position, _targetNormalizedPosition, position);

                // Update the actual position of the scroll rect in the ScrollRect component.
                _scrollRect.horizontalNormalizedPosition = position;

                // Check if the scroll rect has reached the target position (within a small tolerance using Mathf.Epsilon).
                if (Mathf.Abs(_targetNormalizedPosition - position) < Mathf.Epsilon)
                {
                    // Stop the movement by setting moveSpeed to 0.
                    _moveSpeed = 0;

                    // Invoke the OnPageChangeEnded event to signal the completion of the page change animation.
                    // The event arguments are the index of the previous page and the index of the new active page.
                    OnPageChangeEnded?.Invoke(_currentPage, _targetPage);

                    // Update the _currentPage variable to reflect the new active page.
                    _currentPage = _targetPage;
                }
                yield return null;
            }
            _moveCoroutine = null;
        }

        public void SetPage(int index, bool forceTeleport = false)
        {
            if (forceTeleport || _teleportOnSetPage)
            {
                _scrollRect.horizontalNormalizedPosition = GetTargetPagePosition(index);

                var previousPage = _currentPage;
                _targetPage = index;
                _currentPage = index;
                OnPageChangeEnded?.Invoke(previousPage, _currentPage);
                return;
            }
            // Calculate simulated drag direction and amount for the animation.
            float dir = index - _currentPage;
            if (dir < 0 && _currentPage == 0)
            {
                // No can do.
                return;
            }
            // Mark 'drag' start point.
            _targetPage = index;
            OnBeginDrag(null);

            // Move scrollRect slightly to correct direction so that animation can play correctly.
            const float stepAmount = 0.01f;
            var pageWidth = 1f / GetPageCount();
            var dragStep = dir * stepAmount * pageWidth;
            _scrollRect.horizontalNormalizedPosition += dragStep;

            // Start the animation immediately.
            OnEndDrag(null);
        }

        public void OnBeginDrag(PointerEventData _)
        {
            // Store the starting normalized position of the scroll bar.
            _startNormalizedPosition = _scrollRect.horizontalNormalizedPosition;

            // Check if the target page is different from the current page.
            if (_targetPage != _currentPage)
            {
                // If they are different, it means we were potentially in the middle of an animation
                // for a previous page change that got interrupted by this drag. 
                // Therefore, signal the end of the previous page change animation (if any)
                // by invoking the OnPageChangeEnded event.
                // The event arguments are the index of the previous page (_currentPage) 
                // and the index of the target page (_targetPage).
                OnPageChangeEnded?.Invoke(_currentPage, _targetPage);

                // Update the _currentPage variable to reflect the target page,
                // as this is now the intended page after the drag begins.
                _currentPage = _targetPage;
            }

            // Reset the move speed to 0 to stop any ongoing scroll animations.
            // This is necessary because a drag interaction might interrupt an ongoing page change animation.
            _moveSpeed = 0;
        }

        public void OnEndDrag(PointerEventData _)
        {
            // Calculate the width of a single page (normalized value between 0 and 1).
            // This is achieved by dividing 1 by the total number of pages.
            var pageWidth = 1f / GetPageCount();

            // Calculate the normalized position of the current page.
            // When snapping to a page, this position should ideally match the starting normalized position.
            var pagePosition = _currentPage * pageWidth;

            // Get the current normalized position of the scroll rect.
            var currentPosition = _scrollRect.horizontalNormalizedPosition;

            // Determine the minimum amount of drag required (normalized value) to consider a page change.
            // This is calculated by multiplying the page width by the _minDeltaDrag value.
            var minPageDrag = pageWidth * _minDeltaDrag;

            // Check if the drag direction is forward or backward.
            // This is determined by comparing the current position with the starting position.
            // A higher current position indicates a forward drag.
            var isForwardDrag = _scrollRect.horizontalNormalizedPosition > _startNormalizedPosition;

            // Calculate the normalized position where a page change should occur (switchPageBreakpoint).
            // This is calculated by adding (for forward drag) or subtracting (for backward drag) 
            // the minimum page drag distance from the current page position.
            var switchPageBreakpoint = pagePosition + (isForwardDrag ? minPageDrag : -minPageDrag);

            // Determine if a page change should occur based on the current position and the switchPageBreakpoint.
            // If it's a forward drag and the current position is greater than the switchPageBreakpoint, 
            // it means the user has dragged enough to switch to the next page.
            // Similarly, for a backward drag, if the current position is less than the switchPageBreakpoint, 
            // a page change to the previous page is triggered.
            var page = _currentPage;
            if (isForwardDrag && currentPosition > switchPageBreakpoint)
            {
                page++;
            }
            else if (!isForwardDrag && currentPosition < switchPageBreakpoint)
            {
                page--;
            }

            // Call the ScrollToPage function to initiate the page change animation for the determined page.
            ScrollToPage(page);
        }

        /// <summary>
        /// This function handles initiating a page change animation based on a target page index 
        /// during a scroll interaction. It calculates the target scroll position, determines if a page change 
        /// is required based on drag distance and direction, and triggers the animation if necessary.
        /// </summary>
        /// <param name="page">The index of the target page to scroll to.</param>
        private void ScrollToPage(int page)
        {
            // Calculate the target normalized position for the scroll rect based on the target page index.
            _targetNormalizedPosition = GetTargetPagePosition(page);

            // Calculate the speed required to reach the target position within the snap duration.
            _moveSpeed = (_targetNormalizedPosition - _scrollRect.horizontalNormalizedPosition) / _snapDuration;

            // Update the target page variable to reflect the new target page.
            _targetPage = page;

            // If the target page is different from the current page, 
            // invoke the OnPageChangeStarted event to signal the beginning of the page change animation.
            if (_targetPage != _currentPage)
            {
                OnPageChangeStarted?.Invoke(_currentPage, _targetPage);
            }
            if (_moveCoroutine == null)
            {
                _moveCoroutine = StartCoroutine(MoveCoroutine());
            }
        }

        /// <summary>
        /// Calculates the number of scrollable pages in the scroll view, considering the content and viewport width.
        /// </summary>
        /// <returns>The number of scrollable pages.</returns>
        private int GetPageCount()
        {
            var contentWidth = _scrollRect.content.rect.width;
            var rectWidth = ((RectTransform)_scrollRect.transform).rect.size.x;
            return Mathf.RoundToInt(contentWidth / rectWidth) - 1;
        }

        private float GetTargetPagePosition(int page)
        {
            return page * (1f / GetPageCount());
        }

        private ScrollRect FindScrollRect()
        {
            var scrollRect = GetComponentInChildren<ScrollRect>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (scrollRect == null)
            {
                UnityEngine.Debug.LogError("Missing ScrollRect in Children");
            }
#endif
            return scrollRect;
        }
    }
}
