namespace ArcherArcade.Tweening
{
    /// <summary>Reference to a running tween. Stale handles (tween finished or killed) are safely ignored.</summary>
    public readonly struct TweenHandle
    {
        public readonly int Slot;
        public readonly int Version;

        public TweenHandle(int slot, int version)
        {
            Slot = slot;
            Version = version;
        }

        public bool IsValid => Version != 0;
        public bool IsRunning => Tween.IsRunning(this);
        public void Kill(bool complete = false) => Tween.Kill(this, complete);
    }
}
