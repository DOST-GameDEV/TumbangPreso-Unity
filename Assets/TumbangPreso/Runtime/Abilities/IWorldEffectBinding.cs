namespace TumbangPreso.Abilities
{
    /// <summary>Reconnect kit ownership and lifetime after atomic world-field replacement.</summary>
    public interface IWorldEffectBinding
    {
        void RebindWorldEffects(CharacterMotor motor);
    }
}
