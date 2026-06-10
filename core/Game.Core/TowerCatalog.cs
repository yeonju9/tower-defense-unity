using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>업그레이드 한 단계의 비용과 효과 증가량(곡선이 산출하는 값).</summary>
    public readonly struct UpgradePlan
    {
        public int Cost { get; }
        public int AddedDamage { get; }
        public float AddedRange { get; }

        public UpgradePlan(int cost, int addedDamage, float addedRange)
        {
            Cost = cost;
            AddedDamage = addedDamage;
            AddedRange = addedRange;
        }
    }

    /// <summary>
    /// 타워 한 종류의 정의(밸런싱 값 + 비용 + 해금 시점 + 업그레이드 곡선).
    /// CreateUnit()으로 전투용 TowerUnit을 찍어낸다.
    /// vision의 "모든 타워는 사거리/발사간격/데미지/효과 4개 숫자로 정의된다"를 그대로 데이터화하고,
    /// 구매·판매·해금·강화를 위해 Cost·UnlockStageIndex·업그레이드 곡선을 더했다.
    /// </summary>
    public class TowerSpec
    {
        /// <summary>타워가 도달할 수 있는 최대 레벨(1에서 시작).</summary>
        public const int MaxLevel = 3;

        public string Name { get; }
        public int Cost { get; }
        public float Range { get; }
        public float FireInterval { get; }
        public int Damage { get; }
        public float SplashRadius { get; }
        public TowerEffect Effect { get; }
        public ChainSpec Chain { get; }

        /// <summary>이 타워가 열리는 스테이지 인덱스(0-based). 0이면 처음부터 사용 가능.</summary>
        public int UnlockStageIndex { get; }

        public TowerSpec(string name, int cost, float range, float fireInterval, int damage,
            float splashRadius = 0f, TowerEffect effect = default, ChainSpec chain = default,
            int unlockStageIndex = 0)
        {
            Name = name;
            Cost = cost;
            Range = range;
            FireInterval = fireInterval;
            Damage = damage;
            SplashRadius = splashRadius;
            Effect = effect;
            Chain = chain;
            UnlockStageIndex = unlockStageIndex;
        }

        public TowerUnit CreateUnit() =>
            new TowerUnit(Range, FireInterval, Damage, SplashRadius, Effect, Chain);

        /// <summary>해당 스테이지(0-based)에서 이 타워가 해금됐는지.</summary>
        public bool IsUnlockedAt(int stageIndex) => stageIndex >= UnlockStageIndex;

        /// <summary>
        /// 현재 레벨에서 다음 레벨로 올리는 업그레이드 계획. 곡선:
        /// 비용은 기본 비용의 currentLevel배(레벨이 오를수록 비쌈), 데미지는 기본 데미지의 절반(최소 1)씩 증가.
        /// 이미 최대 레벨이면 null(더 올릴 수 없음).
        /// </summary>
        public UpgradePlan? UpgradeFrom(int currentLevel)
        {
            if (currentLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(currentLevel), "레벨은 1 이상이어야 합니다.");
            if (currentLevel >= MaxLevel)
                return null;
            int cost = Cost * currentLevel;
            int addedDamage = Math.Max(1, Damage / 2);
            return new UpgradePlan(cost, addedDamage, addedRange: 0f);
        }
    }

    /// <summary>
    /// vision.md '타워 8종'의 스탯·효과·비용 정의. 세 역할군(딜러/광역/유틸)으로 나뉘며,
    /// 상성(빠른 적↔둔화 / 무리 적↔광역 / 튼튼한 적↔고데미지)을 수치로 구현한다.
    /// 추후 Unity ScriptableObject로 옮길 값.
    /// </summary>
    public static class TowerCatalog
    {
        /// <summary>1. 화살탑(딜러) — 균형 잡힌 단일 공격, 저렴. 초반 주력. 처음부터.</summary>
        public static TowerSpec Arrow =>
            new TowerSpec("화살탑", cost: 50, range: 3.0f, fireInterval: 1.0f, damage: 10,
                unlockStageIndex: 0);

        /// <summary>2. 대포탑(광역) — 착탄 지점 범위 폭발. 무리 적 카운터. 스테이지 2.</summary>
        public static TowerSpec Cannon =>
            new TowerSpec("대포탑", cost: 100, range: 2.5f, fireInterval: 1.5f, damage: 12,
                splashRadius: 1.5f, unlockStageIndex: 1);

        /// <summary>3. 빙결탑(유틸) — 데미지 낮지만 둔화. 빠른 적 카운터, 단독으론 약함. 스테이지 3.</summary>
        public static TowerSpec Frost =>
            new TowerSpec("빙결탑", cost: 80, range: 3.0f, fireInterval: 1.0f, damage: 4,
                effect: TowerEffect.Slow(0.5f, 2.0f), unlockStageIndex: 2);

        /// <summary>4. 저격탑(딜러) — 긴 사거리·고데미지·느린 발사. 튼튼한 적 카운터. 스테이지 4.</summary>
        public static TowerSpec Sniper =>
            new TowerSpec("저격탑", cost: 120, range: 6.0f, fireInterval: 2.5f, damage: 50,
                unlockStageIndex: 3);

        /// <summary>5. 연사탑(딜러) — 짧은 사거리·초고속 연사. 둔화탑과 조합해 빠른 적 처리. 스테이지 5.</summary>
        public static TowerSpec RapidFire =>
            new TowerSpec("연사탑", cost: 110, range: 2.0f, fireInterval: 0.3f, damage: 5,
                unlockStageIndex: 4);

        /// <summary>6. 독탑(유틸) — 시간당 지속 피해(중첩). 튼튼한 적 보조. 스테이지 6.</summary>
        public static TowerSpec Poison =>
            new TowerSpec("독탑", cost: 90, range: 3.0f, fireInterval: 1.5f, damage: 2,
                effect: TowerEffect.Poison(8, 4.0f), unlockStageIndex: 5);

        /// <summary>7. 번개탑(광역) — 한 발이 여러 적에게 체인. 일렬로 몰린 무리 적. 스테이지 7.</summary>
        public static TowerSpec Lightning =>
            new TowerSpec("번개탑", cost: 130, range: 3.5f, fireInterval: 1.2f, damage: 14,
                chain: new ChainSpec(maxTargets: 3, jumpRange: 1.5f, falloff: 0.6f), unlockStageIndex: 6);

        /// <summary>8. 골드탑(유틸) — 공격 약하지만 처치 시 추가 골드. 안전한 구간에 배치. 스테이지 8.</summary>
        public static TowerSpec Gold =>
            new TowerSpec("골드탑", cost: 100, range: 2.5f, fireInterval: 1.5f, damage: 6,
                effect: TowerEffect.GoldBonus(5), unlockStageIndex: 7);

        /// <summary>타워 8종을 해금 순서대로 반환(빌드 메뉴용).</summary>
        public static IReadOnlyList<TowerSpec> All() =>
            new List<TowerSpec> { Arrow, Cannon, Frost, Sniper, RapidFire, Poison, Lightning, Gold };

        /// <summary>해당 스테이지(0-based)까지 해금된 타워만 순서대로 반환(빌드 메뉴 필터용).</summary>
        public static IReadOnlyList<TowerSpec> UnlockedAt(int stageIndex)
        {
            if (stageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(stageIndex), "스테이지 인덱스는 음수일 수 없습니다.");
            var unlocked = new List<TowerSpec>();
            foreach (var spec in All())
                if (spec.IsUnlockedAt(stageIndex))
                    unlocked.Add(spec);
            return unlocked;
        }
    }
}
