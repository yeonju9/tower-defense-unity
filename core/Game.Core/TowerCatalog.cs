using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 타워 한 종류의 정의(밸런싱 값 + 비용). CreateUnit()으로 전투용 TowerUnit을 찍어낸다.
    /// vision의 "모든 타워는 사거리/발사간격/데미지/효과 4개 숫자로 정의된다"를 그대로 데이터화하고,
    /// 구매·판매를 위해 Cost를 더했다.
    /// </summary>
    public class TowerSpec
    {
        public string Name { get; }
        public int Cost { get; }
        public float Range { get; }
        public float FireInterval { get; }
        public int Damage { get; }
        public float SplashRadius { get; }
        public TowerEffect Effect { get; }
        public ChainSpec Chain { get; }

        public TowerSpec(string name, int cost, float range, float fireInterval, int damage,
            float splashRadius = 0f, TowerEffect effect = default, ChainSpec chain = default)
        {
            Name = name;
            Cost = cost;
            Range = range;
            FireInterval = fireInterval;
            Damage = damage;
            SplashRadius = splashRadius;
            Effect = effect;
            Chain = chain;
        }

        public TowerUnit CreateUnit() =>
            new TowerUnit(Range, FireInterval, Damage, SplashRadius, Effect, Chain);
    }

    /// <summary>
    /// vision.md '타워 8종'의 스탯·효과·비용 정의. 세 역할군(딜러/광역/유틸)으로 나뉘며,
    /// 상성(빠른 적↔둔화 / 무리 적↔광역 / 튼튼한 적↔고데미지)을 수치로 구현한다.
    /// 추후 Unity ScriptableObject로 옮길 값.
    /// </summary>
    public static class TowerCatalog
    {
        /// <summary>1. 화살탑(딜러) — 균형 잡힌 단일 공격, 저렴. 초반 주력.</summary>
        public static TowerSpec Arrow =>
            new TowerSpec("화살탑", cost: 50, range: 3.0f, fireInterval: 1.0f, damage: 10);

        /// <summary>2. 대포탑(광역) — 착탄 지점 범위 폭발. 무리 적 카운터.</summary>
        public static TowerSpec Cannon =>
            new TowerSpec("대포탑", cost: 100, range: 2.5f, fireInterval: 1.5f, damage: 12,
                splashRadius: 1.5f);

        /// <summary>3. 빙결탑(유틸) — 데미지 낮지만 둔화. 빠른 적 카운터, 단독으론 약함.</summary>
        public static TowerSpec Frost =>
            new TowerSpec("빙결탑", cost: 80, range: 3.0f, fireInterval: 1.0f, damage: 4,
                effect: TowerEffect.Slow(0.5f, 2.0f));

        /// <summary>4. 저격탑(딜러) — 긴 사거리·고데미지·느린 발사. 튼튼한 적 카운터.</summary>
        public static TowerSpec Sniper =>
            new TowerSpec("저격탑", cost: 120, range: 6.0f, fireInterval: 2.5f, damage: 50);

        /// <summary>5. 연사탑(딜러) — 짧은 사거리·초고속 연사. 둔화탑과 조합해 빠른 적 처리.</summary>
        public static TowerSpec RapidFire =>
            new TowerSpec("연사탑", cost: 110, range: 2.0f, fireInterval: 0.3f, damage: 5);

        /// <summary>6. 독탑(유틸) — 시간당 지속 피해(중첩). 튼튼한 적 보조.</summary>
        public static TowerSpec Poison =>
            new TowerSpec("독탑", cost: 90, range: 3.0f, fireInterval: 1.5f, damage: 2,
                effect: TowerEffect.Poison(8, 4.0f));

        /// <summary>7. 번개탑(광역) — 한 발이 여러 적에게 체인. 일렬로 몰린 무리 적.</summary>
        public static TowerSpec Lightning =>
            new TowerSpec("번개탑", cost: 130, range: 3.5f, fireInterval: 1.2f, damage: 14,
                chain: new ChainSpec(maxTargets: 3, jumpRange: 1.5f, falloff: 0.6f));

        /// <summary>8. 골드탑(유틸) — 공격 약하지만 처치 시 추가 골드. 안전한 구간에 배치.</summary>
        public static TowerSpec Gold =>
            new TowerSpec("골드탑", cost: 100, range: 2.5f, fireInterval: 1.5f, damage: 6,
                effect: TowerEffect.GoldBonus(5));

        /// <summary>타워 8종을 해금 순서대로 반환(빌드 메뉴용).</summary>
        public static IReadOnlyList<TowerSpec> All() =>
            new List<TowerSpec> { Arrow, Cannon, Frost, Sniper, RapidFire, Poison, Lightning, Gold };
    }
}
