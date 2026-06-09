using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    public class SaveStoreTests
    {
        [Test]
        public void RecordClear_클리어와_별점이_기록된다()
        {
            var store = new SaveStore();
            store.RecordClear(stageId: 1, stars: 2);
            Assert.IsTrue(store.IsCleared(1));
            Assert.AreEqual(2, store.GetStars(1));
        }

        [Test]
        public void 클리어한적_없으면_미클리어_0성()
        {
            var store = new SaveStore();
            Assert.IsFalse(store.IsCleared(5));
            Assert.AreEqual(0, store.GetStars(5));
        }

        [Test]
        public void RecordClear_더_높은_별점만_갱신한다()
        {
            var store = new SaveStore();
            store.RecordClear(1, 3);
            store.RecordClear(1, 1); // 낮은 별점은 무시
            Assert.AreEqual(3, store.GetStars(1));

            store.RecordClear(2, 1);
            store.RecordClear(2, 3); // 높은 별점은 갱신
            Assert.AreEqual(3, store.GetStars(2));
        }

        [Test]
        public void RecordClear_별점이_범위밖이면_예외()
        {
            var store = new SaveStore();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => store.RecordClear(1, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => store.RecordClear(1, 4));
        }

        [Test]
        public void Serialize_Deserialize_라운드트립()
        {
            var store = new SaveStore();
            store.RecordClear(1, 3);
            store.RecordClear(3, 2);

            string data = store.Serialize();
            var restored = SaveStore.Deserialize(data);

            Assert.AreEqual(3, restored.GetStars(1));
            Assert.AreEqual(2, restored.GetStars(3));
            Assert.IsFalse(restored.IsCleared(2));
        }

        [Test]
        public void Deserialize_손상된_항목은_건너뛴다()
        {
            // 유효: 1:3, 4:2 / 무효: 별점 범위밖(2:9), 형식깨짐(abc), 음수 ID(-1:2)
            var store = SaveStore.Deserialize("1:3;2:9;abc;-1:2;4:2");
            Assert.AreEqual(3, store.GetStars(1));
            Assert.AreEqual(2, store.GetStars(4));
            Assert.IsFalse(store.IsCleared(2));   // 범위밖이라 무시
            Assert.IsFalse(store.IsCleared(-1));  // 음수 ID 무시
        }

        [Test]
        public void Deserialize_빈문자열이나_null이면_빈_저장소()
        {
            Assert.IsFalse(SaveStore.Deserialize("").IsCleared(1));
            Assert.IsFalse(SaveStore.Deserialize(null).IsCleared(1));
        }
    }
}
