using System;

namespace Game.Core
{
    /// <summary>
    /// 수동 웨이브 시작 + 조기 시작 보너스를 모델링하는 준비 카운트다운.
    /// 준비 시간이 다 지나기 전에 플레이어가 시작하면, 남은 시간에 비례한 보너스 골드를 준다.
    /// (멀티 웨이브에서 진가를 발휘하며, GameLoop 연동은 멀티 웨이브 도입 시 추가한다.)
    /// </summary>
    public class WaveStartTimer
    {
        public float PrepDuration { get; }
        public int BonusPerSecond { get; }

        public float Elapsed { get; private set; }
        public bool Started { get; private set; }

        /// <summary>아직 남은 준비 시간(0 미만으로 내려가지 않음).</summary>
        public float Remaining => Math.Max(0f, PrepDuration - Elapsed);

        /// <summary>준비 시간이 모두 소진됨(이 시점에 시작하면 보너스 0).</summary>
        public bool Expired => Elapsed >= PrepDuration;

        public WaveStartTimer(float prepDuration, int bonusPerSecond)
        {
            if (prepDuration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(prepDuration), "준비 시간은 0보다 커야 합니다.");
            if (bonusPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(bonusPerSecond), "초당 보너스는 음수일 수 없습니다.");
            PrepDuration = prepDuration;
            BonusPerSecond = bonusPerSecond;
            Elapsed = 0f;
            Started = false;
        }

        /// <summary>준비 시간을 경과시킨다. 이미 시작했으면 무시한다.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "경과 시간은 음수일 수 없습니다.");
            if (Started)
                return;
            Elapsed += deltaTime;
        }

        /// <summary>
        /// 지금 웨이브를 시작한다. 남은 준비 시간 × 초당 보너스(내림)를 보너스 골드로 반환한다.
        /// 이미 시작했으면 0을 반환한다(중복 시작 방지).
        /// </summary>
        public int Start()
        {
            if (Started)
                return 0;
            Started = true;
            return (int)(Remaining * BonusPerSecond);
        }
    }
}
