namespace Game.Core
{
    /// <summary>
    /// vision.md '적 6종'의 스탯 정의(밸런싱 값). 능력을 복잡하게 만들지 않고
    /// 스탯 분배(체력/속도/보상)와 보스 플래그만으로 개성을 준다.
    /// 추후 Unity ScriptableObject로 옮길 값이며, 여기서는 코드로 고정해 TDD로 상성을 검증한다.
    /// </summary>
    public static class EnemyCatalog
    {
        /// <summary>일반 보병: 평균적인 체력·속도. 아무 타워나 상대 가능(기본).</summary>
        public static EnemySpec Infantry => new EnemySpec(maxHp: 40, speed: 1.0f, goldReward: 5);

        /// <summary>돌격병: 빠른 이동, 낮은 체력. 빙결탑+연사탑으로 카운터.</summary>
        public static EnemySpec Charger => new EnemySpec(maxHp: 20, speed: 2.0f, goldReward: 6);

        /// <summary>군집 벌레: 약하지만 떼로 등장. 대포탑·번개탑(광역)으로 카운터.</summary>
        public static EnemySpec SwarmBug => new EnemySpec(maxHp: 12, speed: 1.2f, goldReward: 2);

        /// <summary>중장갑병: 매우 높은 체력, 느림. 저격탑+독탑으로 카운터.</summary>
        public static EnemySpec HeavyArmor => new EnemySpec(maxHp: 150, speed: 0.5f, goldReward: 12);

        /// <summary>질주 기병: 빠르면서 체력도 어느 정도(복합). 단일 카운터로 안 막혀 조합 강제.</summary>
        public static EnemySpec Cavalry => new EnemySpec(maxHp: 70, speed: 1.8f, goldReward: 14);

        /// <summary>미니 보스: 웨이브 끝에 등장, 압도적 체력. 모든 화력+스킬 총동원.</summary>
        public static EnemySpec MiniBoss => new EnemySpec(maxHp: 600, speed: 0.6f, goldReward: 60, isBoss: true);
    }
}
