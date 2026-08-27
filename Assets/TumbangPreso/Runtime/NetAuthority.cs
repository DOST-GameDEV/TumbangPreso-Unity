using System;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The seam between gameplay and whichever netcode stack this project ends up on.
    ///
    /// ⚠️⚠️ WHY THIS EXISTS AT ALL. The port plan defers the Mirror-versus-NGO decision to
    /// Phase 5, which is correct, but "defer the decision" normally means "write all the
    /// gameplay single-player and rewrite it later". That is the expensive path: authority
    /// checks are not a layer you bolt on, they are a sentence in the middle of every verb
    /// ("only the host decides who that reached"), and retrofitting them means touching every
    /// file again and getting one wrong.
    ///
    /// So the gameplay code asks THIS instead. Today it answers "you are the host, there are
    /// no peers", which is exactly true of a single-player match and lets everything run.
    /// Phase 5 replaces the provider and the call sites do not move.
    ///
    /// ⚠️ THIS DELIBERATELY DOES NOT ABSTRACT RPCs, AND THAT IS THE POINT. A generic
    /// send-message layer over Mirror and NGO would be a worse version of both, and the
    /// original's whole netcode virtue is that its RPCs are hand-written and explicit. What is
    /// abstracted is only the QUESTION every verb has to ask before it acts. The actual
    /// [Command] / [ClientRpc] attributes get written natively, once, against the chosen
    /// stack.
    ///
    /// The shape they will take is already fixed by the original and should be preserved:
    ///
    ///     client asks     ->  RequestX()      sends position, facing, commitment
    ///     host decides    ->  HostResolveX()  re-checks, and is the ONLY place score is written
    ///     host announces  ->  RpcX()          everyone plays the result
    ///
    /// ⚠️ AND THE CLIENT NEVER SENDS A RESULT, ONLY AN INTENT. It says where it stood, which
    /// way it faced and how hard it committed; the host decides who that reached. A client
    /// that could say "I tagged P3" is a client that can award itself 100 points.
    /// </summary>
    public static class NetAuthority
    {
        /// <summary>
        /// True when this peer decides outcomes. Single-player is a host with no peers, which
        /// is why every host-side path runs unchanged offline.
        /// </summary>
        public static bool IsHost => Provider?.IsHost ?? true;

        /// <summary>True once a real transport is running. False in single player.</summary>
        public static bool IsNetworked => Provider?.IsNetworked ?? false;

        /// <summary>This peer's seat, or -1 for a seatless referee such as a dedicated server.</summary>
        public static int LocalSlot => Provider?.LocalSlot ?? 0;

        /// <summary>
        /// This peer's TRANSPORT identity, which is what the host counts and keys its tables by.
        ///
        /// ⚠️⚠️ A SEAT IS NOT A PEER ID AND THE TWO NAMESPACES OVERLAP, which is what makes
        /// mixing them so expensive to find. Seats are 0-3 and always four of them, filled by
        /// bots where nobody is sitting. Peer ids are handed out by the transport, the host is
        /// always 0, and a spectator or a dedicated referee holds one without holding a seat.
        /// A host in seat 1 beside a client whose peer id is 1 is the case that breaks: a set
        /// keyed by "whichever of the two was to hand" collapses two peers into one entry, and
        /// every gate that counts presses then waits forever for a press it already had.
        /// `ReadyGate.DeclareReady`, `RematchVote.Add` and `LobbySession.PlayingPeerCount` all
        /// want THIS, never `LocalSlot`.
        /// </summary>
        public static int LocalPeerId => Provider?.LocalPeerId ?? 0;

        /// <summary>
        /// ⚠️ A DEDICATED SERVER IS A REFEREE WITH NO SEAT, and that is not a corner case for
        /// the supported Linux server build. The Unity game has no active VPS deployment, but
        /// anything that assumes a launched server is also a player still breaks invisibly.
        /// </summary>
        public static bool IsSeatlessReferee => Provider?.IsSeatlessReferee ?? false;

        public static INetProvider Provider { get; set; }

        /// <summary>
        /// The guard every host-authoritative method opens with. Reads as a sentence at the
        /// call site, which is the point: `if (!NetAuthority.ShouldResolve()) return;`
        /// </summary>
        public static bool ShouldResolve() => IsHost;

        /// <summary>
        /// True when this peer should ASK the host rather than resolving locally. The exact
        /// complement of <see cref="ShouldResolve"/> once a transport is live.
        ///
        /// ⚠️⚠️ THE ORIGINAL SHIPPED A BUG THAT THIS SHAPE EXISTS TO PREVENT, and it is worth
        /// stating because it is so easy to reproduce. The punch and the shove both sent a
        /// request when not the host. The lunge, added later, only guarded its sweep with
        /// "if not networked or host" and had NO else branch. On a client that guard is false,
        /// so the sweep never ran there, and it never ran on the host either, because the host
        /// returns at its authority gate before stepping a body it does not own. The verb was
        /// simply dead for three of the four players in every networked match, for weeks.
        ///
        /// Any verb that calls ShouldResolve MUST also handle ShouldRequest. If a verb has one
        /// without the other, it is broken for somebody and probably silently.
        /// </summary>
        public static bool ShouldRequest() => IsNetworked && !IsHost;
    }

    /// <summary>Implemented in Phase 5 by a thin adapter over the chosen stack.</summary>
    public interface INetProvider
    {
        bool IsHost { get; }
        bool IsNetworked { get; }
        int LocalSlot { get; }
        int LocalPeerId { get; }
        bool IsSeatlessReferee { get; }
    }

    /// <summary>
    /// The offline provider, and the reason single player needs no special casing anywhere:
    /// it is simply a host with no peers.
    /// </summary>
    public sealed class SoloProvider : INetProvider
    {
        public bool IsHost => true;
        public bool IsNetworked => false;
        public int LocalSlot => 0;
        public int LocalPeerId => 0;
        public bool IsSeatlessReferee => false;
    }
}
