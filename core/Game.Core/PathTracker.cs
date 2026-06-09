using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// waypoint 배열 위에서 한 유닛의 진행 상태를 관리한다.
    /// 코너를 넘어가면 다음 구간으로 자연스럽게 이어지며, 마지막을 넘으면 끝점에 고정된다.
    /// Progress(누적 이동 거리)는 TargetSelector의 '얼마나 전진했나' 판단에도 쓰인다.
    /// </summary>
    public class PathTracker
    {
        private readonly IReadOnlyList<Vec2> waypoints;

        private int segmentIndex;        // 현재 향하는 목표 waypoint의 직전 구간 시작 인덱스
        public Vec2 Position { get; private set; }
        public bool ReachedEnd { get; private set; }

        /// <summary>출발점부터 누적 이동한 경로 거리(전진도). 클수록 목표에 가깝다.</summary>
        public float Progress { get; private set; }

        public PathTracker(IReadOnlyList<Vec2> waypoints)
        {
            if (waypoints == null)
                throw new ArgumentNullException(nameof(waypoints));
            if (waypoints.Count < 2)
                throw new ArgumentException("경로는 최소 2개의 waypoint가 필요합니다.", nameof(waypoints));

            this.waypoints = waypoints;
            segmentIndex = 0;
            Position = waypoints[0];
            Progress = 0f;
            ReachedEnd = false;
        }

        /// <summary>주어진 거리만큼 경로를 따라 전진한다. 음수는 허용하지 않는다.</summary>
        public void Advance(float distance)
        {
            if (distance < 0f)
                throw new ArgumentOutOfRangeException(nameof(distance), "전진 거리는 음수일 수 없습니다.");
            if (ReachedEnd)
                return;

            float remaining = distance;

            while (remaining > 0f && segmentIndex < waypoints.Count - 1)
            {
                Vec2 next = waypoints[segmentIndex + 1];
                float toNext = Position.DistanceTo(next);

                if (toNext <= remaining)
                {
                    // 다음 waypoint에 도달하고 남은 거리로 그 다음 구간 계속
                    Progress += toNext;
                    remaining -= toNext;
                    Position = next;
                    segmentIndex++;
                }
                else
                {
                    // 현재 구간 안에서 멈춤
                    Position = Position.MoveTowards(next, remaining);
                    Progress += remaining;
                    remaining = 0f;
                }
            }

            if (segmentIndex >= waypoints.Count - 1)
            {
                Position = waypoints[waypoints.Count - 1];
                ReachedEnd = true;
            }
        }
    }
}
