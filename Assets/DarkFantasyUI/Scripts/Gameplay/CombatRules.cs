using System;
using System.Collections.Generic;

namespace Moonlit.UI
{
    [Serializable]
    public sealed class CombatStats
    {
        public double health = 80, attack = 10, speed = 1;
        public double criticalChance, criticalDamage, dodge, lifeSteal, regeneration, doubleChance, skillDamage;
        public CombatStats Copy() => (CombatStats)MemberwiseClone();
    }

    public sealed class CombatSkill
    {
        public int tier, variant, cooldown;
        public double heal, attackBoost, damage;
    }

    public sealed class CombatAction
    {
        public CombatSkill skill;
        public bool IsBasic => skill == null;
    }

    public sealed class CombatActorState
    {
        public readonly CombatStats stats;
        public readonly List<CombatSkill> skills;
        public double Health { get; private set; }
        public double AttackBoost { get; private set; }
        public int Turns { get; private set; }
        public bool Alive => Health > 0;
        public CombatActorState(CombatStats stats, List<CombatSkill> skills = null)
        {
            this.stats = stats.Copy();
            this.stats.health = Math.Max(1, stats.health);
            this.skills = skills ?? new List<CombatSkill>();
            Health = this.stats.health;
        }
        public void BeginTurn() { Turns++; }
        public double Heal(double amount)
        {
            double gain = Math.Min(stats.health - Health, Math.Max(0, amount));
            Health += gain;
            return gain;
        }
        public double Damage(double amount)
        {
            double loss = Math.Min(Health, Math.Max(0, amount));
            Health -= loss;
            return loss;
        }
        public void SetAttackBoost(double value) { AttackBoost = Math.Max(0, value); }
        public void Regenerate() { if (Alive) Heal(stats.health * CombatRules.Percent(stats.regeneration)); }
        // First activation is on the cooldown-th actor turn; cooldowns continue across waves and reset only with a new stage actor.
        public bool Ready(CombatSkill skill) => Turns > 0 && Turns % Math.Max(1, skill.cooldown) == 0;
    }

    public readonly struct CombatHit
    {
        public readonly double damage, healing;
        public readonly bool evaded, critical;
        public CombatHit(double damage, double healing, bool evaded, bool critical)
        { this.damage = damage; this.healing = healing; this.evaded = evaded; this.critical = critical; }
    }

    public static class CombatRules
    {
        public const int RoundsPerWave = 15;
        public const int WavesPerStage = 3;
        public static double Percent(double value) => Math.Min(1, Math.Max(0, value / 100));
        public static bool PlayerFirst(double playerSpeed, double enemySpeed, double coin)
            => playerSpeed == enemySpeed ? coin < .5 : playerSpeed > enemySpeed;
        public static int StageAfter(int stage, bool won) => Math.Max(1, stage + (won ? 1 : -1));
        public static bool RoundLimitLost(int completedRounds, bool enemyAlive)
            => enemyAlive && completedRounds >= RoundsPerWave;
        public static bool Roll(double percent, Func<double> random) => random() < Percent(percent);
        public static CombatHit Strike(CombatActorState attacker, CombatActorState target,
            double damage, bool skill, Func<double> random)
        {
            if (!attacker.Alive || !target.Alive) return new CombatHit(0, 0, false, false);
            if (Roll(target.stats.dodge, random)) return new CombatHit(0, 0, true, false);
            bool critical = !skill && Roll(attacker.stats.criticalChance, random);
            // Critical damage affix adds to the baseline 150%; skill values stay fixed except the skill-damage affix.
            double multiplier = skill ? 1 + Math.Max(0, attacker.stats.skillDamage) / 100 :
                critical ? 1.5 + Math.Max(0, attacker.stats.criticalDamage) / 100 : 1;
            double dealt = target.Damage(Math.Max(0, damage) * multiplier);
            double healed = attacker.Heal(dealt * Percent(attacker.stats.lifeSteal));
            return new CombatHit(dealt, healed, false, critical);
        }
        public static List<CombatAction> PlanTurn(CombatActorState actor, Func<double> random)
        {
            var result = new List<CombatAction>();
            if (!actor.Alive) return result;
            actor.BeginTurn();
            foreach (var skill in actor.skills)
                if (skill.variant == 0 && actor.Ready(skill)) result.Add(new CombatAction { skill = skill });
            result.Add(new CombatAction());
            if (Roll(actor.stats.doubleChance, random)) result.Add(new CombatAction());
            foreach (var skill in actor.skills)
                if (skill.variant > 0 && actor.Ready(skill)) result.Add(new CombatAction { skill = skill });
            return result;
        }
        public static CombatStats StageEnemy(int stage, int wave)
        {
            // Tunable, deterministic demo opponent curve; no server difficulty is claimed.
            double scale = Math.Pow(1.085, Math.Min(9999, Math.Max(0, stage - 1)));
            scale = Math.Min(scale, 1e100);
            return new CombatStats { health = (45 + 8 * Math.Max(0, wave - 1)) * scale,
                attack = (5 + Math.Max(0, wave - 1)) * scale,
                speed = Math.Max(1, stage) };
        }
    }

    // The Animator event consumes exactly one armed action. Duplicate/stale animation events cannot hit again.
    public sealed class CombatEventGate
    {
        Action action;
        public bool Pending => action != null;
        public void Arm(Action callback) { action = callback; }
        public void Cancel() { action = null; }
        public bool Consume()
        {
            var callback = action;
            action = null;
            if (callback == null) return false;
            callback();
            return true;
        }
    }
}
