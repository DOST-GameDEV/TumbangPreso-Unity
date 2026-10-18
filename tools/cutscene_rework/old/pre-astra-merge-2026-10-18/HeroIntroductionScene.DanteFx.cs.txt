using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // BASILIO'S CUTSCENE IN THE EFFECTS KIT (`UltimateFx`, `Shaders/UltimateErode`), 2026-10-10.
        //
        // The owner pointed at a feed of effects and said "see if you can pull off any of those", then, once the kit had
        // been proven on Ilyas's crash, "give me the next effects". What that kit does that nothing else here does: a
        // shape is a ribbon or a disc with a mask run along it, and it leaves by being EATEN, with a burning line where it
        // tears. That is how smoke, ash and a scorch behave, so those are what it is used for here, in his language
        // (basalt, magma, weight):
        //   * THE STOMP: the dust on the court is drawn IN to the fist for a tenth of a second before it lands (the beat of
        //     compression); on the blow a disc of molten light races out and is eaten from its middle, and what it leaves
        //     is a jagged black scorch with fissures still alight in it; curled smoke with a burning edge is thrown up;
        //     embers fly on arcs.
        //   * THE PLATE BREAKING: a burst of rock dust as it lets go; every falling piece drags a ribbon of smoke and
        //     ember behind it and carries a light, so it is not a dark slab over a dark court; a crown of the sea is
        //     thrown up where each goes in.
        //   * HEAT: slow torn wisps of it climbing off the longhorn's back and off the open seams of the sea.
        // ⚠️ NOT THE COMIC POP. `UltimateFx.Pop` is the kit's flat burst with black shards; the owner refused it for Ilyas
        // and said to "save it for later". It is not used here.
        // ⚠️ NO WHITE. His hottest colour is a yellow well under white, so a frame full of it is not a white frame.
        // A machine that cannot draw the shader (`UltimateFx.Ready` false) plays the cutscene without any of this.
        // =========================================================================================
        private struct DnThrown { public string Shape; public int Look; public float Turn, Lean, Long, Late, Life; }

        // The smoke the stomp throws up: which shape, which way round the fist, how far it leans, how long, how late.
        private static readonly DnThrown[] DnStompSmoke =
        {
            new DnThrown { Shape = "hook", Look = UltimateFx.Smoke, Turn = 205f, Lean = -6f, Long = 4.6f, Late = 0f, Life = .9f },
            new DnThrown { Shape = "arc", Look = UltimateFx.Smoke, Turn = 300f, Lean = 10f, Long = 3.8f, Late = .02f, Life = .8f },
            new DnThrown { Shape = "whip", Look = UltimateFx.Ink, Turn = 130f, Lean = 14f, Long = 4.2f, Late = .01f, Life = .8f },
            new DnThrown { Shape = "hook", Look = UltimateFx.Smoke, Turn = 40f, Lean = -4f, Long = 3.4f, Late = .03f, Life = .9f },
        };
        // The rock dust the plate throws as it breaks.
        // ⚠️ SIX SMALL LOW ONES, NOT FOUR TALL ONES. The first four were pale, smooth and as tall as he is, and read as
        // leaves standing round him (film p8). These are ash, lean well over so they run out along the back instead of
        // standing on it, are half as high as they are long, and are already torn on their first frame. They lean only a
        // little: leant 30 to 44 degrees (films p9, p10) their far ends went down into its back and nothing of them showed.
        private static readonly DnThrown[] DnBreakDust =
        {
            new DnThrown { Shape = "whip", Look = UltimateFx.Smoke, Turn = 35f, Lean = 12f, Long = 2.1f, Late = 0f, Life = .6f },
            new DnThrown { Shape = "arc", Look = UltimateFx.Smoke, Turn = 95f, Lean = 16f, Long = 1.7f, Late = .03f, Life = .55f },
            new DnThrown { Shape = "whip", Look = UltimateFx.Smoke, Turn = 160f, Lean = 10f, Long = 2.3f, Late = .01f, Life = .65f },
            new DnThrown { Shape = "arc", Look = UltimateFx.Smoke, Turn = 215f, Lean = 16f, Long = 1.6f, Late = .05f, Life = .55f },
            new DnThrown { Shape = "whip", Look = UltimateFx.Smoke, Turn = 275f, Lean = 12f, Long = 2.0f, Late = .02f, Life = .6f },
            new DnThrown { Shape = "arc", Look = UltimateFx.Smoke, Turn = 330f, Lean = 14f, Long = 1.8f, Late = .04f, Life = .6f },
        };
        // The stomp's embers: which way each flies (degrees round the fist), how fast out and how fast up.
        private static readonly Vector3[] DnStompEmber =
        {
            new Vector3(15f, 5.5f, 9f), new Vector3(80f, 7f, 7.5f), new Vector3(150f, 6f, 10f), new Vector3(215f, 8f, 6.5f), new Vector3(270f, 5f, 9.5f), new Vector3(330f, 7.5f, 8f),
        };

        private bool _dnFxKit;
        private UltimateFx.Piece _dnFxScorch, _dnFxMolten, _dnFxDrawnIn;
        private readonly UltimateFx.Piece[] _dnFxStompSmoke = new UltimateFx.Piece[DnStompSmoke.Length], _dnFxStompEmber = new UltimateFx.Piece[DnStompEmber.Length],
                                            _dnFxBreakDust = new UltimateFx.Piece[DnBreakDust.Length], _dnFxTrail = new UltimateFx.Piece[DnBreakSimulated],
                                            _dnFxCrown = new UltimateFx.Piece[DnBreakSimulated], _dnFxBackHeat = new UltimateFx.Piece[5], _dnFxSeaHeat = new UltimateFx.Piece[4];
        private readonly int[] _dnFxPieceLight = new int[DnBreakSimulated];

        private void BuildDanteFx()
        {
            for (int i = 0; i < _dnFxPieceLight.Length; i++)
                _dnFxPieceLight[i] = AddGlow("PlatePieceLight" + i, new Color(1f, .5f, .1f, 1), falloff: 1.7f, core: .25f, lift: .2f);
            _dnFxKit = UltimateFx.Ready;
            if (!_dnFxKit) return;
            Color soot = new Color(.06f, .05f, .045f, 1f), ash = new Color(.24f, .2f, .18f, 1f), dust = new Color(.55f, .47f, .38f, 1f), pale = new Color(.74f, .66f, .56f, 1f);
            Color magma = new Color(.8f, .22f, .03f, 1f), orange = new Color(1f, .48f, .08f, 1f), hot = new Color(1f, .8f, .3f, 1f);
            // (Film p9 had this ash at .3, .24, .2 and it was lost against the court; film p8 had pale dust and it read as leaves.)
            Color ashBrown = new Color(.5f, .41f, .33f, 1f), ashWarm = new Color(.42f, .34f, .27f, 1f), lit = new Color(.74f, .46f, .24f, 1f);
            var parent = _root.transform;

            // The scorch: a black burn that stays, torn into jagged islands, every torn edge a fissure still alight.
            _dnFxScorch = UltimateFx.Disc(parent, soot, magma, UltimateFx.Ink, 3).With("_Hot", orange).With("_Glow", 1.3f).With("_Radial", .35f)
                .With("_Scale", new Vector4(2.4f, 5f, 0, 0)).With("_Scroll", new Vector4(0, 0, 0, 0)).With("_Band", .14f).With("_Rim", .07f);
            _dnFxScorch.Seed = 3.1f;
            // The molten light that races out over it and is eaten from the middle along its own streaks.
            _dnFxMolten = UltimateFx.Disc(parent, orange, hot, UltimateFx.Streak, 5).With("_Hot", hot).With("_Glow", .7f).With("_Emit", .9f)
                .With("_Scale", new Vector4(1.1f, 8f, 0, 0)).With("_Spread", 1.2f).With("_Band", .3f);
            // The dust drawn in before the blow.
            _dnFxDrawnIn = UltimateFx.Disc(parent, dust, pale, UltimateFx.Smoke, 2).With("_Hot", pale).With("_Glow", .1f)
                .With("_Scale", new Vector4(1.6f, 6f, 0, 0)).With("_Scroll", new Vector4(-3.2f, 0, 0, 0)).With("_Spread", .8f);
            for (int i = 0; i < DnStompSmoke.Length; i++)
            {
                // Smoke and ash: a dark body with a burning edge, eaten from its foot as it climbs.
                _dnFxStompSmoke[i] = UltimateFx.Wisp(parent, DnStompSmoke[i].Shape, i % 2 == 0 ? soot : ash, orange, DnStompSmoke[i].Look, 8)
                    .With("_Hot", hot).With("_Glow", .9f).With("_Scale", new Vector4(1.8f, 1.5f, 0, 0)).With("_Scroll", new Vector4(2.4f, .2f, 0, 0))
                    .With("_Sweep", 1.1f).With("_Shape", .7f).With("_Band", .22f);
                _dnFxStompSmoke[i].Seed = 2 + i * 1.9f;
            }
            for (int i = 0; i < DnStompEmber.Length; i++)
            {
                _dnFxStompEmber[i] = UltimateFx.Wisp(parent, "streak", orange, hot, UltimateFx.Streak, 9).With("_Hot", hot).With("_Emit", 1f).With("_Glow", .6f).With("_Sweep", 1.4f);
                _dnFxStompEmber[i].Seed = 11 + i;
            }
            for (int i = 0; i < DnBreakDust.Length; i++)
            {
                // Ash, grey-brown, its torn edges lit orange by what is under it.
                _dnFxBreakDust[i] = UltimateFx.Wisp(parent, DnBreakDust[i].Shape, i % 2 == 0 ? ashBrown : ashWarm, lit, DnBreakDust[i].Look, 7)
                    .With("_Hot", orange).With("_Glow", .35f).With("_Scale", new Vector4(3.4f, 2.4f, 0, 0)).With("_Scroll", new Vector4(3.2f, .3f, 0, 0))
                    .With("_Sweep", 1.2f).With("_Shape", .8f).With("_Band", .2f).With("_Warp", 1.1f);
                _dnFxBreakDust[i].Seed = 21 + i * 1.3f;
            }
            for (int i = 0; i < DnBreakSimulated; i++)
            {
                // What a falling piece drags behind it: smoke off its hot faces, alight where it tears.
                _dnFxTrail[i] = UltimateFx.Wisp(parent, "s", ash, orange, UltimateFx.Smoke, 7).With("_Hot", hot).With("_Glow", 1.0f)
                    .With("_Scale", new Vector4(2.2f, 1.4f, 0, 0)).With("_Scroll", new Vector4(3.0f, 0, 0, 0)).With("_Sweep", .8f).With("_Shape", .6f).With("_Band", .26f);
                _dnFxTrail[i].Seed = 31 + i * 2.3f;
                // Where it goes into the sea: a crown of it thrown up.
                // ⚠️ DEEP ORANGE, LOW AND TORN. The first was yellow, smooth and as tall as he is, and beside him it read as a
                // plant and was the brightest thing in the frame (film p8). Its body is the sea's deep orange now, the hot
                // yellow is only the thin line where it tears, and it is streaks and drips from its first frame.
                _dnFxCrown[i] = UltimateFx.Wisp(parent, "crown", magma, orange, UltimateFx.Streak, 8).With("_Hot", hot).With("_Emit", .55f).With("_Glow", .5f)
                    .With("_Scale", new Vector4(2.4f, 3.2f, 0, 0)).With("_Scroll", new Vector4(3.0f, .2f, 0, 0)).With("_Sweep", .9f).With("_Shape", .8f).With("_Band", .14f)
                    .With("_Rim", .03f);
                _dnFxCrown[i].Seed = 41 + i * 1.7f;
            }
            for (int i = 0; i < _dnFxBackHeat.Length; i++)
            {
                _dnFxBackHeat[i] = UltimateFx.Wisp(parent, i % 2 == 0 ? "s" : "spiral", magma, orange, UltimateFx.Smoke, 4).With("_Hot", hot).With("_Emit", .7f).With("_Glow", .5f)
                    .With("_Scale", new Vector4(1.6f, 1.2f, 0, 0)).With("_Scroll", new Vector4(.7f, 0, 0, 0)).With("_Sweep", .7f).With("_Shape", .8f);
                _dnFxBackHeat[i].Seed = 51 + i * 2.9f;
            }
            for (int i = 0; i < _dnFxSeaHeat.Length; i++)
            {
                _dnFxSeaHeat[i] = UltimateFx.Wisp(parent, "s", magma, orange, UltimateFx.Smoke, 4).With("_Hot", hot).With("_Emit", .7f).With("_Glow", .5f)
                    .With("_Scale", new Vector4(1.8f, 1.2f, 0, 0)).With("_Scroll", new Vector4(.6f, 0, 0, 0)).With("_Sweep", .7f).With("_Shape", .8f);
                _dnFxSeaHeat[i].Seed = 61 + i * 3.7f;
            }
        }

        private void DanteThrown(UltimateFx.Piece piece, DnThrown typed, Vector3 at, float since, float size, float tornFrom = .2f, float high = 1f)
        {
            // Each grows out along its own way, quickest at first, and is eaten from its root: it leaves the place.
            float age = since - typed.Late, u = Mathf.Clamp01(age / typed.Life);
            float grown = typed.Long * size * (.3f + .7f * (1 - Mathf.Pow(1 - Mathf.Clamp01(u / .5f), 2.6f)));
            var turn = Quaternion.Euler(0, typed.Turn, 0) * Quaternion.Euler(typed.Lean, 0, 0);
            UltimateFx.Place(piece, at, turn, new Vector3(grown * .8f, grown * high * (1 - .2f * Ease(.5f, 1f, u)), grown * (.7f + .3f * u)));
            UltimateFx.Set(piece, age, Mathf.Lerp(tornFrom, 1f, Ease(.18f, 1f, u)));
        }

        private void SampleDanteFx(float t, float lift, float open, bool broken, float light)
        {
            // ------------------------------------------------------------- the falling pieces' own light
            float since = t - DnCrumble;
            float frame = Mathf.Clamp(since * DnBreakRate, 0, DnBreakFrames - 2);
            int row = (int)frame; float part = frame - row;
            for (int i = 0; i < DnBreakSimulated; i++)
            {
                int k = (i * DnBreakFrames + row) * 9;
                Vector3 from = new Vector3(DnBreakPath[k], DnBreakPath[k + 1], DnBreakPath[k + 2]), to = new Vector3(DnBreakPath[k + 9], DnBreakPath[k + 10], DnBreakPath[k + 11]);
                Vector3 at = Vector3.Lerp(from, to, part), going = (to - from) * DnBreakRate;
                bool flying = since >= 0 && at.y > -.1f && since * DnBreakRate < DnBreakFrames - 2;
                // A light on each, so a dark slab over a dark court has an outline.
                PlaceGlow(_dnFxPieceLight[i], at, Vector3.one * 2.4f, Quaternion.identity, flying ? Mathf.Clamp01(since / .12f) * .42f * light : 0);
                if (!_dnFxKit) continue;
                // Its trail lies back along the way it came, longer the faster it goes, and is eaten as it falls.
                float speed = going.magnitude, lasted = Mathf.Clamp01(since / 1.3f);
                var back = speed > .2f ? Quaternion.FromToRotation(Vector3.up, -going / speed) : Quaternion.identity;
                UltimateFx.Place(_dnFxTrail[i], at, back * Quaternion.Euler(0, i * 50f, 0), new Vector3(1.1f, Mathf.Clamp(speed * .32f, .5f, 1.9f), 1.1f));
                UltimateFx.Set(_dnFxTrail[i], flying && since > .06f ? since : -1f, Mathf.Lerp(.28f, .8f, lasted));
                // And the crown where it goes in: up in a tenth of a second, a hang, then it tears from its foot.
                int landed = DnBreakLanded[i];
                float age = landed < 0 ? -1 : since - landed / DnBreakRate, u = Mathf.Clamp01(age / .6f);
                int spot = (i * DnBreakFrames + Mathf.Max(0, landed)) * 9;
                float up = (1 - Mathf.Pow(1 - Mathf.Clamp01(u / .22f), 3f)) * (1 - .3f * Ease(.4f, 1f, u));
                // Never in front of him: a piece that goes in close to the longhorn has its crown a metre further out.
                var into = new Vector3(DnBreakPath[spot], 0, DnBreakPath[spot + 2]);
                if (into.magnitude < 3.2f) into = into.normalized * (into.magnitude + 1.0f);
                float small = i >= 5 ? .6f : 1f;
                UltimateFx.Place(_dnFxCrown[i], into + Vector3.up * .12f, Quaternion.Euler(0, i * 47f, 0),
                                 new Vector3((.56f + .56f * u) * small, Mathf.Max(.05f, 1.36f * up * small), (.56f + .56f * u) * small));
                UltimateFx.Set(_dnFxCrown[i], age, Mathf.Lerp(.34f, 1f, Ease(.12f, 1f, u)));
            }
            if (!_dnFxKit) return;

            // ------------------------------------------------------------- the plate lets go: rock dust
            for (int i = 0; i < DnBreakDust.Length; i++)
                DanteThrown(_dnFxBreakDust[i], DnBreakDust[i], new Vector3(0, LiftAt(DnCrumble) + .1f, .1f), since, 1.25f, .3f, .5f);

            // ------------------------------------------------------------- heat
            float awake = Ease(DnRiseFrom + .5f, DnRisen, t);
            for (int i = 0; i < _dnFxBackHeat.Length; i++)
            {
                // Off its back and shoulders: each climbs on its own slow clock and is eaten as it goes.
                float life = Mathf.Repeat(t * (.3f + .04f * i) + i * .23f, 1f);
                var foot = new Vector3(-2.7f + i * 1.35f, lift - .1f, -.5f + (i % 2) * .9f);
                UltimateFx.Place(_dnFxBackHeat[i], foot + Vector3.up * (life * 1.2f), Quaternion.Euler(0, i * 70f + t * 14f, 0), new Vector3(1.1f, 1.5f + life * .8f, 1.1f));
                UltimateFx.Set(_dnFxBackHeat[i], awake > .02f ? t : -1f, Mathf.Lerp(.5f, 1f, life), awake * Mathf.Sin(life * Mathf.PI) * .55f);
            }
            for (int i = 0; i < _dnFxSeaHeat.Length; i++)
            {
                // Off the open seams of the sea, near the middle where it is hottest.
                int crack = i == 0 ? 0 : i == 1 ? 2 : i == 2 ? 3 : 5;
                var seam = DanteSeam(crack, .3f) + DanteSeamDrift(crack, open);
                float life = Mathf.Repeat(t * (.26f + .05f * i) + i * .31f, 1f), wide = Mathf.Clamp(open * 1.2f, .3f, 1.2f);
                UltimateFx.Place(_dnFxSeaHeat[i], new Vector3(seam.x, .2f + life * .9f, seam.y), Quaternion.Euler(0, i * 95f + t * 10f, 0), new Vector3(wide, 1.2f + life * .7f, wide));
                UltimateFx.Set(_dnFxSeaHeat[i], broken ? t : -1f, Mathf.Lerp(.5f, 1f, life), Ease(DnBreak, DnBreak + .6f, t) * Mathf.Sin(life * Mathf.PI) * .5f);
            }

            // ------------------------------------------------------------- the stomp
            float landedAt = t - DnStomp;
            // The beat before it: the dust on the court is drawn in under the falling fist.
            {
                float u = Mathf.Clamp01((t - (DnStomp - .14f)) / .14f);
                float wide = Mathf.Lerp(7.5f, 2.2f, u * u);
                UltimateFx.Place(_dnFxDrawnIn, DnStompAt + Vector3.up * .74f, Quaternion.identity, new Vector3(wide, 1, wide));
                UltimateFx.Set(_dnFxDrawnIn, u > 0 && landedAt < 0 ? t : -1f, Mathf.Lerp(.72f, .4f, u), Mathf.Sin(u * Mathf.PI * .9f) * .8f);
            }
            {
                // The light races out; the scorch is what it leaves.
                float u = Mathf.Clamp01(landedAt / .3f), reach = Mathf.Lerp(1.5f, 8.5f, 1 - (1 - u) * (1 - u));
                UltimateFx.Place(_dnFxMolten, DnStompAt + Vector3.up * .76f, Quaternion.identity, new Vector3(reach, 1, reach));
                UltimateFx.Set(_dnFxMolten, landedAt, Mathf.Lerp(.12f, 1f, Ease(.1f, 1f, u)), light);
                float burnt = Mathf.Lerp(1.2f, 4.6f, 1 - (1 - u) * (1 - u));
                UltimateFx.Place(_dnFxScorch, DnStompAt + Vector3.up * .72f, Quaternion.Euler(0, 25, 0), new Vector3(burnt, 1, burnt));
                UltimateFx.Set(_dnFxScorch, landedAt, .42f);
            }
            for (int i = 0; i < DnStompSmoke.Length; i++)
                DanteThrown(_dnFxStompSmoke[i], DnStompSmoke[i], DnStompAt + Vector3.up * .2f, landedAt, 1f);
            for (int i = 0; i < DnStompEmber.Length; i++)
            {
                // On arcs: each a short streak lying along the way it is going.
                float angle = DnStompEmber[i].x * Mathf.Deg2Rad, away = DnStompEmber[i].y, up = DnStompEmber[i].z;
                var way = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                var at = DnStompAt + way * (.8f + away * landedAt) + Vector3.up * (.3f + up * landedAt - 9f * landedAt * landedAt);
                var going = way * away + Vector3.up * (up - 18f * landedAt);
                UltimateFx.Place(_dnFxStompEmber[i], at, Quaternion.FromToRotation(Vector3.up, -going.normalized), new Vector3(.22f, Mathf.Clamp(going.magnitude * .09f, .4f, 1.1f), .22f));
                UltimateFx.Set(_dnFxStompEmber[i], landedAt, Mathf.Lerp(.15f, .9f, Mathf.Clamp01(landedAt / .7f)));
            }
        }
    }
}
