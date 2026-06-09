using System;

namespace Game.Core
{
    /// <summary>골드 부족으로 구매가 불가능할 때 던지는 도메인 예외.</summary>
    public class InsufficientGoldException : Exception
    {
        public InsufficientGoldException(int required, int available)
            : base($"골드 부족: 필요 {required}, 보유 {available}") { }
    }

    /// <summary>골드 보유·적립·차감을 담당하는 순수 도메인 로직.</summary>
    public class Economy
    {
        public int Gold { get; private set; }

        public Economy(int startingGold)
        {
            if (startingGold < 0)
                throw new ArgumentOutOfRangeException(nameof(startingGold), "시작 골드는 음수일 수 없습니다.");
            Gold = startingGold;
        }

        public void AddKillReward(int reward)
        {
            if (reward < 0)
                throw new ArgumentOutOfRangeException(nameof(reward), "보상은 음수일 수 없습니다.");
            Gold += reward;
        }

        /// <summary>타워 판매 등으로 골드를 되돌려준다.</summary>
        public void Refund(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "환불액은 음수일 수 없습니다.");
            Gold += amount;
        }

        /// <summary>조기 웨이브 시작 보너스 등 추가 골드를 적립한다.</summary>
        public void AddBonus(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "보너스는 음수일 수 없습니다.");
            Gold += amount;
        }

        public void Spend(int cost)
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), "비용은 음수일 수 없습니다.");
            if (cost > Gold)
                throw new InsufficientGoldException(cost, Gold);
            Gold -= cost;
        }
    }
}
