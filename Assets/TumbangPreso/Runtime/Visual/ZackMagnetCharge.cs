using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed class ZackMagnetCharge : MonoBehaviour
    {
        private ZackHeroKit _kit;
        private Carrier _carrier;
        private Slipper _shoe;
        private Renderer _source;
        private readonly LineRenderer[] _poles = new LineRenderer[2];
        private float _age;

        public static void Ensure(MeshFilter target, Slipper shoe, ZackHeroKit kit)
        {
            if (target == null || target.sharedMesh == null || shoe == null || shoe.Holder == null || kit == null
                || (!kit.IsOverchargeThrowActive && !kit.IsThunderstrikeActive)) return;
            if (target.GetComponentInChildren<ZackMagnetCharge>() != null) return;
            var go = new GameObject("MagnetChargePoles"); go.layer = target.gameObject.layer;
            go.transform.SetParent(target.transform, false);
            var bounds = target.sharedMesh.bounds; var size = bounds.size;
            var along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            var normal = size.y <= size.x && size.y <= size.z ? Vector3.up : size.x <= size.z ? Vector3.right : Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(along, normal)) > .5f) normal = Vector3.up;
            go.transform.localPosition = bounds.center; go.transform.localRotation = Quaternion.LookRotation(along, normal);
            go.transform.localScale = Vector3.one * Mathf.Max(size.x, size.y, size.z) / .46f;
            var charge = go.AddComponent<ZackMagnetCharge>();
            charge._kit = kit; charge._shoe = shoe; charge._carrier = shoe.Holder.GetComponent<Carrier>();
            charge._source = target.GetComponent<Renderer>();
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                var pole = new GameObject(i == 0 ? "ReceivingPole" : "ReturnPole"); pole.layer = go.layer;
                pole.transform.SetParent(go.transform, false);
                var line = pole.AddComponent<LineRenderer>(); line.useWorldSpace = false;
                line.positionCount = 6; line.widthMultiplier = .007f;
                line.SetPositions(new[] { new Vector3(side * .055f, .045f, -.105f), new Vector3(side * .095f, .035f, -.08f),
                    new Vector3(side * .105f, .04f, -.025f), new Vector3(side * .105f, .05f, .07f),
                    new Vector3(side * .08f, .055f, .085f), new Vector3(side * .045f, .055f, .105f) });
                line.numCornerVertices = 0; line.numCapVertices = 0;
                VfxMaterial.Ghost(line, Color.white, .3f); charge._poles[i] = line;
            }
        }

        private void Update()
        {
            if (_kit == null || (!_kit.IsOverchargeThrowActive && !_kit.IsThunderstrikeActive)
                || _carrier == null || _carrier.Held != _shoe || _carrier.GetComponent<HeroAbilitySystem>()?.Kit != _kit)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            _age += Time.deltaTime;
            for (int i = 0; i < 2; i++)
            {
                float pulse = .52f + .30f * Mathf.Pow(Mathf.Max(0, Mathf.Sin(_age * 6.5f + i * Mathf.PI)), 4);
                var colour = new Color(1, .9f, .22f, pulse * Mathf.Clamp01(_age / .12f));
                _poles[i].startColor = _poles[i].endColor = Color.white;
                _poles[i].sharedMaterial.color = colour;
                _poles[i].sharedMaterial.SetColor("_BaseColor", colour);
            }
        }

        private void LateUpdate()
        {
            if (_source == null) return;
            foreach (var pole in _poles) { pole.enabled = _source.enabled; pole.shadowCastingMode = _source.shadowCastingMode; }
        }
    }
}
