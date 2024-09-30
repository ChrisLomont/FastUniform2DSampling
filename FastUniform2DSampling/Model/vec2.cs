
namespace FastUniform2DSampling.Model
{
    public record vec2(int x, int y)
    {
        public static int Dot(vec2 a, vec2 b)
        {
            return a.x * b.x + a.y * b.y;

        }
        public static vec2 operator -(vec2 a)
        {
            return new(-a.x, -a.y);
        }
        public static vec2 operator +(vec2 a, vec2 b)
        {
            return new(a.x + b.x, a.y + b.y);
        }
        public static vec2 operator -(vec2 a, vec2 b)
        {
            return new(a.x - b.x, a.y - b.y);
        }
        public static vec2 operator *(int s, vec2 a)
        {
            return new(s * a.x, s * a.y);
        }

        public int LengthSquared => x * x + y * y;
        public double Length => System.Math.Sqrt(LengthSquared);
    }


}
