using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The drone that carries a fallen body back onto the Arena's stage. With the stage kit's
    /// model (a TEMPLATE the scene builder leaves in the scene, already wearing the map's
    /// materials: parts `drone_body`, `drone_rotor_0..3` and `drone_beam`, its nose along +z) it
    /// is a copy of that; without one it is the grey-box: a flat box with four rotors and a beam
    /// under it, built from primitives.
    ///
    /// ⚠️ IT DECIDES NOTHING. It hovers over a body for as long as that body's replicated
    /// `EdgeKind` is `Drone` and flies off when it is not, so every peer shows the same carry
    /// from the fields `SyncUnit` already brings. The host's `CharacterMotor.StepDroneCarry`
    /// owns where the body goes.
    ///
    /// WHAT IT SHOWS (owner, 2026-10-05: "more vfx overall in the map, including the drone
    /// stuff"; before this it was a model with a still beam), all through `ArenaFx`, all read
    /// off the same replicated phase, so every peer sees it and nothing is sent:
    ///   * LOCK ON: it drops onto the falling body from 9 m up, and a sonar ping rings out from
    ///     the body the moment it has it (two thin rings, a glint, the ping's sound);
    ///   * THE CATCH: a flash, the word RESCUED! in the game's own callout (`ComicPopup`), and
    ///     the thrusters' burst as the haul begins;
    ///   * THE BEAM IS ALIVE: a wide cone of light from belly to body that breathes, rings
    ///     travelling up it, and motes drawn up into it from under the body;
    ///   * ROTOR WASH under it, and RUNNING LIGHTS that blink at its two sides;
    ///   * THE SET-DOWN: a pool of light on the deck under the body as it comes down, then a
    ///     thump, a ring, dust, and a short shake for the player who was carried.
    /// The five seconds the body then stands frozen are the game's own tagged look (the caught
    /// mark `CharacterVisual` draws for `StunElement.None`): the drone ends its carry with
    /// `ApplyTagged`, so nothing is added here for it.
    ///
    /// IN A BREAK the same drones lift every player off the stage while it rebuilds
    /// (`ArenaShow`, which poses them itself through `Hold`): a break runs with
    /// `Time.timeScale` 0, so that path takes its own step.
    /// </summary>
    public sealed class ArenaDrone : MonoBehaviour
    {
        public const float Hover=2.6f,ArriveFrom=9,LeaveSeconds=.7f,LeaveSpeed=11;
        /// <summary>The model's beam is a cone this long, hung from the drone's belly at scale 1.</summary>
        private const float ModelBeam=2.5f;
        /// <summary>Half the drone's width: where its running lights sit (the kit's is 1.62 m across the ducts).</summary>
        private const float Wing=.82f;
        private bool _modelled;
        private readonly Transform[] _rotors=new Transform[4];
        private CharacterMotor _body;
        private Transform _beam;
        private float _leaving,_ringAt,_moteAt,_washAt,_pingAt=-1,_clock;
        private int _phase=-1;
        private bool _carried,_held;

        public static ArenaDrone Build(Transform parent,GameObject template=null)
        {
            if(template!=null)
            {
                var copy=Instantiate(template,parent,false);copy.name="Arena drone";copy.SetActive(false);
                var modelled=copy.GetComponent<ArenaDrone>();if(modelled==null)modelled=copy.AddComponent<ArenaDrone>();
                int found=0;
                foreach(var part in copy.GetComponentsInChildren<Transform>(true))
                {
                    if(part.name.StartsWith("drone_rotor",System.StringComparison.Ordinal)&&found<modelled._rotors.Length)modelled._rotors[found++]=part;
                    else if(part.name.StartsWith("drone_beam",System.StringComparison.Ordinal))modelled._beam=part;
                }
                // A template with no beam or with rotors missing is not the kit's drone: the grey-box is drawn.
                if(modelled._beam!=null&&found==modelled._rotors.Length){modelled._modelled=true;return modelled;}
                Destroy(copy);
            }
            var root=new GameObject("Arena drone");root.transform.SetParent(parent,false);
            var drone=root.AddComponent<ArenaDrone>();
            Part(PrimitiveType.Cube,"Body",root.transform,Vector3.zero,new Vector3(1.1f,.26f,1.1f),new Color(.42f,.45f,.50f),false);
            for(int i=0;i<4;i++)
            {
                var at=new Vector3(i%2==0?-.62f:.62f,.16f,i<2?-.62f:.62f);
                drone._rotors[i]=Part(PrimitiveType.Cylinder,"Rotor",root.transform,at,new Vector3(.52f,.02f,.52f),new Color(.20f,.22f,.26f),false);
            }
            // A Unity cylinder is 2 m tall at scale 1: the beam is scaled and hung in LateUpdate.
            drone._beam=Part(PrimitiveType.Cylinder,"Beam",root.transform,Vector3.zero,Vector3.one,new Color(.55f,.95f,1.0f,.35f),true);
            root.SetActive(false);
            return drone;
        }

        private static Transform Part(PrimitiveType shape,string name,Transform parent,Vector3 at,Vector3 scale,Color colour,bool beam)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;
            // Decoration only. A primitive's collider would be a floor for the body's own probes.
            Visual.VfxMaterial.StripCollider(go);
            go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=scale;
            var renderer=go.GetComponent<Renderer>();
            if(beam)Visual.VfxMaterial.Ghost(renderer,colour,.8f);else Visual.VfxMaterial.Solid(renderer,colour);
            renderer.shadowCastingMode=beam?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
            return go.transform;
        }

        public void Attend(CharacterMotor body)
        {
            if(_body==body&&_leaving<=0&&gameObject.activeSelf)return;
            _body=body;_leaving=0;_phase=-1;_carried=false;_held=false;gameObject.SetActive(true);Place();
        }

        /// <summary>
        /// Posed from outside for one frame (the break's lift, `ArenaShow`): the drone at `at`,
        /// turned to `yaw`, its beam reaching `beam` metres down to a body's shoulders (0: no
        /// beam). `dt` is the caller's own step, because a break holds `Time.deltaTime` at 0.
        /// </summary>
        public void Hold(Vector3 at,float yaw,float beam,float dt)
        {
            _body=null;_held=true;_leaving=0;
            if(!gameObject.activeSelf)gameObject.SetActive(true);
            transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
            SetBeam(beam);
            Spin(dt);
            Dress(at+Vector3.down*(beam+1.25f),beam,dt,beam>.05f);
        }

        /// <summary>Let go of a drone posed through `Hold`.</summary>
        public void Release(){_held=false;_body=null;if(gameObject.activeSelf)gameObject.SetActive(false);}

        private bool Carrying=>_body!=null&&_body.gameObject.activeInHierarchy&&_body.EdgeKind==EdgeRecoveryKind.Drone;

        private void LateUpdate()
        {
            if(_held)return;
            float dt=Time.deltaTime;
            if(Carrying){_leaving=0;Place();Carry(dt);}
            else
            {
                // Set down, or the round reset under it: let go and climb away.
                if(_leaving<=0)SetDown();
                _leaving+=dt;_beam.gameObject.SetActive(false);
                transform.position+=Vector3.up*LeaveSpeed*dt;
                Dress(transform.position,0,dt,false);
                if(_leaving>=LeaveSeconds){_body=null;gameObject.SetActive(false);}
            }
            Spin(dt);
        }

        private void Spin(float dt)
        {
            for(int i=0;i<_rotors.Length;i++)if(_rotors[i]!=null)_rotors[i].Rotate(0,(i%2==0?1400:-1400)*dt,0,Space.Self);
        }

        private void Place()
        {
            // It drops onto the falling body over the catch and holds its height after.
            float arrive=_body.EdgePhase==0?Mathf.SmoothStep(0,1,_body.EdgePhaseRatio):1;
            var feet=_body.transform.position;
            transform.SetPositionAndRotation(feet+Vector3.up*(Hover+ArriveFrom*(1-arrive)),Quaternion.Euler(0,_body.transform.eulerAngles.y,0));
            // The beam reaches from the drone's belly to the shoulders, and only once it has the body.
            SetBeam((Hover-1.25f)*arrive);
        }

        private void SetBeam(float length)
        {
            _beam.gameObject.SetActive(length>.05f);
            if(_modelled){_beam.localPosition=Vector3.zero;_beam.localScale=new Vector3(1,length/ModelBeam,1);return;}
            _beam.localPosition=Vector3.down*(.13f+length*.5f);
            _beam.localScale=new Vector3(.55f,length*.5f,.55f);
        }

        // The carry's own moments, read off the replicated phase: the lock-on as the drone
        // appears, the catch as the haul begins, the pool of light as it sets the body down.
        private void Carry(float dt)
        {
            var fx=ArenaFx.Instance;
            var feet=_body.transform.position;
            int phase=_body.EdgePhase;
            if(phase!=_phase)
            {
                if(_phase<0)
                {
                    // Locked on. The second ring follows the first.
                    ArenaFx.Cue("sfx_arena_drone_ping",feet,.97f,1.04f);
                    if(fx!=null){fx.Ring(feet+Vector3.up,.3f,5.5f,ArenaFx.Cyan,.9f,.6f,ArenaFx.Cell.ThinRing);fx.Glint(feet+Vector3.up*1.1f,2.4f,ArenaFx.White,.9f,.4f);}
                    _pingAt=.14f;
                }
                if(phase>=1&&_phase<1)
                {
                    _carried=true;
                    ArenaFx.Cue("sfx_arena_thruster",transform.position,.95f,1.08f);
                    Visual.ComicPopup.Spawn(feet+Vector3.up*.6f,"RESCUED!",ArenaFx.Cyan,1.1f,Visual.ComicPopup.Weight.Cast,_body);
                    if(fx!=null)
                    {
                        fx.Flash(feet+Vector3.up,1.2f,6,ArenaFx.Cyan,.85f,.3f);
                        fx.Ring(feet+Vector3.up*.2f,.4f,3.6f,ArenaFx.White,.8f,.4f);
                        fx.Sparks(transform.position,Vector3.down,35,14,5,12,ArenaFx.Cyan,.9f,.25f,.5f,.16f,4);
                    }
                }
                _phase=phase;
            }
            if(_pingAt>0){_pingAt-=dt;if(_pingAt<=0&&fx!=null)fx.Ring(feet+Vector3.up,.3f,4,ArenaFx.Cyan,.6f,.55f,ArenaFx.Cell.ThinRing);}

            float beam=(Hover-1.25f)*(phase==0?Mathf.SmoothStep(0,1,_body.EdgePhaseRatio):1);
            Dress(feet,beam,dt,beam>.05f);

            // Coming down: a pool of light on the deck under the body, growing as it nears.
            if(phase==2&&fx!=null&&Physics.Raycast(feet+Vector3.up*.5f,Vector3.down,out var deck,9,~0,QueryTriggerInteraction.Ignore)
               &&deck.collider.GetComponentInParent<CharacterMotor>()==null)
            {
                float near=_body.EdgePhaseRatio;
                fx.DrawFlat(ArenaFx.Cell.Disc,deck.point+Vector3.up*.06f,1.6f+1.6f*near,1.6f+1.6f*near,0,ArenaFx.Cyan,.18f+.3f*near);
                fx.DrawFlat(ArenaFx.Cell.ThinRing,deck.point+Vector3.up*.07f,3.6f-1.2f*near,3.6f-1.2f*near,0,ArenaFx.White,.5f*near);
            }
        }

        /// <summary>The drone's standing effects for one frame: its running lights and wash,
        /// and, with a beam, the light from its belly down to `feet` and what rides it.</summary>
        private void Dress(Vector3 feet,float beam,float dt,bool lit)
        {
            var fx=ArenaFx.Instance;if(fx==null)return;
            _clock+=dt;
            var at=transform.position;var right=transform.right;

            // Running lights: one side then the other, and a steady one under the belly.
            bool left=Mathf.Repeat(_clock*1.7f,1)<.22f,other=Mathf.Repeat(_clock*1.7f+.5f,1)<.22f;
            if(left)fx.DrawBillboard(ArenaFx.Cell.Dot,at-right*Wing,.55f,ArenaFx.Magenta,.95f);
            if(other)fx.DrawBillboard(ArenaFx.Cell.Dot,at+right*Wing,.55f,ArenaFx.White,.95f);
            fx.DrawBillboard(ArenaFx.Cell.Dot,at+Vector3.down*.16f,.42f,ArenaFx.Cyan,.6f);

            // Rotor wash: thin streaks blown down from under it.
            _washAt-=dt;
            if(_washAt<=0)
            {
                _washAt=.07f;
                fx.Sparks(at+Vector3.down*.2f,Vector3.down,32,2,4,8,ArenaFx.White,.22f,.2f,.36f,.1f,0,1.5f);
            }
            if(!lit)return;

            // The beam: a cone of light that breathes, wide at the body.
            var belly=at+Vector3.down*.14f;var shoulders=feet+Vector3.up*1.25f;
            float breath=.26f+.07f*Mathf.Sin(_clock*9);
            fx.DrawBeam(belly,shoulders+Vector3.down*1.1f,.5f,1.9f,ArenaFx.Cyan,breath);
            fx.DrawBeam(belly,shoulders+Vector3.down*1.1f,.18f,.7f,ArenaFx.White,breath*.8f);

            // Rings travelling up it from the body's feet.
            _ringAt-=dt;
            if(_ringAt<=0)
            {
                _ringAt=.13f;
                fx.Ring(feet+Vector3.up*.1f,.85f,.4f,ArenaFx.Cyan,.75f,.5f,ArenaFx.Cell.ThinRing,(beam+1.3f)/.5f);
            }

            // Motes drawn up into it from under the body.
            _moteAt-=dt;
            if(_moteAt<=0)
            {
                _moteAt=.045f;
                float a=fx.Rand(0,Mathf.PI*2),r=fx.Rand(.2f,1);
                var from=feet+new Vector3(Mathf.Cos(a)*r,-fx.Rand(.4f,1.8f),Mathf.Sin(a)*r);
                var pull=(shoulders-from)*1.6f;
                fx.Emit(ArenaFx.Cell.Dot,ArenaFx.Mode.Billboard,from,pull,ArenaFx.Cyan,.8f,.55f,.16f,.05f);
            }
        }

        // The carry is over with the body on its feet: the thump, where it stands.
        private void SetDown()
        {
            if(!_carried||_body==null||!_body.gameObject.activeInHierarchy||_body.IsEdgeRecovering){_carried=false;return;}
            _carried=false;
            var feet=_body.transform.position;
            ArenaFx.Cue("sfx_arena_drone_set",feet,.96f,1.04f);
            var fx=ArenaFx.Instance;
            if(fx!=null)
            {
                fx.Ring(feet+Vector3.up*.08f,.4f,4.4f,ArenaFx.Cyan,.85f,.5f);
                fx.Ring(feet+Vector3.up*.08f,.2f,2.2f,ArenaFx.White,.7f,.3f,ArenaFx.Cell.ThinRing);
                fx.Dots(feet+Vector3.up*.1f,Vector3.up,80,12,1.5f,4,ArenaFx.White,.4f,.3f,.6f,.3f,3);
            }
            // Felt by whoever was carried, and only by them.
            var view=Camera.main;
            var rig=view!=null?view.GetComponent<CameraSystem.CameraRig>():null;
            if(rig!=null&&rig.IsFollowing(_body))rig.Shake(.55f,.6f);
        }
    }
}
