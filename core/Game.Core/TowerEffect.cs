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

        public TowerEffect(float slowFactor, float slowDuration, int poisonDps, float poisonDuration)
        {
            SlowFactor = slowFactor;
            SlowDuration = slowDuration;
            PoisonDps = poisonDps;
            PoisonDuration = poisonDuration;
        }

        public bool AppliesSlow => SlowDuration > 0f && SlowFactor > 0f && SlowFactor < 1f;
        public bool AppliesPoison => PoisonDps > 0 && PoisonDuration > 0f;

        /// <summary>둔화 효과(빙결탑).</summary>
        public static TowerEffect Slow(float factor, float duration) =>
            new TowerEffect(factor, duration, 0, 0f);

        /// <summary>지속 피해 효과(독탑).</summary>
        public static TowerEffect Poison(int dps, float duration) =>
            new TowerEffect(1f, 0f, dps, duration);
    }
}
