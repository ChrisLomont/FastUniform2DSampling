//
//  Copyright 2024 Chris Lomont
//
//  Permission is hereby granted, free of charge, to any person obtaining a
//  copy of this software and associated documentation files (the "Software"),
//  to deal in the Software without restriction, including without limitation
//  the rights to use, copy, modify, merge, publish, distribute, sublicense,
//  and/or sell copies of the Software, and to permit persons to whom the
//  Software is furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//  OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//  DEALINGS IN THE SOFTWARE.

#include <cmath>
#include <cstdint>
#include <algorithm>
using namespace std;
namespace
{
    int32_t GCD(int32_t a, int32_t b)
    {
        while (b != 0)
        {
            a = a % b;
			swap(a, b);
        }
        return a;
    }

    // get a basis of the lattice into (x1,y1), (x2,y2)
    void MakeBasis(int32_t delta, int32_t width, int32_t& x1, int32_t& y1, int32_t& x2, int32_t& y2)
    {
        if (delta % width == 0)
            delta++; // avoids infinite loops
        x1 = delta % width;
        y1 = delta / width;
        x2 = x1;
    	y2 = y1;
        int32_t k = 2;
        do
        {
            x2 = (k * delta) % width;
            y2 = (k * delta) / width;
            ++k;
            // check if they're parallel
        } while (x1 * y2 == x2 * y1);
    }

    // given basis of lattice, find basis of shortest items
    void LatticeReduction(int32_t& x1, int32_t& y1, int32_t& x2, int32_t& y2)
    {
        // https://en.wikipedia.org/wiki/Lattice_reduction
        if (x2 * x2 + y2 * y2 > x1 * x1 + y1 * y1)
        {
            swap(x1,x2);
            swap(y1, y2);
        }

        while (x2 * x2 + y2 * y2 < x1 * x1 + y1 * y1)
        {
            int32_t num = x1 * x2 + y1 * y2; // u.v
            int32_t den = x2 * x2 + y2 * y2; // |v|^2
            int q = (2 * num + den) / (2 * den);
            //double qd = double(num ) / den;
            //int q = (int)(round(qd));
            x1 -= q * x2;
            y1 -= q * y2;
            swap(x1, x2);
            swap(y1, y2);

        }
    }

}

#ifdef _WINDOWS
#define DLL_EXPORT __declspec(dllexport)
#else
#define DLL_EXPORT 
#endif

// given 2D grid size width x height, desired number of samples, and how many times
// to probe for a good answer, determine a spacing delta that has nice coverage of the grid
extern "C" DLL_EXPORT
int32_t MakeDelta(int32_t width, int32_t height, int32_t samples, int32_t testCountMax)
{
    const int32_t area = width * height;
    const bool isEven = (area & 1) == 0; // allow faster method for even cases
    const int deltaStep = isEven ? 2 : 1;

    // enough to cover surface, rounded up
    int32_t delta = (area + samples - 1) / samples;
    if (isEven && ((delta&1)==0))
    {
        delta++; // make odd for area even case
    }

    // find best within testCountMax items
    int32_t testCount = 0;
    int32_t bestDelta = delta;
    double bestError = width + height;
    while (testCount++ < testCountMax)
    {
        // next relatively prime item
        while (GCD(area, delta) != 1)
            delta += deltaStep;

        int32_t x1, y1, x2, y2;
        MakeBasis(delta, width, x1, y1, x2, y2);
        LatticeReduction(x1, y1, x2, y2);
        const int len1Squared = x1 * x1 + y1 * y1;
        const double len1 = sqrt(len1Squared);
        const int len2Squared = x2 * x2 + y2 * y2;
        const double len2 = sqrt(len2Squared);
        const double ratio = len1 / len2;
        const double err = abs(1.0 - ratio); // want close to ratio 1.0
        // check ratio better, angle good
        if (err < bestError && abs((x1*x2+y1*y2)/(len1*len2)) < 0.25)
        {
            bestError = err;
            bestDelta = delta;
        }
        delta += deltaStep; // try next one
    }
    return bestDelta;
}

