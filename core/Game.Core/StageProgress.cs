using System;

namespace Game.Core
{
    /// <summary>
    /// 스테이지 해금 규칙(게임 규칙). SaveStore(순수 저장소)의 클리어 기록을 읽어
    /// "이전 스테이지를 클리어해야 다음이 열린다"는 진행 흐름을 판정한다.
    /// 저장소(데이터)와 해금 규칙(로직)을 분리해 둔다.
    /// </summary>
    public class StageProgress
    {
        private readonly SaveStore store;

        public StageProgress(SaveStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>해당 스테이지가 도전 가능한지. 0번은 항상 열려 있고, N번은 N-1 클리어 시 열린다.</summary>
        public bool IsUnlocked(int stageIndex)
        {
            if (stageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(stageIndex), "스테이지 인덱스는 음수일 수 없습니다.");
            if (stageIndex == 0)
                return true;
            return store.IsCleared(stageIndex - 1);
        }

        /// <summary>
        /// 다음에 도전할 스테이지(해금됐지만 아직 클리어하지 않은 첫 스테이지).
        /// 모두 클리어했다면 마지막 스테이지를 가리킨다.
        /// </summary>
        public int NextStageToPlay(int totalStages)
        {
            if (totalStages <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalStages), "전체 스테이지 수는 1 이상이어야 합니다.");
            for (int i = 0; i < totalStages; i++)
            {
                if (IsUnlocked(i) && !store.IsCleared(i))
                    return i;
            }
            return totalStages - 1; // 전부 클리어 → 마지막
        }
    }
}
