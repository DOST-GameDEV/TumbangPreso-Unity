namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        // Aiming gesture only. No hit kick is played when acquisition is cancelled.
        private static readonly Key[] ClosedCircuitClip =
        {
            new Key(0, 0, 0, 0, 0, 0, 0),
            new Key(.08f, .015f, 0, .01f, .12f, .05f, -.09f),
            new Key(.18f, .025f, 0, .015f, -.38f, .08f, -.12f, true),
            new Key(.36f, .02f, 0, .01f, -.36f, .075f, -.10f),
            new Key(.48f, .01f, 0, 0, -.16f, .035f, -.04f),
            new Key(.64f, 0, 0, 0, 0, 0, 0),
        };
    }
}
