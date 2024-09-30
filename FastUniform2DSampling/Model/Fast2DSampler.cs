using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Lomont.Numerical;

namespace FastUniform2DSampling.Model
{
    public static class Fast2DSampler
    {

        static int GCD(int a, int b)
        {
            while (b != 0)
                (a, b) = (b, a % b); // swap

            return a;
        }

        static double CosAngle(vec2 u, vec2 v)
        {
            return vec2.Dot(u, v) / (u.Length * v.Length);
        }

        public static int MakeDelta(int width, int height, int samples, int testCountMax)
        {
            var nativeDelta = Native.MakeDelta(width, height, samples, testCountMax);

            int area = width * height;
            bool isEven = (area & 1) == 0; // allow faster method for even cases
            int deltaStep = isEven ? 2 : 1;

            // enough to cover surface, rounded up
            int delta = (area + samples - 1) / samples;
            if (isEven && ((delta & 1) == 0))
            {
                delta++; // make odd for area even case
            }

            // find best within testCountMax items
            int testCount = 0;
            int bestDelta = delta;
            double bestError = double.MaxValue;
            while (testCount++ < testCountMax)
            {
                // next relatively prime item
                while (GCD(area, delta) != 1)
                    delta += deltaStep;

                var (b1, b2) = MakeBasis(delta, width);
                var (m1, m2) = LatticeReduction(b1, b2);
                var ratio = m1.Length / m2.Length;
                var err = Math.Abs(1.0 - ratio); // want close to ratio 1.0
                if (err < bestError && Math.Abs(CosAngle(m1, m2)) < 0.25)
                {
                    bestError = err;
                    bestDelta = delta;
                }
                delta += deltaStep; // try next one
            }

           Trace.Assert(nativeDelta == bestDelta);

            return bestDelta;
        }

        public static (vec2 b1, vec2 b2) LatticeReduction(vec2 u, vec2 v)
        {
            // https://en.wikipedia.org/wiki/Lattice_reduction
            // enforce start order v <= u
            if (v.LengthSquared > u.LengthSquared)
                (u, v) = (v, u);

            while (v.LengthSquared < u.LengthSquared)
            {
                var num = vec2.Dot(u,v);
                var den = v.LengthSquared;
                int q = (2 * num + den) / (2 * den);

                //double qd = (double)vec2.Dot(u, v) / v.LengthSquared;
                //int q = (int)(Math.Round(qd));
                (u,v)=(v, u - q * v);
                //vec2 r = u - qi * v;
                //u = v;
                //v = r;
            }

            return (u, v);
        }

        public static (vec2 b1, vec2 b2) MakeBasis(int delta, int width)
        {
            if (delta % width == 0)
                delta++; // avoids infinite loops
            int x1 = delta % width;
            int y1 = delta / width;
            int x2 = x1, y2 = y1;
            int k = 2;
            do
            {
                x2 = (k * delta) % width;
                y2 = (k * delta) / width;
                ++k;
                // check if they're parallel
            } while (x1*y2==x2*y1);

            return (new vec2(x1,y1), new (x2,y2));
        }

        static class Native
        {
            [DllImport("NativeLib.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int MakeDelta(int width, int height, int samples, int testCountMax);

            //[DllImport("FastUniform2DSampling.dll", CallingConvention = CallingConvention.Cdecl)]
            //public static extern int MakeDelta(int width, int height, int samples, int testCountMax);

        }

    }
}
