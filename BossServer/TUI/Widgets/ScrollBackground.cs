﻿using System.Linq;
using TerrariaUI.Base;
using TerrariaUI.Base.Style;

namespace TerrariaUI.Widgets
{
    /// <summary>
    /// Widget for scrolling parent's layout by pulling layout background.
    /// </summary>
    public class ScrollBackground : VisualObject
    {
        #region Data

        private Action<ScrollBackground, int> ScrollBackgroundCallback;
        public int BeginOffset { get; protected internal set; }
        public int Limit { get; protected set; }
        public bool AllowToPull { get; set; }
        public bool RememberTouchPosition { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Widget for scrolling parent's layout by pulling layout background.
        /// </summary>
        /// <param name="allowToPull">Ability to pull beyond a border</param>
        /// <param name="rememberTouchPosition">Pulling the same point of layout background during touch session</param>
        public ScrollBackground(bool allowToPull = true, bool rememberTouchPosition = true, bool useMoving = true, Action<ScrollBackground, int> callback = null)
            : base(0, 0, 0, 0, new UIConfiguration() { UseMoving = useMoving, UseEnd = true, UseOutsideTouches = true })
        {
            AllowToPull = allowToPull;
            RememberTouchPosition = rememberTouchPosition;
            ScrollBackgroundCallback = callback;
            Layer = Int32.MinValue;

            SetWidthParentStretch();
            SetHeightParentStretch();
        }

        #endregion
        #region Invoke

        protected override void Invoke(Touch touch)
        {
            if (Parent?.LayoutConfiguration == null)
                throw new Exception("Scroll has no parent or parent doesn't have layout.");
            LayoutConfiguration layout = Parent.LayoutConfiguration;
            int offset = layout.Offset;
            Limit = layout.OffsetLimit;
            bool vertical = layout.Direction == Direction.Up || layout.Direction == Direction.Down;
            bool forward = layout.Direction == Direction.Right || layout.Direction == Direction.Down;
            if (touch.State == TouchState.Begin)
                BeginOffset = offset;
            if (touch.State == TouchState.End || (Configuration.UseMoving && touch.State == TouchState.Moving))
            {
                int newOffset;
                int offsetDelta;
                if (RememberTouchPosition)
                {
                    offsetDelta = vertical
                        ? touch.AbsoluteY - touch.Session.BeginTouch.AbsoluteY
                        : touch.AbsoluteX - touch.Session.BeginTouch.AbsoluteX;
                    newOffset = BeginOffset + (forward ? -offsetDelta : offsetDelta);
                }
                else
                {
                    offsetDelta = vertical
                        ? touch.AbsoluteY - touch.Session.PreviousTouch.AbsoluteY
                        : touch.AbsoluteX - touch.Session.PreviousTouch.AbsoluteX;
                    newOffset = offset + (forward ? -offsetDelta : offsetDelta);
                }
                if (newOffset != offset || touch.State == TouchState.End)
                {
                    var inLayout = Parent.Child.Where(child => child.InLayout);
                    VisualObject first = inLayout.FirstOrDefault();
                    VisualObject last = inLayout.LastOrDefault();
                    if (first == null)
                        return;
                    if (touch.State == TouchState.End || !AllowToPull)
                    {
                        if (newOffset < 0)
                            newOffset = 0;
                        else if (newOffset > Limit)
                            newOffset = Limit;
                    }
                    if (Parent.LayoutConfiguration.Offset != newOffset)
                    {
                        if (ScrollBackgroundCallback is Action<ScrollBackground, int> callback)
                            callback.Invoke(this, newOffset);
                        else
                            Parent.LayoutOffset(newOffset).Update().Apply().Draw();
                    }
                }
            }
        }

        #endregion
    }
}
