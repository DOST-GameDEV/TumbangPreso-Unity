using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.CameraSystem
{
    // V2: the held silhouette shows possession/preparation. Its own lens keeps
    // that read stable while the player's world FOV changes. Physics uses AimEye.
    public sealed partial class ViewmodelArms
    {
        public const float FixedViewmodelFov=95;
        private sealed class ViewSurface
        {
            public Renderer Renderer;public MaterialPropertyBlock Block;
            public bool Hidden;public ShadowCastingMode Shadows;
        }
        private sealed class ViewFrame
        {
            public Camera Camera;public Vector3 Position,Scale,LensScale;
            public readonly List<Renderer> Renderers=new List<Renderer>();
            public readonly List<ViewSurface> Surfaces=new List<ViewSurface>();
        }
        private readonly List<ViewFrame> _viewFrames=new List<ViewFrame>();
        private readonly Stack<ViewFrame> _viewPool=new Stack<ViewFrame>();
        private Transform _viewLens;
        private Camera _viewCamera;
        private MaterialPropertyBlock _viewBlock;
        private float _chargeBegan=-100;
        private Vector3 _releaseSweep,_releaseLeftSweep;
        private void RestoreReleaseSweep()
        {
            if(_rightPivot!=null)_rightPivot.localPosition-=_releaseSweep;
            if(_leftPivot!=null)_leftPivot.localPosition-=_releaseLeftSweep;
            _releaseSweep=_releaseLeftSweep=Vector3.zero;
        }
        private void ApplyReleaseSweep()
        {
            if(!ReferenceEquals(_clip,ThrowClip) || _rightPivot==null)return;
            float weight=Mathf.Clamp01(WorldCueProfile.Current.ViewmodelFraming);
            if(Settings.SettingsStore.Current.ReducedUiMotion)weight=0;
            Vector3 contact=new Vector3(-.20f,.14f,.02f),follow=ReleaseFollow(ThrowGesture.Spin(_actionName));
            float time=_clipTime;
            if(time<ThrowGesture.ContactSeconds)_releaseSweep=Vector3.Lerp(Vector3.zero,contact,time/ThrowGesture.ContactSeconds);
            else if(time<ThrowGesture.FollowSeconds)_releaseSweep=Vector3.LerpUnclamped(contact,follow,SweepWhip(Mathf.InverseLerp(ThrowGesture.ContactSeconds,ThrowGesture.FollowSeconds,time)));
            else _releaseSweep=Vector3.Lerp(follow,Vector3.zero,ThrowGesture.Recovery(time));
            _releaseSweep*=weight;
            _releaseLeftSweep=new Vector3(-.10f,-.10f,-.02f)*Mathf.Clamp01(_releaseSweep.magnitude/.5f);
            _rightPivot.localPosition+=_releaseSweep;
            if(_leftPivot!=null)_leftPivot.localPosition+=_releaseLeftSweep;
        }
        public float ChargeAnticipation
        {
            get
            {
                if(!_carrying || _charge<0 || Settings.SettingsStore.Current.ReducedUiMotion)return 0;
                float age=Time.time-_chargeBegan;
                return age>=0 && age<.12f?Mathf.Sin(age/.12f*Mathf.PI)*WorldCueProfile.Current.ViewmodelFraming:0;
            }
        }
        /// <summary>
        /// Where a first-person hand ends, in world space: the forward face of that arm's drawn mesh
        /// (HERO-9: Paete's vines leave from here in first person, owner: *"his vines actually come
        /// from his arm"*). ⚠️ Read outside a render, so it is the lens at rest; the per-render
        /// framing scale moves it by a few centimetres, which the braid's width at the forearm covers.
        /// </summary>
        public bool TryHandTip(bool left, out Vector3 world)
        {
            world = default;
            var arm = left ? _leftArmRenderer : _rightArmRenderer;
            if (arm == null || !arm.enabled || !arm.gameObject.activeInHierarchy) return false;
            var eye = _viewCamera != null ? _viewCamera : GetComponentInParent<Camera>();
            if (eye == null) return false;
            Vector3 f = eye.transform.forward, c = arm.bounds.center, e = arm.bounds.extents;
            float reach = Mathf.Abs(f.x) * e.x + Mathf.Abs(f.y) * e.y + Mathf.Abs(f.z) * e.z;
            world = c + f * reach * 0.85f;
            return true;
        }

        /// <summary>
        /// ⚠️⚠️ WHERE A FIRST-PERSON ARM IS DRAWN, NOT WHERE IT RESTS (HERO-9, owner 2026-09-26, of LIANA LEAP from his own
        /// eyes: *"make it look like its actually his arms extending bcz it doesnt look like taht"*). The drawn arm is lensed
        /// per render (`BeginViewFrame`: pulled toward a fixed 95 degree look, lowered 8 cm, scaled), so a world object started
        /// at the RESTING hand (`TryHandTip`) sat beside the hand on screen, and Paete's braid read as planks next to his arms.
        /// This applies the own camera's framing for a moment, reads the arm's front face (the hand), its back end (where it
        /// comes into view) and its half-width across the view, and puts everything back. Never called inside a render.
        /// </summary>
        public bool TryDrawnArm(bool left, out Vector3 hand, out Vector3 back, out float halfWidth)
        {
            hand = back = default; halfWidth = 0f;
            var arm = left ? _leftArmRenderer : _rightArmRenderer;
            if (arm == null || !arm.enabled || !arm.gameObject.activeInHierarchy || _viewFrames.Count > 0) return false;
            if (!EnsureViewLens() || _viewCamera == null) return false;
            var camera = _viewCamera;
            Vector3 position = transform.localPosition, scale = transform.localScale, lens = _viewLens.localScale;
            float weight = Mathf.Clamp01(WorldCueProfile.Current.ViewmodelFraming);
            float compensate = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f) / Mathf.Tan(FixedViewmodelFov * Mathf.Deg2Rad * .5f);
            float lensScale = Mathf.Lerp(1, compensate, weight);
            try
            {
                _viewLens.localScale = Vector3.Scale(lens, new Vector3(lensScale, lensScale, 1));
                transform.localPosition = position + Vector3.down * (.08f * weight);
                transform.localScale = scale * Mathf.Lerp(1, .64f / CameraRig.ViewmodelScale, weight);
                Vector3 f = camera.transform.forward, r = camera.transform.right, c = arm.bounds.center, e = arm.bounds.extents;
                float reach = Mathf.Abs(f.x) * e.x + Mathf.Abs(f.y) * e.y + Mathf.Abs(f.z) * e.z;
                hand = c + f * reach * 0.85f;
                back = c - f * reach * 0.85f;
                halfWidth = Mathf.Abs(r.x) * e.x + Mathf.Abs(r.y) * e.y + Mathf.Abs(r.z) * e.z;
            }
            finally
            {
                _viewLens.localScale = lens; transform.localPosition = position; transform.localScale = scale;
            }
            return true;
        }

        /// <summary>
        /// ⚠️ PAETE'S FOREARMS LENGTHEN AS HIS VINES LEAVE THEM (the same owner note as `TryDrawnArm`): 0 is his rest, 0.35 is a
        /// third again as long. His arms are bark with the vines already in them (v16), so in his own eyes the arm itself
        /// reaches, then the strands pour out of its end. Only Paete's arms; any other hero's call is ignored.
        /// </summary>
        public void SetReachStretch(float stretch)
        {
            if (_currentHeroId != "paete") return;
            var length = new Vector3(PaeteArmBulk, 1.0f + Mathf.Clamp(stretch, 0f, 0.6f), PaeteArmBulk);
            if (_rightArm != null) _rightArm.localScale = length;
            if (_leftArm != null) _leftArm.localScale = length;
        }

        /// <summary>The two drawn arms (HERO-9 v5: Paete's channel lights his own hands in first person, `Visual.PaeteChannelGlow`).</summary>
        public bool TryArmRenderers(out MeshRenderer left, out MeshRenderer right)
        {
            left = _leftArmRenderer; right = _rightArmRenderer;
            return left != null && right != null && left.gameObject.activeInHierarchy && right.gameObject.activeInHierarchy;
        }

        private void OnEnable(){Camera.onPreCull+=BeginViewFrame;Camera.onPostRender+=EndViewFrame;}
        private void OnDisable()
        {
            Camera.onPreCull-=BeginViewFrame;Camera.onPostRender-=EndViewFrame;
            for(int i=_viewFrames.Count-1;i>=0;i--)RestoreViewFrame(_viewFrames[i]);_viewFrames.Clear();
            RestoreReleaseSweep();
        }
        private void OnDestroy(){if(_viewLens!=null)Destroy(_viewLens.gameObject);}
        private bool EnsureViewLens()
        {
            if(_viewLens!=null && _viewCamera!=null)return true;
            _viewCamera=GetComponentInParent<Camera>();
            if(_viewCamera==null || transform.parent==null)return false;
            var parent=transform.parent;
            var go=new GameObject("~PrivateViewmodelLens");go.transform.SetParent(parent,false);
            _viewLens=go.transform;transform.SetParent(_viewLens,false);return true;
        }
        private void BeginViewFrame(Camera camera)
        {
            if(!EnsureViewLens())return;
            var frame=_viewPool.Count>0?_viewPool.Pop():new ViewFrame();frame.Camera=camera;
            frame.Position=transform.localPosition;frame.Scale=transform.localScale;frame.LensScale=_viewLens.localScale;
            GetComponentsInChildren(true,frame.Renderers);
            var basis=_viewFrames.Count>0?_viewFrames[0]:frame;
            bool own=camera==_viewCamera;float weight=Mathf.Clamp01(WorldCueProfile.Current.ViewmodelFraming);
            if(own)
            {
                float compensate=Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Tan(FixedViewmodelFov*Mathf.Deg2Rad*.5f);
                float scale=Mathf.Lerp(1,compensate,weight);
                _viewLens.localScale=Vector3.Scale(basis.LensScale,new Vector3(scale,scale,1));
                transform.localPosition=basis.Position+Vector3.down*(.08f*weight);
                transform.localScale=basis.Scale*Mathf.Lerp(1,.64f/CameraRig.ViewmodelScale,weight);
            }
            _viewBlock??=new MaterialPropertyBlock();
            for(int i=0;i<frame.Renderers.Count;i++)
            {
                var surface=frame.Renderers[i];
                if(i==frame.Surfaces.Count)frame.Surfaces.Add(new ViewSurface{Block=new MaterialPropertyBlock()});
                var saved=frame.Surfaces[i];saved.Renderer=surface;saved.Hidden=surface.forceRenderingOff;
                saved.Shadows=surface.shadowCastingMode;surface.GetPropertyBlock(saved.Block);
                if(!own){surface.forceRenderingOff=true;continue;}
                int originalIndex=basis.Renderers.IndexOf(surface);
                if(originalIndex>=0)surface.forceRenderingOff=basis.Surfaces[originalIndex].Hidden;
                surface.GetPropertyBlock(_viewBlock);
                _viewBlock.SetFloat("_ViewmodelRimStrength",surface==_heldRenderer?WorldCueProfile.Current.ViewmodelRim*weight:0);
                surface.SetPropertyBlock(_viewBlock);
                if(weight>0)surface.shadowCastingMode=ShadowCastingMode.Off;
            }
            _viewFrames.Add(frame);
        }
        private void EndViewFrame(Camera camera)
        {
            for(int i=_viewFrames.Count-1;i>=0;i--)if(_viewFrames[i].Camera==camera)
            {var frame=_viewFrames[i];_viewFrames.RemoveAt(i);RestoreViewFrame(frame);return;}
        }
        private void RestoreViewFrame(ViewFrame frame)
        {
            if(_viewLens!=null)_viewLens.localScale=frame.LensScale;
            transform.localPosition=frame.Position;transform.localScale=frame.Scale;
            for(int i=0;i<frame.Renderers.Count;i++)
            {
                var state=frame.Surfaces[i];if(state.Renderer==null)continue;
                state.Renderer.SetPropertyBlock(state.Block);state.Renderer.forceRenderingOff=state.Hidden;state.Renderer.shadowCastingMode=state.Shadows;
            }
            frame.Renderers.Clear();frame.Camera=null;_viewPool.Push(frame);
        }
    }
}
