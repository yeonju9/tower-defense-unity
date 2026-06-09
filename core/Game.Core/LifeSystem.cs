using System;

namespace Game.Core
{
    /// <summary>플레이어 라이프 보유·감소·소진 판정.</summary>
    public class LifeSystem
    {
        public int Lives { get; private set; }
        public bool IsDepleted => Lives <= 0;

        public LifeSystem(int startingLives)
        {
            if (startingLives <= 0)
                throw new ArgumentOutOfRangeException(nameof(startingLives), "시작 라이프는 1 이상이어야 합니다.");
            Lives = startingLives;
        }

        /// <summary>라이프를 감소시킨다. 0 미만으로는 내려가지 않는다.</summary>
        public void LoseLife(int amount = 1)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "감소량은 음수일 수 없습니다.");
            Lives -= amount;
            if (Lives < 0)
                Lives = 0;
        }
    }
}
