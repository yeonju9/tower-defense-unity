using System;

namespace Game.Core
{
    /// <summary>
    /// Unity 비의존 2D 좌표·거리 계산 값 타입.
    /// Core 레이어는 UnityEngine.Vector2/3를 참조하지 않으므로, 위치 계산은 모두 이 타입으로 한다.
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public float X { get; }
        public float Y { get; }

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float DistanceTo(Vec2 other)
        {
            float dx = other.X - X;
            float dy = other.Y - Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// target 방향으로 maxDelta만큼 이동한 위치. 남은 거리가 maxDelta 이하면 target에 정확히 도달한다.
        /// </summary>
        public Vec2 MoveTowards(Vec2 target, float maxDelta)
        {
            if (maxDelta < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxDelta), "이동 거리는 음수일 수 없습니다.");

            float dx = target.X - X;
            float dy = target.Y - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist <= maxDelta || dist == 0f)
                return target;

            float t = maxDelta / dist;
            return new Vec2(X + dx * t, Y + dy * t);
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => (X, Y).GetHashCode();
        public override string ToString() => $"({X}, {Y})";
    }
}
