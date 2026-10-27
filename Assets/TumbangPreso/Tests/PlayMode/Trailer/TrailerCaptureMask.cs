using System;
using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>Render-time suppression only. Native gameplay/state and authored power VFX keep running.</summary>
    public sealed class TrailerCaptureMask : IDisposable
    {
        private readonly List<(Renderer renderer,bool hidden)> _renderers=new List<(Renderer,bool)>();
        private readonly List<(Canvas canvas,bool enabled)> _canvases=new List<(Canvas,bool)>();
        public int HiddenRenderers => _renderers.Count;

        public void Apply(bool hideHands=false)
        {
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            { _canvases.Add((canvas,canvas.enabled)); canvas.enabled=false; }
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                bool cue=renderer.GetComponentInParent<CharacterNameplate>()!=null
                    ||renderer.GetComponentInParent<TayaIdentity>()!=null
                    ||renderer.GetComponentInParent<TrajectoryPreview>()!=null
                    ||renderer.GetComponentInParent<LataClockPresentation>()!=null
                    ||renderer.GetComponentInParent<CourtBoundaryPresentation>()!=null
                    ||renderer.GetComponentInParent<ComicPopup>()!=null
                    ||renderer.GetComponentInParent<DanteWardBadge>()!=null
                    ||renderer.GetComponentInParent<PaeteCatchMarker>()!=null
                    ||renderer.GetComponentInParent<PhaisterAimSigil>()!=null
                    ||renderer.name=="TayaHeadband"
                    ||renderer.name.StartsWith("StatusBadge",StringComparison.Ordinal)
                    ||renderer.GetComponentInParent<UI.PlayerNameplate>()!=null
                    ||(hideHands && renderer.GetComponentInParent<CameraSystem.ViewmodelArms>()!=null);
                if(!cue)
                    foreach(var material in renderer.sharedMaterials)
                        if(material!=null && material.shader!=null &&
                           (material.shader.name=="TumbangPreso/PlayerGroundMarker" || material.shader.name=="TumbangPreso/AimGuide"))
                        { cue=true; break; }
                if(!cue) continue;
                _renderers.Add((renderer,renderer.forceRenderingOff)); renderer.forceRenderingOff=true;
            }
        }

        public void Dispose()
        {
            foreach(var pair in _renderers) if(pair.renderer!=null) pair.renderer.forceRenderingOff=pair.hidden;
            foreach(var pair in _canvases) if(pair.canvas!=null) pair.canvas.enabled=pair.enabled;
            _renderers.Clear(); _canvases.Clear();
        }
    }
}
