namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        // The right hand offers its held slipper; the free hand traces and withdraws.
        private static readonly Key[] BankLoadClip =
        {
            new Key(0, 0, 0, 0, 0, 0, 0),
            new Key(.08f, .04f, .04f, -.02f, .05f, -.08f, .05f),
            new Key(.18f, .12f, -.10f, .07f, -.40f, .28f, -.12f),
            new Key(.28f, .10f, -.08f, .04f, -.28f, .38f, -.20f, true),
            new Key(.42f, .05f, -.04f, .02f, -.12f, .16f, -.08f),
            new Key(.64f, 0, 0, 0, 0, 0, 0),
        };
    }
}
