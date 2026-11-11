using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Her redesign paints the face into the atlas. Repaint just its ink on the
    // cinematic copy, so the cheeks, hair shadow, outline and lighting stay on the same head.
    internal sealed class AmihanCinematicFace : IDisposable
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Eyes = Shader.PropertyToID("_Eyes");
        private static readonly int Mouth = Shader.PropertyToID("_Mouth");

        private readonly SkinnedMeshRenderer _head;
        private readonly Texture _atlas;
        private readonly RenderTexture _paint;
        private readonly Material _brush;
        private readonly MaterialPropertyBlock _original = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _draw = new MaterialPropertyBlock();
        private Face _shown;
        private bool _hasPaint;

        private readonly struct Face
        {
            public readonly Vector4 Eyes, Mouth;

            public Face(Vector4 eyes, Vector4 mouth)
            {
                Eyes = eyes;
                Mouth = mouth;
            }

            public static Face Blend(Face from, Face to, float weight) =>
                new Face(Vector4.Lerp(from.Eyes, to.Eyes, weight), Vector4.Lerp(from.Mouth, to.Mouth, weight));
        }

        // Eyes: left/right opening, lid curve, lift. Mouth: half-width, opening, smile, tilt.
        // A small side smile is enough on this atlas. Larger or darker marks change her face.
        private static readonly Face Calm = new Face(new Vector4(.65f, .68f, 2, 0), new Vector4(30, 4, 12, 1));
        private static readonly Face Listening = new Face(new Vector4(.88f, .52f, 2, 2), new Vector4(24, 4, 3, -4));
        private static readonly Face Whistle = new Face(new Vector4(.48f, .58f, 1, 0), new Vector4(13, 16, 0, 0));
        private static readonly Face LookingUp = new Face(new Vector4(1.02f, .94f, 0, 5), new Vector4(18, 8, 4, 1));
        private static readonly Face Delight = new Face(new Vector4(.87f, .81f, 3, 1), new Vector4(37, 8, 13, -1));
        private static readonly Face Brace = new Face(new Vector4(.41f, .47f, -4, -1), new Vector4(23, 4, 0, 1));
        private static readonly Face Drive = new Face(new Vector4(.29f, .36f, -3, -2), new Vector4(21, 9, 0, 0));
        private static readonly Face Smile = new Face(new Vector4(.87f, .81f, 4, 0), new Vector4(36, 7, 13, -3));
        private static readonly Face Wink = new Face(new Vector4(.86f, .04f, 13, 0), new Vector4(36, 8, 14, -4));

        private AmihanCinematicFace(SkinnedMeshRenderer head, Texture atlas, Shader shader)
        {
            _head = head;
            _atlas = atlas;
            _head.GetPropertyBlock(_original);
            _brush = new Material(shader) { name = "Amihan cinematic ink" };
            _paint = new RenderTexture(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "Amihan cinematic face atlas",
                useMipMap = true,
                autoGenerateMips = true,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        public static AmihanCinematicFace Attach(IEnumerable<Renderer> renderers)
        {
            var shader = Resources.Load<Shader>("Shaders/AmihanCinematicFace");
            if (shader == null || !shader.isSupported) return null;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (!(renderer is SkinnedMeshRenderer head) || head.sharedMesh == null
                    || head.sharedMesh.name.IndexOf("head", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                head.GetPropertyBlock(block);
                var material = head.sharedMaterial;
                var atlas = block.GetTexture(MainTex)
                    ?? (material != null && material.HasProperty(MainTex) ? material.GetTexture(MainTex) : null);
                if (atlas == null || atlas.name.IndexOf("amihan-redesign-atlas", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var face = new AmihanCinematicFace(head, atlas, shader);
                if (face._paint.Create()) return face;
                face.Dispose();
                return null;
            }
            return null;
        }

        public void Sample(float time)
        {
            Face pose = PoseAt(time);
            if (_hasPaint && pose.Eyes == _shown.Eyes && pose.Mouth == _shown.Mouth) return;
            _brush.SetVector(Eyes, pose.Eyes);
            _brush.SetVector(Mouth, pose.Mouth);
            var previous = RenderTexture.active;
            bool previousSrgb = GL.sRGBWrite;
            try
            {
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(_atlas, _paint, _brush);
            }
            finally
            {
                GL.sRGBWrite = previousSrgb;
                RenderTexture.active = previous;
            }
            _head.GetPropertyBlock(_draw);
            _draw.SetTexture(MainTex, _paint);
            _head.SetPropertyBlock(_draw);
            _shown = pose;
            _hasPaint = true;
        }

        private static Face PoseAt(float time)
        {
            // Give the whistle time to form on the raised hand. Relax into curiosity as the
            // wind answers, then smile at the bird before looking back down the release lane.
            if (time < .77f) return Ease(Calm, Listening, .43f, .77f, time);
            if (time < 1.78f) return Ease(Listening, Whistle, 1.65f, 1.78f, time);
            if (time < 2.24f) return Ease(Whistle, LookingUp, 1.98f, 2.24f, time);
            if (time < 2.99f) return Ease(LookingUp, Delight, 2.74f, 2.99f, time);
            if (time < 3.95f) return Ease(Delight, Brace, 3.59f, 3.95f, time);
            if (time < 4.59f) return Ease(Brace, Drive, 4.43f, 4.59f, time);
            if (time < 5.34f) return Ease(Drive, Smile, 4.91f, 5.34f, time);
            // The little wink lands on the hand-at-chin beat and resolves into an open smile.
            if (time < 5.47f) return Ease(Smile, Wink, 5.36f, 5.47f, time);
            return Ease(Wink, Smile, 5.51f, 5.69f, time);
        }

        private static Face Ease(Face from, Face to, float start, float end, float time) =>
            Face.Blend(from, to, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, time)));

        public void Dispose()
        {
            if (_head != null) _head.SetPropertyBlock(_original);
            _paint.Release();
            Destroy(_paint);
            Destroy(_brush);
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
