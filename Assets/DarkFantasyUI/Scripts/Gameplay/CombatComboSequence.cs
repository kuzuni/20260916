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
        readonly float completionTime;
        float elapsed;
        public int ResolvedHits { get; private set; }
        public int HitCount => hitTimes.Length;
        public bool Pending { get; private set; } = true;
        public bool Cancelled { get; private set; }

        public CombatComboSequence(double totalDamage, float[] times, Func<bool> canContinue,
            Action<int, double> impact, Action<bool> stopped = null, float completionTime = -1)
        {
            if (times == null || times.Length == 0 || times[0] < 0 || float.IsNaN(times[0]) || float.IsInfinity(times[0]))
                throw new ArgumentException("A combo needs a finite, nonnegative first impact time.", nameof(times));
            for (int i = 1; i < times.Length; i++)
                if (float.IsNaN(times[i]) || float.IsInfinity(times[i]) || times[i] <= times[i - 1])
                    throw new ArgumentException("Combo impacts must have strictly increasing finite times.", nameof(times));
            hitTimes = (float[])times.Clone();
            this.completionTime = completionTime < 0 ? hitTimes[hitTimes.Length - 1] : completionTime;
            if (float.IsNaN(this.completionTime) || float.IsInfinity(this.completionTime) || this.completionTime < hitTimes[hitTimes.Length - 1])
                throw new ArgumentException("Completion cannot precede the last impact.", nameof(completionTime));
            this.totalDamage = Math.Max(0, totalDamage);
            this.canContinue = canContinue ?? throw new ArgumentNullException(nameof(canContinue));
            this.impact = impact ?? throw new ArgumentNullException(nameof(impact));
            this.stopped = stopped;
        }
        // Animator events authorize the action; visual contact times remain relative to motion start.
        public static float[] TimesAfterEvent(float[] motionTimes, float eventTime)
        {
            if (motionTimes == null) throw new ArgumentNullException(nameof(motionTimes));
            var relative = new float[motionTimes.Length];
            for (int i = 0; i < relative.Length; i++)
            {
                if (motionTimes[i] < eventTime - .00001f)
                    throw new ArgumentException("A contact cannot precede its authorizing Animator event.", nameof(motionTimes));
                relative[i] = Math.Max(0, motionTimes[i] - eventTime);
            }
            return relative;
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
            }
            if (Pending && ResolvedHits == hitTimes.Length && elapsed >= completionTime) Finish(false);
        }
        public void Cancel() { if (Pending) Finish(true); }
        void Finish(bool cancelled)
        {
            Pending = false; Cancelled = cancelled;
            stopped?.Invoke(cancelled);
        }
    }
}
