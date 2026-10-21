using UnityEngine;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// One screen or popup in the hub's stack.
    ///
    /// ⚠️ A SCREEN IS BUILT WHEN IT IS PUSHED AND DESTROYED WHEN IT IS POPPED, so every visit draws
    /// today's data rather than a copy that went stale behind another screen. The two things that
    /// must survive a round trip (the mode card's choice, the loadout's tab) live in settings.
    /// </summary>
    public abstract class HubScreen : MonoBehaviour
    {
        public TumpHub Hub { get; internal set; }
        public RectTransform Root { get; internal set; }

        /// <summary>A popup keeps the screen under it drawn and dimmed; a screen replaces it.</summary>
        public virtual bool IsPopup => false;

        /// <summary>The top-centre queue plate is drawn over this screen while queued.</summary>
        public virtual bool ShowsQueuePlate => true;

        /// <summary>
        /// This screen wears the owner's hand-made board pieces (`HubBoardSkin`): her stickers for its
        /// kit buttons and plates, her BACK and title plate, and HOME's scene behind a maroon wash.
        /// ⚠️ OPT-IN, SCREEN BY SCREEN (owner, 2026-10-22): a screen turns this on only once its restyle
        /// has been captured and shown to her. Turning it on for every screen at once dressed the PLAY
        /// path and the custom lobby in half-fitting pieces she called broken.
        /// </summary>
        public virtual bool WearsBoard => false;

        /// <summary>How much of the live court shows through: 0 is the full court, 1 is none.</summary>
        public virtual float CourtShade => 0.0f;

        public abstract void Build();
        public virtual bool BuildAcrossFrames=>false;
        public virtual System.Collections.IEnumerator PrepareView(){Build();yield break;}
        public virtual void RevealPreparedView(){ }

        /// <summary>
        /// Where a pad or keyboard's focus lands when this screen opens: the screen's one primary.
        /// ⚠️ `CLAUDE.md` § 6.2's "first press" answered for the device with no pointer. Null keeps
        /// `ScreenFocus`'s own reading-order choice.
        /// </summary>
        public virtual UnityEngine.UI.Selectable FirstFocus => null;

        /// <summary>BACK, Escape, pad B and Android BACK. Return true when handled in place;
        /// false lets the hub pop this screen.</summary>
        public virtual bool Back() => false;

        /// <summary>Called when this screen is on top again after a screen above it closed.</summary>
        public virtual void Resumed() { }

        /// <summary>Called every frame while this screen is on top.</summary>
        public virtual void Tick() { }

        protected void Close() => Hub.Pop(this);
    }
}
