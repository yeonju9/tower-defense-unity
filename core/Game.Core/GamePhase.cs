namespace Game.Core
{
    public enum Phase
    {
        Ready,    // 배치 준비 단계
        Playing,  // 전투 진행 중
        Won,      // 승리(흡수 상태)
        Lost      // 패배(흡수 상태)
    }

    /// <summary>
    /// 판 전체 상태기. Ready→Playing→(Won|Lost) 전이 규칙을 한 곳에 모은다.
    /// Won/Lost는 흡수 상태라 더 이상 전이하지 않는다.
    /// </summary>
    public class GamePhase
    {
        public Phase Current { get; private set; } = Phase.Ready;

        public bool IsOver => Current == Phase.Won || Current == Phase.Lost;

        /// <summary>웨이브 시작. Ready에서만 Playing으로 전이한다.</summary>
        public void StartWave()
        {
            if (Current == Phase.Ready)
                Current = Phase.Playing;
        }

        /// <summary>라이프 소진. Playing에서만 Lost로 전이한다.</summary>
        public void NotifyLifeDepleted()
        {
            if (Current == Phase.Playing)
                Current = Phase.Lost;
        }

        /// <summary>전멸+스폰종료. Playing에서만 Won으로 전이한다.</summary>
        public void NotifyAllCleared()
        {
            if (Current == Phase.Playing)
                Current = Phase.Won;
        }
    }
}
