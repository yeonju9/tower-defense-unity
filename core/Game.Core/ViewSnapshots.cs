namespace Game.Core
{
    /// <summary>
    /// 화면 렌더링을 위한 적 한 마리의 읽기 전용 스냅샷.
    /// Unity 레이어가 매 프레임 GameLoop에서 받아 GameObject를 배치·갱신한다.
    /// Id는 프레임 간 같은 적을 같은 GameObject에 매핑하기 위한 안정적 식별자다.
    /// </summary>
    public readonly struct EnemyView
    {
        public int Id { get; }
        public Vec2 Position { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        public bool IsSlowed { get; }
        public bool IsPoisoned { get; }
        public bool IsBoss { get; }   // 보스 스프라이트·크기·등장 연출 구분용

        public EnemyView(int id, Vec2 position, int hp, int maxHp, bool isSlowed, bool isPoisoned,
            bool isBoss)
        {
            Id = id;
            Position = position;
            Hp = hp;
            MaxHp = maxHp;
            IsSlowed = isSlowed;
            IsPoisoned = isPoisoned;
            IsBoss = isBoss;
        }
    }

    /// <summary>화면 렌더링을 위한 타워 한 대의 읽기 전용 스냅샷.</summary>
    public readonly struct TowerView
    {
        public Vec2 Position { get; }
        public int Level { get; }
        public float Range { get; }
        public bool IsSplash { get; }
        public bool IsChain { get; }            // 번개탑 스프라이트·체인 이펙트 구분용
        public TargetingMode Targeting { get; } // 선택 패널에 현재 조준 정책 표시용

        public TowerView(Vec2 position, int level, float range, bool isSplash, bool isChain,
            TargetingMode targeting)
        {
            Position = position;
            Level = level;
            Range = range;
            IsSplash = isSplash;
            IsChain = isChain;
            Targeting = targeting;
        }
    }
}
