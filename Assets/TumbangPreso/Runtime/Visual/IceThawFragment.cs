using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed class IceThawFragment : MonoBehaviour
    {
        public Vector3 Velocity,Spin;
        private float _age;
        private Material _material;
        private void Start() => _material=GetComponent<Renderer>().sharedMaterial;
        private void Update()
        {
            float dt=Time.deltaTime;_age+=dt;
            Velocity+=Vector3.down*(3*dt);
            transform.position+=Velocity*dt;transform.Rotate(Spin*dt,Space.Self);
            var color=_material.color;color.a=.62f*Mathf.Clamp01(1-_age/.65f);_material.color=color;
        }
    }
}
