namespace Game.Core
{
    /// <summary>
    /// 타워가 명중 시 적에게 거는 부가 효과(둔화/지속피해). 기본값(default)은 '효과 없음'이라
    /// 기존 단일 데미지 타워(화살탑·대포탑·저격탑)는 이 구조체를 신경 쓰지 않아도 된다.
    /// </summary>
    public readonly struct TowerEffect
    {
        public float SlowFactor { get; }     // (0,1] — 0.5면 속도 절반. 둔화 미적용 시 의미 없음.
        public float SlowDuration { get; }
        public int PoisonDps { get; }
        public float PoisonDuration { get; }
        public int KillBonusGold { get; }    // 이 타워가 막타로 처치 시 지급되는 추가 골드(골드탑).

        public TowerEffect(float slowFactor, float slowDuration, int poisonDps, float poisonDuration,
            int killBonusGold = 0)
        {
            SlowFactor = slowFactor;
            SlowDuration = slowDuration;
            PoisonDps = poisonDps;
            PoisonDuration = poisonDuration;
            KillBonusGold = killBonusGold;
        }

        public bool AppliesSlow => SlowDuration > 0f && SlowFactor > 0f && SlowFactor < 1f;
        public bool AppliesPoison => PoisonDps > 0 && PoisonDuration > 0f;
        public bool AppliesKillBonus => KillBonusGold > 0;

        /// <summary>둔화 효과(빙결탑).</summary>
        public static TowerEffect Slow(float factor, float duration) =>
            new TowerEffect(factor, duration, 0, 0f);

        /// <summary>지속 피해 효과(독탑).</summary>
        public static TowerEffect Poison(int dps, float duration) =>
            new TowerEffect(1f, 0f, dps, duration);

        /// <summary>처치 보너스 골드 효과(골드탑). 막타로 적을 죽이면 추가 골드를 준다.</summary>
        public static TowerEffect GoldBonus(int bonus) =>
            new TowerEffect(1f, 0f, 0, 0f, bonus);
    }
}
