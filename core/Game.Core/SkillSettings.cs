namespace Game.Core
{
    /// <summary>
    /// 스킬 3종의 쿨다운·효과 수치. vision.md 기본값을 따르며, 테스트나 밸런싱에서 바꿀 수 있다.
    /// </summary>
    public class SkillSettings
    {
        // 운석 낙하: 지정 범위에 즉시 광역 피해.
        public float MeteorCooldown { get; set; } = 30f;
        public float MeteorRadius { get; set; } = 1.5f;
        public int MeteorDamage { get; set; } = 100;

        // 시간 정지: 모든 적 일정 시간 정지.
        public float TimeStopCooldown { get; set; } = 45f;
        public float TimeStopDuration { get; set; } = 3f;

        // 골드 러시: 일정 시간 처치 골드 배수.
        public float GoldRushCooldown { get; set; } = 60f;
        public float GoldRushDuration { get; set; } = 10f;
        public int GoldRushMultiplier { get; set; } = 2;

        public static SkillSettings Default => new SkillSettings();
    }
}
