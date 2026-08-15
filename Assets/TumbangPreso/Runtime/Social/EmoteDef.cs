using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Social
{
    /// <summary>One emote a player can pick off the wheel.</summary>
    public struct EmoteDef
    {
        /// <summary>Stable id. ⚠️ CROSSES THE WIRE, so never rename one.</summary>
        public string Id;

        /// <summary>Short label, for the wheel segment where space is tight.</summary>
        public string Label;

        /// <summary>Full name, for the centre of the wheel once a segment is selected.</summary>
        public string Name;

        public EmoteDef(string id, string label, string name)
        {
            Id = id;
            Label = label;
            Name = name;
        }
    }
}
