using System.Collections.Generic;
using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class TargetSelectorTests
    {
        private static EnemyUnit Enemy() => new EnemyUnit(maxHp: 30, speed: 1.5f, goldReward: 8);

        [Test]
        public void 사거리밖_적은_무시한다()
        {
            var selector = new TargetSelector();
            var towerPos = new Vec2(0f, 0f);
            var candidates = new List<TargetSelector.Candidate>
            {
                new TargetSelector.Candidate(Enemy(), new Vec2(10f, 0f), progress: 5f) // 거리 10 > 사거리 2.5
            };

            var target = selector.Select(towerPos, range: 2.5f, candidates);
            Assert.IsNull(target);
        }

        [Test]
        public void 사거리내_적이없으면_null()
        {
            var selector = new TargetSelector();
            var target = selector.Select(new Vec2(0f, 0f), 2.5f, new List<TargetSelector.Candidate>());
            Assert.IsNull(target);
        }

        [Test]
        public void 사거리내_여러적중_진행도가_가장높은적을_고른다()
        {
            var selector = new TargetSelector();
            var towerPos = new Vec2(0f, 0f);
            var leader = Enemy();   // 진행도 9 (목표에 더 가까움)
            var laggard = Enemy();  // 진행도 3
            var candidates = new List<TargetSelector.Candidate>
            {
                new TargetSelector.Candidate(laggard, new Vec2(1f, 0f), progress: 3f),
                new TargetSelector.Candidate(leader,  new Vec2(2f, 0f), progress: 9f),
            };

            var target = selector.Select(towerPos, range: 2.5f, candidates);
            Assert.AreSame(leader, target);
        }

        [Test]
        public void 죽은적은_후보에서_제외된다()
        {
            var selector = new TargetSelector();
            var dead = Enemy();
            dead.TakeDamage(100); // 사망(진행도는 가장 높지만 제외되어야 함)
            var alive = Enemy();
            var candidates = new List<TargetSelector.Candidate>
            {
                new TargetSelector.Candidate(dead,  new Vec2(1f, 0f), progress: 9f),
                new TargetSelector.Candidate(alive, new Vec2(1f, 0f), progress: 2f),
            };

            var target = selector.Select(new Vec2(0f, 0f), range: 2.5f, candidates);
            Assert.AreSame(alive, target);
        }
    }
}
