using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The fifteen profile pictures, and which one a player gets.
    ///
    /// ⚠️⚠️ 🧑 ASKED FOR THESE AFTER CROPPING THE FIRST IDENTITY CHIP: **"like tf is that pic
    /// doing there"**, then *"maybe give them an option to pick from a bunch of cute profile
    /// pics"*. The picture on that chip was a square cut out of
    /// `docs/Godot_Character_Select_References/`, which are full captures of the OLD Godot
    /// character screen: a panel, a heading and a model. **It was a picture of a user interface,
    /// on a user interface.**
    ///
    /// ⚠️ THEY ARE DRAWN RATHER THAN CUT OUT AND `tools/build_avatar_art.py` CARRIES THE WHOLE
    /// ARGUMENT. Two passes tried the cut-out. The background knockout is solvable; the FRAMING
    /// is not, because the model stands at a different height and scale in every sheet, and a
    /// picker is twelve things seen TOGETHER. Twelve things that disagree about where the eyes
    /// sit is not a set.
    ///
    /// ⚠️ THE DEFAULT IS DERIVED FROM THE NAME AND NEVER RANDOM. A random default changes every
    /// time the process starts, so a player who has not chosen would see a different face on
    /// every launch and learn that the chip does not mean anything. `docs/TODO.md` § 96 is about
    /// a door nobody recognised; a door whose picture changes is worse.
    /// </summary>
    public static class Avatars
    {
        /// <summary>
        /// Every avatar id, in picker order.
        ///
        /// ⚠️ THE THREE OBJECTS COME LAST AND THAT IS DELIBERATE. Most people want a face, so the
        /// twelve faces are what a picker opens on; the tsinelas, the lata and the chalk star are
        /// for somebody who does not, and they are the game's own subject rather than a fourth
        /// kind of thing.
        /// </summary>
        public static readonly string[] Ids =
        {
            "avatar_01", "avatar_02", "avatar_03", "avatar_04", "avatar_05", "avatar_06",
            "avatar_07", "avatar_08", "avatar_09", "avatar_10", "avatar_11", "avatar_12",
            "avatar_tsinelas", "avatar_lata", "avatar_star",
        };

        /// <summary>Loads one by id, or the first face if the id is unknown.</summary>
        public static Sprite Get(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var found = Resources.Load<Sprite>($"UI/avatars/{id}");
                if (found != null) return found;
            }

            return Resources.Load<Sprite>($"UI/avatars/{Ids[0]}");
        }

        /// <summary>
        /// The avatar a player has until they pick one.
        ///
        /// ⚠️ IT IS A HASH OF THE NAME, SO IT IS STABLE ACROSS LAUNCHES AND DIFFERENT BETWEEN
        /// PEOPLE. `Player#8226` gets the same face on this machine and on the host's, which
        /// matters the moment the lobby draws four of them side by side: an avatar that disagrees
        /// between two peers is a player identifying the wrong seat.
        ///
        /// ⚠️ AND IT ONLY EVER PICKS FROM THE TWELVE FACES. An object is a CHOICE somebody makes,
        /// and defaulting a player to a slipper reads as the game failing to load a picture.
        /// </summary>
        public static string DefaultFor(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return Ids[0];

            unchecked
            {
                int k = 17;
                foreach (char c in playerName) k = (k * 31) + c;
                return Ids[Mathf.Abs(k) % 12];
            }
        }

        /// <summary>
        /// Puts an avatar on a node, framed the way the mark frames everything else.
        ///
        /// ⚠️ THE FRAME IS THE DEEP-RED STROKE, WHICH IS `Front_End_Design.md` § 1.2'S ONE SIGN
        /// FOR "you can act on this". The identity chip IS pressable (it is the door into the
        /// profile, `docs/TODO.md` § 96), so the stroke is telling the truth. ⚠️ **A picker tile
        /// that is only being displayed must not carry it**, or the sign stops answering the one
        /// question it exists for.
        /// </summary>
        public static Image Frame(Transform parent, string name, string id)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = Get(id);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
