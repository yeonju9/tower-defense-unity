using System;

namespace Game.Core
{
    /// <summary>한 타워의 발사 타이밍(쿨다운)과 전투 수치(사거리·데미지) 보유.</summary>
    public class TowerUnit
    {
        public float Range { get; private set; }
        public float FireInterval { get; }
        public int Damage { get; private set; }

        /// <summary>착탄 지점 범위 피해의 반경. 0이면 단일 타깃(기존 화살탑·저격탑 등).</summary>
        public float SplashRadius { get; }

        /// <summary>범위 피해 타워인지(대포탑 등).</summary>
        public bool IsSplash => SplashRadius > 0f;

        /// <summary>업그레이드 레벨. 1부터 시작하며 Upgrade마다 1씩 오른다.</summary>
        public int Level { get; private set; } = 1;

        private float cooldownRemaining;

        /// <summary>쿨다운이 0 이하이면 발사 가능.</summary>
        public bool CanFire => cooldownRemaining <= 0f;

        public TowerUnit(float range, float fireInterval, int damage, float splashRadius = 0f)
        {
            if (range <= 0f)
                throw new ArgumentOutOfRangeException(nameof(range), "사거리는 0보다 커야 합니다.");
            if (fireInterval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fireInterval), "발사 간격은 0보다 커야 합니다.");
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "데미지는 음수일 수 없습니다.");
            if (splashRadius < 0f)
                throw new ArgumentOutOfRangeException(nameof(splashRadius), "범위 반경은 음수일 수 없습니다.");
            Range = range;
            FireInterval = fireInterval;
            Damage = damage;
            SplashRadius = splashRadius;
            cooldownRemaining = 0f; // 시작 즉시 발사 가능
        }

        /// <summary>경과 시간만큼 쿨다운을 줄인다.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "경과 시간은 음수일 수 없습니다.");
            if (cooldownRemaining > 0f)
                cooldownRemaining -= deltaTime;
        }

        /// <summary>발사 직후 호출. 쿨다운을 발사 간격만큼 리셋한다.</summary>
        public void OnFired()
        {
            cooldownRemaining = FireInterval;
        }

        /// <summary>데미지·사거리를 올리고 레벨을 1 증가시킨다. 증가량은 음수일 수 없다.</summary>
        public void Upgrade(int addedDamage, float addedRange = 0f)
        {
            if (addedDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(addedDamage), "추가 데미지는 음수일 수 없습니다.");
            if (addedRange < 0f)
                throw new ArgumentOutOfRangeException(nameof(addedRange), "추가 사거리는 음수일 수 없습니다.");
            Damage += addedDamage;
            Range += addedRange;
            Level++;
        }
    }
}
