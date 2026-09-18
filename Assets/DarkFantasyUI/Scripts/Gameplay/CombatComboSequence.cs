using System;

namespace Moonlit.UI
{
    // Fixed skill totals are distributed over authored impacts, without rounding each small hit upward.
    // The animation relay creates this sequence once; it cannot start or restart itself.
    public sealed class CombatComboSequence
    {
        readonly float[] hitTimes;
        readonly double totalDamage;
        readonly Func<bool> canContinue;
        readonly Action<int, double> impact;
        readonly Action<bool> stopped;
        float elapsed;
        public int ResolvedHits { get; private set; }
        public int HitCount => hitTimes.Length;
        public bool Pending { get; private set; } = true;
        public bool Cancelled { get; private set; }

        public CombatComboSequence(double totalDamage, float[] times, Func<bool> canContinue,
            Action<int, double> impact, Action<bool> stopped = null)
        {
            if (times == null || times.Length == 0 || times[0] != 0)
                throw new ArgumentException("A combo needs its first impact at zero.", nameof(times));
            for (int i = 1; i < times.Length; i++)
                if (float.IsNaN(times[i]) || float.IsInfinity(times[i]) || times[i] <= times[i - 1])
                    throw new ArgumentException("Combo impacts must have strictly increasing finite times.", nameof(times));
            hitTimes = (float[])times.Clone();
            this.totalDamage = Math.Max(0, totalDamage);
            this.canContinue = canContinue ?? throw new ArgumentNullException(nameof(canContinue));
            this.impact = impact ?? throw new ArgumentNullException(nameof(impact));
            this.stopped = stopped;
        }
        public static double DamageForHit(double total, int hitIndex, int hitCount)
        {
            if (hitCount < 1 || hitIndex < 0 || hitIndex >= hitCount) throw new ArgumentOutOfRangeException();
            double share = Math.Max(0, total) / hitCount;
            return hitIndex == hitCount - 1 ? Math.Max(0, total) - share * (hitCount - 1) : share;
        }
        public void Advance(float deltaTime)
        {
            if (!Pending) return;
            if (!canContinue()) { Cancel(); return; }
            elapsed += Math.Max(0, deltaTime);
            while (Pending && ResolvedHits < hitTimes.Length && elapsed >= hitTimes[ResolvedHits])
            {
                if (!canContinue()) { Cancel(); return; }
                int index = ResolvedHits++;
                impact(index, DamageForHit(totalDamage, index, hitTimes.Length));
                if (!Pending) return;
                if (!canContinue() && ResolvedHits < hitTimes.Length) { Cancel(); return; }
                if (ResolvedHits == hitTimes.Length) Finish(false);
            }
        }
        public void Cancel() { if (Pending) Finish(true); }
        void Finish(bool cancelled)
        {
            Pending = false; Cancelled = cancelled;
            stopped?.Invoke(cancelled);
        }
    }
}
