using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 한 스테이지를 이루는 웨이브들의 시퀀스. 각 웨이브는 자기만의 시간 기준을 갖는
    /// WaveSchedule이며, 웨이브는 순서대로 진행된다(마지막 웨이브까지 클리어해야 승리).
    /// </summary>
    public class Stage
    {
        public IReadOnlyList<WaveSchedule> Waves { get; }
        public int WaveCount => Waves.Count;

        public Stage(IReadOnlyList<WaveSchedule> waves)
        {
            if (waves == null)
                throw new ArgumentNullException(nameof(waves));
            if (waves.Count < 1)
                throw new ArgumentException("스테이지는 최소 1개의 웨이브가 필요합니다.", nameof(waves));
            foreach (var w in waves)
                if (w == null)
                    throw new ArgumentException("웨이브는 null일 수 없습니다.", nameof(waves));
            Waves = new List<WaveSchedule>(waves);
        }

        /// <summary>단일 웨이브 스테이지를 만드는 편의 생성자.</summary>
        public static Stage SingleWave(WaveSchedule wave) =>
            new Stage(new List<WaveSchedule> { wave });
    }
}
