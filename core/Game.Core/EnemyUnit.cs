using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>적 한 종류의 스탯 정의(밸런싱 값). 추후 Unity ScriptableObject로 옮길 값.</summary>
    public readonly struct EnemySpec
    {
        public int MaxHp { get; }
        public float Speed { get; }
        public int GoldReward { get; }

        public EnemySpec(int maxHp, float speed, int goldReward)
        {
            if (maxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp), "최대 체력은 1 이상이어야 합니다.");
            if (speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), "이동 속도는 0보다 커야 합니다.");
            if (goldReward < 0)
                throw new ArgumentOutOfRangeException(nameof(goldReward), "보상은 음수일 수 없습니다.");
            MaxHp = maxHp;
            Speed = speed;
            GoldReward = goldReward;
        }
    }

    /// <summary>한 적의 전투 상태(체력·속도·보상·상태이상). 위치/이동은 PathTracker가 담당한다.</summary>
    public class EnemyUnit
    {
        private struct PoisonStack
        {
            public int Dps;
            public float Remaining;
        }

        public int Hp { get; private set; }
        public int MaxHp { get; }
        public float Speed { get; }
        public int GoldReward { get; }
        public bool IsDead => Hp <= 0;

        // --- 둔화(빙결탑) 상태 ---
        private float slowFactor = 1f;   // 1 = 둔화 없음. 작을수록 강한 둔화.
        private float slowRemaining;

        // --- 지속 피해(독탑) 상태 ---
        private readonly List<PoisonStack> poisonStacks = new List<PoisonStack>();
        private float poisonBuffer;      // 정수 체력에 맞춰 누적되는 소수 피해

        /// <summary>둔화가 반영된 실제 이동 속도. 이동 계산은 이 값을 쓴다.</summary>
        public float EffectiveSpeed => Speed * slowFactor;

        /// <summary>현재 둔화 상태인지(테스트·UI용).</summary>
        public bool IsSlowed => slowRemaining > 0f && slowFactor < 1f;

        /// <summary>현재 중독 상태인지(테스트·UI용).</summary>
        public bool IsPoisoned => poisonStacks.Count > 0;

        public EnemyUnit(int maxHp, float speed, int goldReward)
        {
            if (maxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp), "최대 체력은 1 이상이어야 합니다.");
            Hp = maxHp;
            MaxHp = maxHp;
            Speed = speed;
            GoldReward = goldReward;
        }

        public EnemyUnit(EnemySpec spec)
            : this(spec.MaxHp, spec.Speed, spec.GoldReward) { }

        /// <summary>피해를 입는다. 체력은 0 미만으로 내려가지 않으며, 이미 죽었으면 무시한다.</summary>
        public void TakeDamage(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "피해량은 음수일 수 없습니다.");
            if (IsDead)
                return;
            Hp -= amount;
            if (Hp < 0)
                Hp = 0;
        }

        /// <summary>
        /// 둔화를 적용한다. factor는 (0,1] 범위(0.5면 속도 절반). 더 강한 둔화가 우선하고,
        /// 지속시간은 더 긴 쪽으로 갱신된다.
        /// </summary>
        public void ApplySlow(float factor, float duration)
        {
            if (factor <= 0f || factor > 1f)
                throw new ArgumentOutOfRangeException(nameof(factor), "둔화 계수는 (0,1] 범위여야 합니다.");
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "둔화 지속시간은 0보다 커야 합니다.");
            if (IsDead)
                return;

            if (!IsSlowed || factor < slowFactor)
                slowFactor = factor;                 // 더 강한 둔화가 이김
            slowRemaining = Math.Max(slowRemaining, duration); // 더 긴 지속이 이김
        }

        /// <summary>지속 피해(독)를 한 스택 추가한다. 여러 번 걸면 중첩된다.</summary>
        public void ApplyPoison(int dps, float duration)
        {
            if (dps <= 0)
                throw new ArgumentOutOfRangeException(nameof(dps), "초당 피해는 1 이상이어야 합니다.");
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "독 지속시간은 0보다 커야 합니다.");
            if (IsDead)
                return;
            poisonStacks.Add(new PoisonStack { Dps = dps, Remaining = duration });
        }

        /// <summary>한 스텝 동안 상태이상을 진행한다(독 피해 적용, 둔화·독 지속시간 감소).</summary>
        public void TickStatus(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "경과 시간은 음수일 수 없습니다.");
            if (IsDead)
                return;

            // 독: 활성 스택의 dps 합 × dt를 누적해 정수 단위로 피해.
            if (poisonStacks.Count > 0)
            {
                int totalDps = 0;
                foreach (var s in poisonStacks)
                    totalDps += s.Dps;

                poisonBuffer += totalDps * deltaTime;
                int applied = (int)poisonBuffer;
                if (applied > 0)
                {
                    poisonBuffer -= applied;
                    TakeDamage(applied);
                }

                for (int i = poisonStacks.Count - 1; i >= 0; i--)
                {
                    var s = poisonStacks[i];
                    s.Remaining -= deltaTime;
                    if (s.Remaining <= 0f)
                        poisonStacks.RemoveAt(i);
                    else
                        poisonStacks[i] = s;
                }
            }

            // 둔화: 지속시간 감소, 만료되면 원래 속도로 복귀.
            if (slowRemaining > 0f)
            {
                slowRemaining -= deltaTime;
                if (slowRemaining <= 0f)
                {
                    slowRemaining = 0f;
                    slowFactor = 1f;
                }
            }
        }
    }
}
