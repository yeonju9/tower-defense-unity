using System;

namespace Game.Core
{
    /// <summary>쿨다운 타이머. 스킬처럼 '쓰면 일정 시간 다시 못 쓰는' 자원에 쓴다.</summary>
    public class Cooldown
    {
        public float Duration { get; }
        private float remaining;

        public bool IsReady => remaining <= 0f;
        public float Remaining => remaining < 0f ? 0f : remaining;

        public Cooldown(float duration)
        {
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "쿨다운은 0보다 커야 합니다.");
            Duration = duration;
            remaining = 0f; // 시작 즉시 사용 가능
        }

        /// <summary>사용 처리. 쿨다운을 가득 채운다.</summary>
        public void Trigger() => remaining = Duration;

        /// <summary>시간을 진행해 쿨다운을 회복시킨다.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "경과 시간은 음수일 수 없습니다.");
            if (remaining > 0f)
                remaining -= deltaTime;
        }
    }
}
