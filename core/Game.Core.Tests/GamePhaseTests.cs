using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class GamePhaseTests
    {
        [Test]
        public void 초기상태는_Ready()
        {
            var phase = new GamePhase();
            Assert.AreEqual(Phase.Ready, phase.Current);
        }

        [Test]
        public void StartWave_Ready에서만_Playing으로_전이()
        {
            var phase = new GamePhase();
            phase.StartWave();
            Assert.AreEqual(Phase.Playing, phase.Current);
        }

        [Test]
        public void NotifyLifeDepleted_Playing에서_Lost로_전이()
        {
            var phase = new GamePhase();
            phase.StartWave();
            phase.NotifyLifeDepleted();
            Assert.AreEqual(Phase.Lost, phase.Current);
        }

        [Test]
        public void NotifyAllCleared_Playing에서_Won으로_전이()
        {
            var phase = new GamePhase();
            phase.StartWave();
            phase.NotifyAllCleared();
            Assert.AreEqual(Phase.Won, phase.Current);
        }

        [Test]
        public void Ready에서_종료통지는_무시된다()
        {
            var phase = new GamePhase();
            phase.NotifyAllCleared();
            phase.NotifyLifeDepleted();
            Assert.AreEqual(Phase.Ready, phase.Current);
        }

        [Test]
        public void 종료상태는_흡수상태라_더이상_전이하지않는다()
        {
            var phase = new GamePhase();
            phase.StartWave();
            phase.NotifyAllCleared();      // Won
            phase.NotifyLifeDepleted();    // 무시되어야 함
            Assert.AreEqual(Phase.Won, phase.Current);
            Assert.IsTrue(phase.IsOver);
        }
    }
}
