using System;

namespace Game.Core
{
    /// <summary>
    /// 스테이지 클리어 시 남은 라이프로 별점(1~3)을 매긴다(vision.md: "라이프를 얼마나 지켰는가").
    /// - 라이프를 모두 지킴 → 3성
    /// - 절반 이상 지킴 → 2성
    /// - 그 외(살아서 클리어) → 1성
    /// </summary>
    public static class StarRating
    {
        public static int FromLives(int livesRemaining, int startingLives)
        {
            if (startingLives <= 0)
                throw new ArgumentOutOfRangeException(nameof(startingLives), "시작 라이프는 1 이상이어야 합니다.");

            // 방어적 클램프(잘못된 입력에도 1~3 범위를 보장).
            if (livesRemaining < 0) livesRemaining = 0;
            if (livesRemaining > startingLives) livesRemaining = startingLives;

            if (livesRemaining == startingLives)
                return 3;
            if (livesRemaining * 2 >= startingLives) // 절반 이상
                return 2;
            return 1;
        }
    }
}
