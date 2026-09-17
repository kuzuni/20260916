using System;
using UnityEngine;

namespace Moonlit.UI
{
    public sealed class CombatAnimationRelay : MonoBehaviour
    {
        readonly CombatEventGate gate = new CombatEventGate();
        int expectedKind;
        public bool Pending => gate.Pending;
        public void Arm(int kind, Action callback) { expectedKind = kind; gate.Arm(callback); }
        public void Cancel() { gate.Cancel(); }
        // Hook this method from the Animator clip event, using 0 basic / 1 buff / 2 weak / 3 strong.
        public void OnCombatImpact(int kind) { if (kind == expectedKind) gate.Consume(); }
        void OnDisable() { Cancel(); }
    }
}
