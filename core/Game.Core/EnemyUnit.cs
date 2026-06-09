using System;

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

    /// <summary>한 적의 전투 상태(체력·속도·보상). 위치/이동은 PathTracker가 담당한다.</summary>
    public class EnemyUnit
    {
        public int Hp { get; private set; }
        public float Speed { get; }
        public int GoldReward { get; }
        public bool IsDead => Hp <= 0;

        public EnemyUnit(int maxHp, float speed, int goldReward)
        {
            if (maxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp), "최대 체력은 1 이상이어야 합니다.");
            Hp = maxHp;
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
    }
}
