using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// SAGIP ("rescue"), the drone that carries a fallen body back onto the Arena's stage. With
    /// the stage kit's model (a TEMPLATE the scene builder leaves in the scene, already wearing
    /// the map's materials) it is a copy of that; without one, or with a template that is not
    /// dressed, it is the grey-box: a ball in a ring with a bar for a fan, built from primitives.
    ///
    /// THE MODEL (tools/author_arena_stage.py, `drone`; owner, 2026-10-05: "drone design and ufo
    /// effect needs to be more stylized". Before this it was a grey quadcopter over a plain
    /// translucent cone). A chubby rescue bot in a lifebuoy, a ceiling fan for a rotor, a
    /// nameplate on its brow, its nose along +z. Its parts, found by name:
    ///   `drone_fan`            spun about the drone's own axis;
    ///   `drone_claw_0..2`      the crane-game claw round the lens, each prong hinged at its own
    ///                          origin and opened about the tangent there;
    ///   `drone_antenna`        whipped about its foot;
    ///   `drone_face_search`, `_lock`, `_carry`, `_proud`   four screens, one shown at a time;
    ///   `drone_beam`, `drone_beam_core`   two cones of light hung at the lens, `ModelBeam` long
    ///                          at scale 1: their length is scaled, their drawings (zigzag bands
    ///                          and halftone; a barber's pole) are slid up them and coloured
    ///                          through a property block, so no material is ever copied;
    ///   `drone_spot`           the painted landing mark, taken off the drone and laid on the deck.
    ///
    /// ⚠️ IT DECIDES NOTHING. It hovers over a body for as long as that body's replicated
    /// `EdgeKind` is `Drone` and flies off when it is not, so every peer shows the same carry
    /// from the fields `SyncUnit` already brings. The host's `CharacterMotor.StepDroneCarry`
    /// owns where the body goes and how long each part takes; nothing here changes either.
    ///
    /// WHAT IT SHOWS: SIX ACTS, EACH READ OFF THE SAME REPLICATED PHASE and each told apart at a
    /// glance by the drone's own shape, its face, its beam's colour and what flies round it
    /// (all through `ArenaFx`: pooled, nothing allocated, halved under reduced effects):
    ///   SEARCH   (the catch's first half) it drops in stretched tall, prongs shut, eyes down,
    ///            sweeping a thin GOLD line of light; a scan ring turns on the body.
    ///   LOCK     (the catch's second half) it squashes, the prongs spring open, the eyes are
    ///            gold targets with a "!", a target snaps shut round the body inside a comic
    ///            burst, both cones flash on WHITE, the blip sounds, HULI KA! ("got you!").
    ///   HAUL     (the lift: the long fast part) stretched tall and shuddering, eyes screwed
    ///            shut, the cones CYAN with their bands racing up, stars spiralling up the beam,
    ///            bold rings left behind in the shaft and speed lines rushing down past.
    ///   ACROSS   (the float over the stage) banked into its travel, the beam calm, a ribbon
    ///            of sparkles and stars trailing where the body has been.
    ///   SET-DOWN the painted landing mark turns on the deck and closes on the spot, the beam
    ///            goes GOLD and slides DOWN, the eyes look down again.
    ///   PROUD, then gone: the beam off, ^ ^, a twirl with a clack of the claw, a ding, a
    ///            burst of stars and three puffs where the feet landed; then it crouches and
    ///            zips away up a white streak, with a twinkle where it went.
    /// The five seconds the body then stands frozen are the game's own tagged look (the caught
    /// mark `CharacterVisual` draws for `StunElement.None`): the drone ends its carry with
    /// `ApplyTagged`, so nothing is added here for it.
    ///
    /// IN A BREAK the same drones lift every player off the stage while it rebuilds
    /// (`ArenaShow`, which poses them itself through `Hold` and names the act): a break runs
    /// with `Time.timeScale` 0, so that path takes its own step.
    /// </summary>
    public sealed partial class ArenaDrone : MonoBehaviour
    {
        public enum Act:byte{Search,Lock,Haul,Across,SetDown,Proud,Leave}

        public const float Hover=2.6f,ArriveFrom=9,ProudSeconds=.5f,LeaveSeconds=.6f;
        /// <summary>The share of the catch after which it has the body: the lock-on.</summary>
        public const float LockAt=.45f;
        /// <summary>The model's beams are cones this long, hung under its belly at scale 1.</summary>
        private const float ModelBeam=2.5f;
        /// <summary>Where the lens is under the drone's middle, and where its ear lamps sit.</summary>
        private const float Belly=.33f,Ear=.46f,EarHeight=.17f;
        /// <summary>How far the claw's prongs open, in degrees: further and they meet the buoy.</summary>
        private const float ClawOpen=32;
        private const string GlowShader="TumbangPreso/ArenaGlow";
        private static readonly int MainTexSt=Shader.PropertyToID("_MainTex_ST"),ColourId=Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock _block;
        // The beam's colours: the kit's gold while it looks, white as it locks, the stage's cyan for the carry.
        private static readonly Color BeamGold=new Color(1,.80f,.25f),BeamCyan=new Color(.35f,.92f,1),BeamWhite=new Color(.95f,.98f,1);

        private bool _modelled;
        private Transform _fan,_antenna,_beam,_core,_spot;
        private readonly Transform[] _claws=new Transform[3],_faces=new Transform[4];
        private readonly Quaternion[] _clawRest=new Quaternion[3];
        private readonly Vector3[] _clawAxis=new Vector3[3];
        private Quaternion _antennaRest;
        private Vector3 _beamRest,_coreRest,_antennaTip=new Vector3(-.05f,.60f,-.36f);
        private Renderer _beamRenderer,_coreRenderer,_spotRenderer;
        private CharacterMotor _body;
        private Act _act;
        private int _face=-1;
        private float _leaving,_clock,_scroll,_coreScroll,_ringAt,_lineAt,_trailAt,_humAt,_claw,_yaw;
        private bool _started,_carried,_held,_landed,_voiced=true,_flat;
        private Vector3 _last,_travel,_mark;

        public static ArenaDrone Build(Transform parent,GameObject template=null)
        {
            if(template!=null)
            {
                var copy=Instantiate(template,parent,false);copy.name="Arena drone";copy.SetActive(false);
                var modelled=copy.GetComponent<ArenaDrone>();if(modelled==null)modelled=copy.AddComponent<ArenaDrone>();
                // A template that is not the kit's drone (an older export), or that was never dressed
                // (the scene is older than the model: its beam is not light), is not used: the grey-box is drawn.
                if(modelled.Find(parent)){modelled._modelled=true;return modelled;}
                Destroy(copy);
            }
            var root=new GameObject("Arena drone");root.transform.SetParent(parent,false);
            var drone=root.AddComponent<ArenaDrone>();
            Part(PrimitiveType.Sphere,"Body",root.transform,Vector3.zero,new Vector3(.9f,.76f,.9f),new Color(.93f,.90f,.84f),false);
            Part(PrimitiveType.Cylinder,"Buoy",root.transform,Vector3.down*.17f,new Vector3(1.38f,.13f,1.38f),new Color(.86f,.16f,.30f),false);
            drone._fan=new GameObject("Fan").transform;drone._fan.SetParent(root.transform,false);
            Part(PrimitiveType.Cube,"Blades",drone._fan,Vector3.up*.71f,new Vector3(1.6f,.04f,.26f),new Color(.95f,.93f,.86f),false);
            // A Unity cylinder is 2 m tall at scale 1 with its middle at its origin: hung by a holder at the lens.
            drone._beam=new GameObject("Beam").transform;drone._beam.SetParent(root.transform,false);drone._beam.localPosition=Vector3.down*Belly;
            Part(PrimitiveType.Cylinder,"Light",drone._beam,Vector3.down*(ModelBeam*.5f),new Vector3(1.1f,ModelBeam*.5f,1.1f),new Color(.55f,.95f,1.0f,.35f),true);
            drone._beamRest=drone._beam.localPosition;
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

        // The kit's parts by name. False: this is not the kit's drone, dressed.
        private bool Find(Transform home)
        {
            int claws=0;
            foreach(var part in GetComponentsInChildren<Transform>(true))
            {
                string n=part.name;
                if(n.StartsWith("drone_claw",System.StringComparison.Ordinal)){if(claws<_claws.Length)_claws[claws++]=part;}
                else if(n=="drone_fan")_fan=part;
                else if(n=="drone_antenna")_antenna=part;
                else if(n=="drone_beam_core")_core=part;
                else if(n=="drone_beam")_beam=part;
                else if(n=="drone_spot")_spot=part;
                else if(n=="drone_face_search")_faces[0]=part;
                else if(n=="drone_face_lock")_faces[1]=part;
                else if(n=="drone_face_carry")_faces[2]=part;
                else if(n=="drone_face_proud")_faces[3]=part;
            }
            if(_fan==null||_beam==null)return false;
            _beamRenderer=_beam.GetComponent<Renderer>();
            if(_beamRenderer==null||_beamRenderer.sharedMaterial==null||_beamRenderer.sharedMaterial.shader==null
               ||_beamRenderer.sharedMaterial.shader.name!=GlowShader)return false;
            _beamRest=_beam.localPosition;
            if(_core!=null){_coreRenderer=_core.GetComponent<Renderer>();_coreRest=_core.localPosition;}
            if(_antenna!=null){_antennaRest=_antenna.localRotation;_antennaTip=_antenna.localPosition+_antennaRest*new Vector3(-.05f,.25f,-.15f);}
            for(int i=0;i<_claws.Length;i++)
            {
                var claw=_claws[i];if(claw==null)continue;
                _clawRest[i]=claw.localRotation;
                var outward=claw.localPosition;outward.y=0;
                outward=outward.sqrMagnitude>1e-6f?outward.normalized:Vector3.forward;
                var axis=Vector3.Cross(Vector3.up,outward);
                // Which way round is OPEN is measured, not assumed: the turn that swings the prong's middle outward.
                var filter=claw.GetComponent<MeshFilter>();
                var middle=_clawRest[i]*(filter!=null&&filter.sharedMesh!=null?filter.sharedMesh.bounds.center:Vector3.down*.1f);
                float one=Vector3.Dot(Quaternion.AngleAxis(20,axis)*middle,outward),other=Vector3.Dot(Quaternion.AngleAxis(-20,axis)*middle,outward);
                _clawAxis[i]=one>=other?axis:-axis;
            }
            // The landing mark lies on the deck, not on the drone: it goes to the drones' own root.
            if(_spot!=null)
            {
                _spotRenderer=_spot.GetComponent<Renderer>();
                _spot.SetParent(home,false);_spot.name="Arena drone landing mark";_spot.gameObject.SetActive(false);
            }
            Face(0);
            return true;
        }

        private void OnDestroy(){if(_spot!=null)Destroy(_spot.gameObject);}

        private void OnDisable(){if(_spot!=null)_spot.gameObject.SetActive(false);}

        public void Attend(CharacterMotor body)
        {
            if(_body==body&&_leaving<=0&&gameObject.activeSelf)return;
            _body=body;_leaving=0;_started=false;_carried=false;_held=false;_landed=false;_voiced=true;_flat=false;_claw=0;
            gameObject.SetActive(true);
            _last=body.transform.position+Vector3.up*(Hover+ArriveFrom);_travel=Vector3.zero;
            Carry(0);
        }

        /// <summary>
        /// Posed from outside for one frame (the break's lift, `ArenaShow`): the drone at `at`
        /// in the middle of `act` (`t`, 0 to 1 through it), its beam reaching `beam` metres down
        /// (0: no beam) to a body drawn with its feet at `feet`, to be set down on `mark`.
        /// `dt` is the caller's own step, because a break holds `Time.deltaTime` at 0. Only a
        /// `voiced` drone sounds: a break lifts eight at once.
        /// </summary>
        public void Hold(Vector3 at,Act act,float t,float beam,Vector3 feet,Vector3 mark,float dt,bool voiced)
        {
            _body=null;_leaving=0;_voiced=voiced;_flat=true;
            if(!_held||!gameObject.activeSelf){_held=true;_started=false;_last=at;_travel=Vector3.zero;_claw=0;gameObject.SetActive(true);}
            // It faces whoever is watching: the break has one camera, the same on every peer.
            var view=ArenaFx.View;
            var to=view!=null?view.transform.position-at:Vector3.forward;to.y=0;
            float yaw=to.sqrMagnitude>.01f?Mathf.Atan2(to.x,to.z)*Mathf.Rad2Deg:0;
            _mark=mark;
            Step(act,t,at,yaw,feet,beam>.05f,true,dt);
        }

        /// <summary>Let go of a drone posed through `Hold`.</summary>
        public void Release(){_held=false;_body=null;_started=false;if(gameObject.activeSelf)gameObject.SetActive(false);}

        private bool Carrying=>_body!=null&&_body.gameObject.activeInHierarchy&&_body.EdgeKind==EdgeRecoveryKind.Drone;

        private void LateUpdate()
        {
            if(_held)return;
            float dt=Time.deltaTime;
            if(Carrying){_leaving=0;Carry(dt);return;}

            // Set down, or the round reset under it: let go, take a bow if it earned one, and go.
            if(_leaving<=0)
            {
                _landed=_carried&&_body!=null&&_body.gameObject.activeInHierarchy&&!_body.IsEdgeRecovering;
                _carried=false;
                if(_landed)_mark=_body.transform.position;
            }
            _leaving+=dt;
            float bow=_landed?ProudSeconds:0;
            var at=_last;
            if(_leaving<bow)Step(Act.Proud,_leaving/bow,at,_yaw,_mark,false,true,dt);
            else
            {
                float t=Mathf.Clamp01((_leaving-bow)/LeaveSeconds);
                // Slow off the mark, then gone: 13 m in six tenths of a second.
                at+=Vector3.up*(4+60*t)*dt;
                Step(Act.Leave,t,at,_yaw,_mark,false,false,dt);
                if(t>=1){_body=null;_started=false;gameObject.SetActive(false);}
            }
        }

        // The carry, read off the replicated phase: which act, how far through it, and where the drone is.
        private void Carry(float dt)
        {
            var feet=_body.transform.position;
            int phase=_body.EdgePhase;float ratio=_body.EdgePhaseRatio;
            Act act;float t;
            if(phase==0){bool locked=ratio>=LockAt;act=locked?Act.Lock:Act.Search;t=locked?(ratio-LockAt)/(1-LockAt):ratio/LockAt;}
            else if(phase==1)
            {
                bool lifting=ratio<CharacterMotor.DroneLiftShare;
                act=lifting?Act.Haul:Act.Across;
                t=lifting?ratio/CharacterMotor.DroneLiftShare:(ratio-CharacterMotor.DroneLiftShare)/(1-CharacterMotor.DroneLiftShare);
                _carried=true;
            }
            else{act=Act.SetDown;t=ratio;_carried=true;}

            // It drops onto the falling body over the first three fifths of the catch and holds its height after.
            float arrive=phase==0?Mathf.SmoothStep(0,1,ratio/.6f):1;
            var at=feet+Vector3.up*(Hover+ArriveFrom*(1-arrive));
            // Taking the weight: a dip as the haul begins.
            if(act==Act.Haul&&t<.16f)at.y-=.32f*Mathf.Sin(t/.16f*Mathf.PI);
            // The mark is where the body will stand: the deck straight under it as it comes down.
            bool marked=false;_mark=feet;
            if(act==Act.SetDown&&Physics.Raycast(feet+Vector3.up*.5f,Vector3.down,out var deck,9,~0,QueryTriggerInteraction.Ignore)
               &&deck.collider.GetComponentInParent<CharacterMotor>()==null){marked=true;_mark=deck.point;}
            // Its face is turned to the camera that follows the carried body, which sits behind that body.
            _yaw=_body.transform.eulerAngles.y+180;
            Step(act,t,at,_yaw,feet,true,marked,dt);
        }

        // ------------------------------------------------------------------ one frame of one act

        private void Step(Act act,float t,Vector3 at,float yaw,Vector3 feet,bool lit,bool marked,float dt)
        {
            var fx=ArenaFx.Instance;
            bool entered=!_started||act!=_act;
            _started=true;_act=act;_clock+=dt;
            bool reduced=Settings.SettingsStore.Current.ReducedEffects;
            if(dt>1e-5f)_travel=Vector3.Lerp(_travel,(at-_last)/dt,Mathf.Clamp01(dt*8));
            _last=at;

            // ---- the drone's own shape: stretch and squash, lean, turn, face, claw, fan
            float wide=1,tall=1,open=0,spin=1200,nod=8,turn=0,whip=5;
            int face=0;
            switch(act)
            {
                case Act.Search:
                    wide=.94f;tall=1.12f;spin=900;nod=16;turn=Mathf.Sin(_clock*9)*16;
                    break;
                case Act.Lock:
                {
                    // A squash that springs back, and the prongs flung open past their stop.
                    float punch=Mathf.Sin(Mathf.Clamp01(t*1.6f)*Mathf.PI);
                    wide=1+.16f*punch;tall=1-.22f*punch;face=1;spin=500;nod=18;whip=16;
                    open=ClawOpen*(1+.25f*Mathf.Sin(Mathf.Clamp01(t*2)*Mathf.PI));
                    break;
                }
                case Act.Haul:
                {
                    float shudder=reduced?0:Mathf.Sin(_clock*38)*.018f;
                    wide=.90f-shudder;tall=1.20f+shudder*2;face=2;spin=2400;nod=10;open=ClawOpen*.9f;whip=18;
                    break;
                }
                case Act.Across:
                    face=2;spin=1500;open=ClawOpen*.75f;whip=9;
                    break;
                case Act.SetDown:
                    wide=1.03f;tall=.96f;spin=800;nod=16;open=ClawOpen*.7f*(1-Mathf.SmoothStep(0,1,(t-.6f)/.4f));
                    break;
                case Act.Proud:
                {
                    // A twirl, a hop, two clacks of the claw, then a crouch before it goes.
                    float ease=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.75f)),crouch=Mathf.Clamp01((t-.78f)/.22f);
                    turn=360*ease;at.y+=.28f*Mathf.Sin(Mathf.Clamp01(t/.75f)*Mathf.PI);
                    wide=1+.12f*crouch;tall=1-.16f*crouch;face=3;spin=1300;nod=2;whip=12;
                    open=14*Mathf.Abs(Mathf.Sin(t*Mathf.PI*2));
                    break;
                }
                default:
                    wide=.84f;tall=1.36f;face=3;spin=2800;nod=0;whip=20;
                    break;
            }
            // Banked into its travel across the stage; a carry straight up or down has nothing to lean into.
            var flat=_travel;flat.y=0;
            float bank=Mathf.Min(20,flat.magnitude*2.6f);
            var lean=bank>.5f?Quaternion.AngleAxis(bank,Vector3.Cross(Vector3.up,flat.normalized)):Quaternion.identity;
            var facing=lean*Quaternion.Euler(nod,yaw+turn,0);
            if(act!=Act.Proud&&act!=Act.Leave)at.y+=Mathf.Sin(_clock*3.1f)*.05f;
            transform.SetPositionAndRotation(at,facing);
            transform.localScale=new Vector3(wide,tall,wide);
            Face(face);
            _claw=Mathf.Lerp(_claw,open,Mathf.Clamp01(dt*26));
            for(int i=0;i<_claws.Length;i++)if(_claws[i]!=null)_claws[i].localRotation=Quaternion.AngleAxis(_claw,_clawAxis[i])*_clawRest[i];
            if(_antenna!=null)_antenna.localRotation=_antennaRest*Quaternion.Euler(Mathf.Sin(_clock*13)*whip,0,Mathf.Cos(_clock*9)*whip*.6f);
            if(_fan!=null)_fan.Rotate(0,spin*dt,0,Space.Self);

            // ---- the beam's two cones, and the mark on the deck
            var belly=at+facing*Vector3.down*(Belly*tall);
            float reach=Mathf.Max(0,belly.y-feet.y+.12f);
            Beams(act,t,lit&&act<Act.Proud,reach,tall,wide,yaw,dt);
            Mark(act,t,marked,dt);

            if(entered)Enter(act,at,feet,fx);
            if(fx==null)return;
            Lamps(fx,act,at,facing);
            if(lit&&act<Act.Proud)Ride(fx,act,t,belly,feet,reach,reduced,dt);
            else if(act==Act.Leave)
            {
                // The streak it leaves: one fat white line and two thin gold ones, from the drone down to where it stood.
                float length=Mathf.Min(9,2+t*16);
                fx.DrawBeam(at,at+Vector3.down*length,.55f,.05f,ArenaFx.White,.85f*(1-t*.5f),ArenaFx.Cell.Line);
                var side=facing*Vector3.right*.34f;
                fx.DrawBeam(at+side,at+side+Vector3.down*length*.7f,.2f,.03f,ArenaFx.Gold,.7f,ArenaFx.Cell.Line);
                fx.DrawBeam(at-side,at-side+Vector3.down*length*.7f,.2f,.03f,ArenaFx.Gold,.7f,ArenaFx.Cell.Line);
                if(t>=1&&!_held)fx.Emit(ArenaFx.Cell.Sparkle,ArenaFx.Mode.Billboard,at,Vector3.zero,ArenaFx.White,.95f,.32f,.5f,2.2f,-1,-1,0,0,0,.2f);
            }

            // The hover's own voice: a wobbly hum, re-struck while it works, higher the harder it pulls.
            if(_voiced&&act>=Act.Haul&&act<=Act.SetDown)
            {
                _humAt-=dt;
                if(_humAt<=0)
                {
                    _humAt=.5f;
                    float pitch=act==Act.Haul?1.3f+.3f*t:act==Act.Across?1.05f:.88f;
                    Sound("sfx_arena_drone_hum",at,pitch,.55f);
                }
            }
        }

        private void Face(int face)
        {
            if(face==_face)return;
            _face=face;
            for(int i=0;i<_faces.Length;i++)if(_faces[i]!=null)_faces[i].gameObject.SetActive(i==face);
        }

        // The cones: their length scaled, their drawings slid along them, their colour set for the act.
        private void Beams(Act act,float t,bool lit,float reach,float tall,float wide,float yaw,float dt)
        {
            // Searching, the model shows only its thin core.
            bool wideOn=lit&&!(_modelled&&act==Act.Search);
            if(_beam.gameObject.activeSelf!=wideOn)_beam.gameObject.SetActive(wideOn);
            if(_core!=null&&_core.gameObject.activeSelf!=lit)_core.gameObject.SetActive(lit);
            if(!lit)return;
            // The light falls straight down whatever way the drone is leaning: the body is under it.
            var down=Quaternion.Euler(0,yaw,0);
            _beam.rotation=down;
            if(_core!=null)_core.rotation=down;
            if(!_modelled){_beam.localScale=new Vector3(act==Act.Search?.3f:1,reach/ModelBeam/Mathf.Max(.1f,tall),act==Act.Search?.3f:1);return;}

            // Searching: only the thin core, gold. Locked: both, white. The carry: cyan, racing on the haul.
            // Set-down: gold again, sliding DOWN. The drone's own squash is taken back out of the cones.
            float outer=1,inner=1,speed=1.1f,alpha=.9f;
            Color colour=BeamCyan,core=BeamCyan;
            switch(act)
            {
                case Act.Search:outer=0;inner=.34f;speed=2.4f;colour=core=BeamGold;alpha=.65f+.3f*Mathf.Sin(_clock*40);break;
                case Act.Lock:outer=1.3f-.3f*Mathf.Clamp01(t*2);inner=.7f;speed=4;colour=BeamWhite;core=BeamGold;alpha=1;break;
                case Act.Haul:outer=1+.05f*Mathf.Sin(_clock*17);speed=3.4f;core=BeamWhite;alpha=1;break;
                case Act.Across:speed=1.1f;alpha=.85f;break;
                default:outer=1-.25f*t;inner=.8f-.3f*t;speed=-1.3f;colour=BeamGold;core=BeamWhite;alpha=.9f-.4f*t;break;
            }
            _scroll=Mathf.Repeat(_scroll-speed*.5f*dt,1);_coreScroll=Mathf.Repeat(_coreScroll-speed*.85f*dt,1);
            float length=reach/ModelBeam/Mathf.Max(.1f,tall);
            if(_block==null)_block=new MaterialPropertyBlock();
            _beam.localPosition=_beamRest;
            _beam.localScale=new Vector3(Mathf.Max(.01f,outer)/wide,length,Mathf.Max(.01f,outer)/wide);
            float light=ArenaFx.Light;
            _block.SetVector(MainTexSt,new Vector4(1,1,0,_scroll));
            _block.SetColor(ColourId,new Color(colour.r*1.5f,colour.g*1.5f,colour.b*1.5f,alpha*.8f*light));
            _beamRenderer.SetPropertyBlock(_block);
            if(_core==null||_coreRenderer==null)return;
            _core.localPosition=_coreRest;
            _core.localScale=new Vector3(inner/wide,length,inner/wide);
            _block.SetVector(MainTexSt,new Vector4(1,1,_coreScroll*.5f,_coreScroll));
            _block.SetColor(ColourId,new Color(core.r*1.7f,core.g*1.7f,core.b*1.7f,alpha*light));
            _coreRenderer.SetPropertyBlock(_block);
        }

        // The landing mark: laid on the deck as the body comes down, closing on the spot and
        // turning; it swells and goes as the drone takes its bow.
        private void Mark(Act act,float t,bool marked,float dt)
        {
            if(_spot==null)return;
            bool shown=marked&&(act==Act.SetDown||act==Act.Proud);
            if(_spot.gameObject.activeSelf!=shown)_spot.gameObject.SetActive(shown);
            if(!shown)return;
            float size=act==Act.SetDown?1.55f-.55f*Mathf.SmoothStep(0,1,t):1+.3f*t;
            float alpha=act==Act.SetDown?Mathf.Clamp01(t*5):1-t;
            _spot.SetPositionAndRotation(_mark+Vector3.up*.06f,Quaternion.Euler(0,_clock*150,0));
            _spot.localScale=new Vector3(size,1,size);
            if(_spotRenderer==null)return;
            if(_block==null)_block=new MaterialPropertyBlock();
            _block.SetVector(MainTexSt,new Vector4(1,1,0,0));
            _block.SetColor(ColourId,new Color(1,1,1,alpha));
            _spotRenderer.SetPropertyBlock(_block);
        }

        private void Sound(string id,Vector3 at,float pitch,float volume=1)
        {
            if(!_voiced)return;
            if(_flat)ArenaFx.CueFlat(id,pitch*.98f,pitch*1.02f,volume*.8f);else ArenaFx.Cue(id,at,pitch*.97f,pitch*1.04f,volume);
        }

        // ------------------------------------------------------------------ an act's one moment

        private void Enter(Act act,Vector3 at,Vector3 feet,ArenaFx fx)
        {
            var chest=feet+Vector3.up;
            switch(act)
            {
                case Act.Lock:
                    Sound("sfx_arena_drone_ping",feet,1);
                    if(_body!=null)Visual.ComicPopup.Spawn(feet+Vector3.up*.6f,"HULI KA!",ArenaFx.Gold,1.1f,Visual.ComicPopup.Weight.Cast,_body);
                    if(fx==null)break;
                    fx.Emit(ArenaFx.Cell.Burst,ArenaFx.Mode.Billboard,chest,Vector3.zero,ArenaFx.White,.9f,.34f,2.2f,5.2f,-1,-1,0,0,0,.1f);
                    fx.Flash(chest,1,4.5f,ArenaFx.Gold,.7f,.22f);
                    fx.Stars(chest,Vector3.up,180,6,3,7,ArenaFx.Gold,ArenaFx.White,.95f,.35f,.6f,.5f,3);
                    break;
                case Act.Haul:
                    Sound("sfx_arena_drone_beam",at,1);
                    _humAt=.1f;
                    if(fx==null)break;
                    fx.Emit(ArenaFx.Cell.Band,ArenaFx.Mode.Flat,feet+Vector3.up*.2f,Vector3.zero,ArenaFx.White,.9f,.4f,1.6f,7,-1,-1,0,0,0,.05f);
                    fx.Flash(chest,1.2f,5,ArenaFx.Cyan,.8f,.25f);
                    break;
                case Act.Proud:
                    Sound("sfx_arena_drone_set",_mark,1);
                    if(fx!=null)
                    {
                        var deck=_mark+Vector3.up*.08f;
                        fx.Emit(ArenaFx.Cell.Burst,ArenaFx.Mode.Flat,deck,Vector3.zero,ArenaFx.White,.9f,.4f,1.6f,6,-1,-1,0,0,0,.08f);
                        fx.Emit(ArenaFx.Cell.Band,ArenaFx.Mode.Flat,deck,Vector3.zero,ArenaFx.Gold,.85f,.5f,1.2f,4.6f,-1,-1,0,0,0,.05f);
                        fx.Stars(_mark+Vector3.up*.5f,Vector3.up,70,8,3,6.5f,ArenaFx.Gold,ArenaFx.White,.95f,.5f,.9f,.55f,7);
                        for(int i=0;i<ArenaFx.Count(3);i++)
                        {
                            float a=fx.Rand(0,Mathf.PI*2);var away=new Vector3(Mathf.Cos(a),.25f,Mathf.Sin(a));
                            fx.Emit(ArenaFx.Cell.Puff,ArenaFx.Mode.Billboard,_mark+away*.5f+Vector3.up*.2f,away*2.4f,ArenaFx.White,.55f,.5f,.5f,1.1f,-1,-1,0,2.5f,0,.1f);
                        }
                    }
                    // Felt by whoever was carried, and only by them.
                    var view=Camera.main;
                    var rig=view!=null?view.GetComponent<CameraSystem.CameraRig>():null;
                    if(rig!=null&&_body!=null&&rig.IsFollowing(_body))rig.Shake(.5f,.5f);
                    break;
                case Act.Leave:
                    Sound("sfx_arena_drone_zip",at,1,.9f);
                    if(fx!=null)fx.Emit(ArenaFx.Cell.Band,ArenaFx.Mode.Flat,at+Vector3.down*.4f,Vector3.zero,ArenaFx.White,.7f,.3f,1,3.4f,-1,-1,0,0,0,.05f);
                    break;
            }
        }

        // ------------------------------------------------------------------ what is drawn round it

        // Its lamps: the two ears wink in turn, the beacon on its antenna flashes when it has work.
        private void Lamps(ArenaFx fx,Act act,Vector3 at,Quaternion facing)
        {
            var right=facing*Vector3.right;var up=facing*Vector3.up;
            bool one=Mathf.Repeat(_clock*1.7f,1)<.3f,other=Mathf.Repeat(_clock*1.7f+.5f,1)<.3f;
            if(one)fx.DrawBillboard(ArenaFx.Cell.Dot,at-right*Ear+up*EarHeight,.5f,ArenaFx.Gold,.8f);
            if(other)fx.DrawBillboard(ArenaFx.Cell.Dot,at+right*Ear+up*EarHeight,.5f,ArenaFx.Gold,.8f);
            bool busy=act>=Act.Lock&&act<=Act.SetDown;
            if(Mathf.Repeat(_clock*(busy?5:1.4f),1)<.5f)fx.DrawBillboard(ArenaFx.Cell.Dot,at+facing*_antennaTip,busy?.6f:.4f,ArenaFx.Magenta,.9f);
        }

        // What rides the beam, act by act.
        private void Ride(ArenaFx fx,Act act,float t,Vector3 belly,Vector3 feet,float reach,bool reduced,float dt)
        {
            float thin=reduced?2:1;
            var chest=feet+Vector3.up;

            if(act==Act.Search)
            {
                // A scan ring turning on the body, closing as the drone nears; the line of light is the core cone.
                float size=4.2f-1.4f*t;
                fx.DrawFlat(ArenaFx.Cell.Scan,chest,size,size,_clock*220,ArenaFx.Gold,.75f);
                fx.DrawBeam(belly,feet,.12f,.3f,ArenaFx.Gold,.35f);
                Lines(fx,belly+Vector3.up*.8f,Vector3.up*9,.5f,.05f*thin,ArenaFx.White,.5f,dt);
                return;
            }
            if(act==Act.Lock)
            {
                // The target snaps shut round the body: big and turning, down to a body's width, then a held beat.
                float shut=1-Mathf.Pow(1-Mathf.Clamp01(t*2.2f),3);
                fx.DrawSpun(ArenaFx.Cell.Target,chest,5.6f-3.0f*shut,90*(1-shut),ArenaFx.Gold,.95f);
                fx.DrawBillboard(ArenaFx.Cell.Dot,chest,3,ArenaFx.Gold,.25f);
                return;
            }

            // The carry. A soft glow behind the cones and round the body, and the stars that ride up the beam.
            Color glow=act==Act.SetDown?ArenaFx.Gold:ArenaFx.Cyan;
            fx.DrawBeam(belly,feet+Vector3.down*.2f,.6f,2.6f,glow,.16f);
            fx.DrawBillboard(ArenaFx.Cell.Dot,chest,3.2f,glow,.22f);
            int riders=reduced?2:4;
            float climb=act==Act.Haul?2.2f:act==Act.Across?1.1f:-.8f;
            for(int k=0;k<riders;k++)
            {
                float h=Mathf.Repeat(_clock*climb*.5f+k/(float)riders,1),a=_clock*5+k*2.4f;
                float r=.25f+.6f*(1-h);
                var p=feet+new Vector3(Mathf.Cos(a)*r,h*reach,Mathf.Sin(a)*r);
                fx.DrawSpun(k%2==0?ArenaFx.Cell.Star5:ArenaFx.Cell.Sparkle,p,.34f,a*40,k%2==0?ArenaFx.Gold:ArenaFx.White,.95f*Mathf.Sin(h*Mathf.PI));
            }

            if(act==Act.Haul)
            {
                // Bold rings left standing in the shaft as it climbs, and speed lines rushing down past it.
                _ringAt-=dt;
                if(_ringAt<=0)
                {
                    _ringAt=.085f*thin;
                    fx.Emit(ArenaFx.Cell.Band,ArenaFx.Mode.Flat,feet+Vector3.down*.3f,Vector3.down*3,fx.Rand()<.5f?ArenaFx.White:ArenaFx.Cyan,.85f,.5f,2.0f,3.6f,-1,-1,0,0,0,.05f);
                }
                Lines(fx,chest,Vector3.down*16,2.3f,.028f*thin,ArenaFx.White,.8f,dt);
            }
            else if(act==Act.Across)
            {
                // A ribbon of sparkles left where the body has been, and a ring it rides on.
                _trailAt-=dt;
                if(_trailAt<=0)
                {
                    _trailAt=.055f*thin;
                    bool star=fx.Rand()<.5f;
                    var at=chest+new Vector3(fx.Rand(-.4f,.4f),fx.Rand(-.7f,.5f),fx.Rand(-.4f,.4f));
                    fx.Emit(star?ArenaFx.Cell.Star5:ArenaFx.Cell.Sparkle,ArenaFx.Mode.Billboard,at,new Vector3(0,fx.Rand(-.6f,.2f),0),star?ArenaFx.Gold:ArenaFx.White,.95f,fx.Rand(.5f,.85f),.42f,.08f,-1,-1,1.2f,.5f,0,.1f);
                }
                float pulse=2.3f+.2f*Mathf.Sin(_clock*8);
                fx.DrawFlat(ArenaFx.Cell.Band,feet+Vector3.up*.05f,pulse,pulse,0,ArenaFx.Cyan,.6f);
            }
            else
            {
                // Coming down: a gold ring closing on the mark with the body.
                float size=5.4f-2.8f*t;
                fx.DrawFlat(ArenaFx.Cell.Band,_mark+Vector3.up*.05f,size,size,0,ArenaFx.Gold,.3f+.4f*t);
                fx.DrawFlat(ArenaFx.Cell.Halftone,_mark+Vector3.up*.04f,3.4f,3.4f,-_clock*60,ArenaFx.White,.3f*t);
            }
        }

        // Speed lines: hard slivers thrown along `velocity` from a ring round `centre`.
        private void Lines(ArenaFx fx,Vector3 centre,Vector3 velocity,float radius,float every,Color colour,float alpha,float dt)
        {
            _lineAt-=dt;
            if(_lineAt>0)return;
            _lineAt=every;
            float a=fx.Rand(0,Mathf.PI*2),r=radius*fx.Rand(.55f,1);
            var from=centre+new Vector3(Mathf.Cos(a)*r,fx.Rand(-1.5f,2.5f),Mathf.Sin(a)*r)-velocity*.08f;
            float length=velocity.magnitude*fx.Rand(.10f,.19f);
            fx.Emit(ArenaFx.Cell.Line,ArenaFx.Mode.Stretch,from,velocity,fx.Rand()<.3f?ArenaFx.Cyan:colour,alpha,fx.Rand(.22f,.34f),.16f,.10f,length,length*.6f,0,0,0,.15f);
        }
    }
}
