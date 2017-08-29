using UnityEngine;
using System;

public struct WorleySample {
    public double[] F;
    public uint[] ID;

    public WorleySample(double[] F, uint[] ID) {
        this.F = F;
        this.ID = ID;
    }
}

public enum DistanceFunction {
    EUCLIDIAN,
    MANHATTAN,
    CHEBYSHEV
};

public static class Noise {
    static int[] poisson = new int[256]{
      4,3,1,1,1,2,4,2,2,2,5,1,0,2,1,2,2,0,4,3,2,1,2,1,3,2,2,4,2,2,5,1,
      2,3,2,2,2,2,2,3,2,4,2,5,3,2,2,2,5,3,3,5,2,1,3,3,4,4,2,3,0,4,2,2,
      2,1,3,2,2,2,3,3,3,1,2,0,2,1,1,2,2,2,2,5,3,2,3,2,3,2,2,1,0,2,1,1,
      2,1,2,2,1,3,4,2,2,2,5,4,2,4,2,2,5,4,3,2,2,5,4,3,3,3,5,2,2,2,2,2,
      3,1,1,4,2,1,3,3,4,3,2,4,3,3,3,4,5,1,4,2,4,3,1,2,3,5,3,2,1,3,1,3,
      3,3,2,3,1,5,5,4,2,2,4,1,3,4,1,5,3,3,5,3,4,3,2,2,1,1,1,1,1,2,4,5,
      4,5,4,2,1,5,1,1,2,3,3,3,2,5,2,3,3,2,0,2,1,1,4,2,1,3,2,1,2,2,3,2,
      5,5,3,4,5,5,2,4,4,5,3,2,2,2,1,4,2,3,3,4,2,5,4,2,4,2,2,2,4,5,3,2
    };

    private static int fastfloor(double x) {
        return x < 0 ? ((int)x - 1) : ((int)x);
    }

    const double DENSITY_ADJUSTMENT = 0.85;

    // max_order must be > 0
    public static WorleySample Worley3(float x, float y, float z, uint max_order, DistanceFunction dFunc) {
        double[] F = new double[max_order];
        uint[] ID = new uint[max_order];

        for (int i = 0; i < max_order; ++i) {
            F[i] = 999999.9;
        }

        double[] at = new double[3];
        at[0] = x;
        at[1] = y;
        at[2] = z;
        int iatx = fastfloor(at[0]);
        int iaty = fastfloor(at[1]);
        int iatz = fastfloor(at[2]);

        // test center cube first
        AddSamples(iatx, iaty, iatz, max_order, F, ID, at, dFunc);
        // check if neighbor cubes are even possible by checking square distances
        double x2 = at[0] - iatx;
        double y2 = at[1] - iaty;
        double z2 = at[2] - iatz;
        double mx2 = (1.0 - x2) * (1.0 - x2);
        double my2 = (1.0 - y2) * (1.0 - y2);
        double mz2 = (1.0 - z2) * (1.0 - z2);
        x2 *= x2;
        y2 *= y2;
        z2 *= z2;

        //6 facing neighbors of center cube are closest
        // so they have greatest chance for feature point
        if (x2 < F[max_order - 1]) AddSamples(iatx - 1, iaty, iatz, max_order, F, ID, at, dFunc);
        if (y2 < F[max_order - 1]) AddSamples(iatx, iaty - 1, iatz, max_order, F, ID, at, dFunc);
        if (z2 < F[max_order - 1]) AddSamples(iatx, iaty, iatz - 1, max_order, F, ID, at, dFunc);
        if (mx2 < F[max_order - 1]) AddSamples(iatx + 1, iaty, iatz, max_order, F, ID, at, dFunc);
        if (my2 < F[max_order - 1]) AddSamples(iatx, iaty + 1, iatz, max_order, F, ID, at, dFunc);
        if (mz2 < F[max_order - 1]) AddSamples(iatx, iaty, iatz + 1, max_order, F, ID, at, dFunc);

        // next closest is 12 edge cubes
        if (x2 + y2 < F[max_order - 1]) AddSamples(iatx - 1, iaty - 1, iatz, max_order, F, ID, at, dFunc);
        if (x2 + z2 < F[max_order - 1]) AddSamples(iatx - 1, iaty, iatz - 1, max_order, F, ID, at, dFunc);
        if (y2 + z2 < F[max_order - 1]) AddSamples(iatx, iaty - 1, iatz - 1, max_order, F, ID, at, dFunc);
        if (mx2 + my2 < F[max_order - 1]) AddSamples(iatx + 1, iaty + 1, iatz, max_order, F, ID, at, dFunc);
        if (mx2 + mz2 < F[max_order - 1]) AddSamples(iatx + 1, iaty, iatz + 1, max_order, F, ID, at, dFunc);
        if (my2 + mz2 < F[max_order - 1]) AddSamples(iatx, iaty + 1, iatz + 1, max_order, F, ID, at, dFunc);
        if (x2 + my2 < F[max_order - 1]) AddSamples(iatx - 1, iaty + 1, iatz, max_order, F, ID, at, dFunc);
        if (x2 + mz2 < F[max_order - 1]) AddSamples(iatx - 1, iaty, iatz + 1, max_order, F, ID, at, dFunc);
        if (y2 + mz2 < F[max_order - 1]) AddSamples(iatx, iaty - 1, iatz + 1, max_order, F, ID, at, dFunc);
        if (mx2 + y2 < F[max_order - 1]) AddSamples(iatx + 1, iaty - 1, iatz, max_order, F, ID, at, dFunc);
        if (mx2 + z2 < F[max_order - 1]) AddSamples(iatx + 1, iaty, iatz - 1, max_order, F, ID, at, dFunc);
        if (my2 + z2 < F[max_order - 1]) AddSamples(iatx, iaty + 1, iatz - 1, max_order, F, ID, at, dFunc);

        // final 8 corners
        if (x2 + y2 + z2 < F[max_order - 1]) AddSamples(iatx - 1, iaty - 1, iatz - 1, max_order, F, ID, at, dFunc);
        if (x2 + y2 + mz2 < F[max_order - 1]) AddSamples(iatx - 1, iaty - 1, iatz + 1, max_order, F, ID, at, dFunc);
        if (x2 + my2 + z2 < F[max_order - 1]) AddSamples(iatx - 1, iaty + 1, iatz - 1, max_order, F, ID, at, dFunc);
        if (x2 + my2 + mz2 < F[max_order - 1]) AddSamples(iatx - 1, iaty + 1, iatz + 1, max_order, F, ID, at, dFunc);
        if (mx2 + y2 + z2 < F[max_order - 1]) AddSamples(iatx + 1, iaty - 1, iatz - 1, max_order, F, ID, at, dFunc);
        if (mx2 + y2 + mz2 < F[max_order - 1]) AddSamples(iatx + 1, iaty - 1, iatz + 1, max_order, F, ID, at, dFunc);
        if (mx2 + my2 + z2 < F[max_order - 1]) AddSamples(iatx + 1, iaty + 1, iatz - 1, max_order, F, ID, at, dFunc);
        if (mx2 + my2 + mz2 < F[max_order - 1]) AddSamples(iatx + 1, iaty + 1, iatz + 1, max_order, F, ID, at, dFunc);

        // We're done! Convert to right size scale
        for (int i = 0; i < max_order; i++) {
            F[i] = Math.Sqrt(F[i]) / DENSITY_ADJUSTMENT;
        }

        return new WorleySample(F, ID);
    }

    private static void AddSamples(int xi, int yi, int zi, uint max_order, double[] F, uint[] ID, double[] at, DistanceFunction distFunc) {
        double dx, dy, dz, fx, fy, fz, d2;
        int count, index;
        uint seed, this_id;

        // each cube has random number seed based on cubes ID
        // LCG using Knuth constants for maximal periods
        seed = 702395077 * (uint)xi + 915488749 * (uint)yi + 2120969693 * (uint)zi;
        count = poisson[seed >> 24];    // 256 element table lookup. using MSB
        seed = 1402024253 * seed + 586950981;   //churns seed

        for (uint j = 0; j < count; ++j) {
            this_id = seed;

            // get random values for fx,fy,fz based on seed
            seed = 1402024253 * seed + 586950981;
            fx = (seed + 0.5) * (1.0 / 4294967296.0);
            seed = 1402024253 * seed + 586950981;
            fy = (seed + 0.5) * (1.0 / 4294967296.0);
            seed = 1402024253 * seed + 586950981;
            fz = (seed + 0.5) * (1.0 / 4294967296.0);
            seed = 1402024253 * seed + 586950981;

            dx = xi + fx - at[0];
            dy = yi + fy - at[1];
            dz = zi + fz - at[2];

            // get distance squared using specified function
            // this used to be using external functions and delegates but this way proved faster by around 30%
            switch (distFunc) {
                default:
                case DistanceFunction.EUCLIDIAN:
                    d2 = dx * dx + dy * dy + dz * dz;
                    break;
                case DistanceFunction.MANHATTAN:
                    d2 = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                    break;
                case DistanceFunction.CHEBYSHEV:
                    d2 = Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz));
                    break;
            }

            // in order insertion
            if (d2 < F[max_order - 1]) {
                index = (int)max_order;
                while (index > 0 && d2 < F[index - 1]) --index;

                // insert this new point into slot # <index>
                // bump down more distant information to make room for this new point.
                for (int i = (int)max_order - 2; i >= index; --i) {
                    F[i + 1] = F[i];
                    ID[i + 1] = ID[i];
                }
                // insert the new point's information into the list.
                F[index] = d2;
                ID[index] = this_id;
            }
        }
    }


    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight,
        int seed, float frequency, float offsetX, float offsetY, int cellType) {

        float[,] nmap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);

        int range = 100000;
        offsetX += prng.Next(-range, range);
        offsetY += prng.Next(-range, range);

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        if (frequency <= 0) {
            frequency = 0.00001f;
        }

        uint reqOrder = 0;
        switch (cellType) {
            default:
            case 0:
                reqOrder = 1; break;
            case 1:
                reqOrder = 2; break;
            case 2:
                reqOrder = 3; break;
            case 3:
                reqOrder = 2; break;
            case 4:
                reqOrder = 3; break;
            case 5:
                reqOrder = 2; break;
            case 6:
                reqOrder = 2; break;
            case 7:
                reqOrder = 2; break;

        }

        for (int y = 0; y < mapHeight; ++y) {
            for (int x = 0; x < mapWidth; ++x) {
                float sampleX = (x - halfWidth) * frequency + offsetX;
                float sampleY = (y - halfHeight) * frequency + offsetY;

                //nmap[x, y] = Mathf.PerlinNoise(sampleX, sampleY);

                WorleySample w = Worley3(sampleX, 0, sampleY, reqOrder, DistanceFunction.EUCLIDIAN);

                double value = 0.0;
                switch (cellType) {
                    default:
                    case 0:
                        value = w.F[0]; break;
                    case 1:
                        value = w.F[1]; break;
                    case 2:
                        value = w.F[2]; break;
                    case 3:
                        value = w.F[1] - w.F[0]; break;
                    case 4:
                        value = w.F[2] - w.F[1]; break;
                    case 5:
                        value = w.F[0] + w.F[1] / 2.0; break;
                    case 6:
                        value = w.F[0] * w.F[1]; break;
                    case 7:
                        value = Math.Min(1.0, 10 * (w.F[1] - w.F[0])); break;
                }
                nmap[x, y] = (float)value;
                //nmap[x, y] = (w.ID[0] % 255) / 255.0f;
            }
        }

        return nmap;
    }

    private static readonly float F3 = 1.0f / 3.0f;
    private static readonly float G3 = 1.0f / 6.0f;

    private static readonly int[][] grad3 = {
        new int[]{ 1, 1, 0 }, new int[]{ -1, 1, 0 }, new int[]{ 1, -1, 0 }, new int[]{ -1, -1, 0 },
        new int[]{ 1, 0, 1 }, new int[]{ -1, 0, 1 }, new int[]{ 1, 0, -1 }, new int[]{ -1, 0, -1 },
        new int[]{ 0, 1, 1 }, new int[]{ 0, -1, 1 }, new int[]{ 0, 1, -1 }, new int[]{ 0, -1, -1 }
    };

    private static float dot(int[] g, float x, float y, float z) {
        return g[0] * x + g[1] * y + g[2] * z;
    }

    private static readonly int[] perm = {
        151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,
        8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,
        35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,
        134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,
        55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208, 89,
        18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,
        250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,
        189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,
        172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,
        228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,
        107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,

        151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,
        8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,
        35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,
        134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,
        55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208, 89,
        18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,
        250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,
        189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,
        172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,
        228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,
        107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    // 3D raw Simplex noise
    public static float Simplex3(float x, float y, float z) {
        float n0, n1, n2, n3; // Noise contributions from the four corners

        // Skew the input space to determine which simplex cell we're in
        float s = (x + y + z) * F3; // Very nice and simple skew factor for 3D
        int i = fastfloor(x + s);
        int j = fastfloor(y + s);
        int k = fastfloor(z + s);
        float t = (i + j + k) * G3; // Very nice and simple unskew factor, too
        float X0 = i - t; // Unskew the cell origin back to (x,y,z) space
        float Y0 = j - t;
        float Z0 = k - t;
        float x0 = x - X0; // The x,y,z distances from the cell origin
        float y0 = y - Y0;
        float z0 = z - Z0;

        // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
        // Determine which simplex we are in.
        int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
        int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords

        if (x0 >= y0) {
            if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // X Y Z order
            else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // X Z Y order
            else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // Z X Y order
        } else { // x0<y0
            if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
            else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
            else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
        }

        // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
        // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
        // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
        // c = 1/6.
        float x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
        float y1 = y0 - j1 + G3;
        float z1 = z0 - k1 + G3;
        float x2 = x0 - i2 + 2.0f * G3; // Offsets for third corner in (x,y,z) coords
        float y2 = y0 - j2 + 2.0f * G3;
        float z2 = z0 - k2 + 2.0f * G3;
        float x3 = x0 - 1.0f + 3.0f * G3; // Offsets for last corner in (x,y,z) coords
        float y3 = y0 - 1.0f + 3.0f * G3;
        float z3 = z0 - 1.0f + 3.0f * G3;

        // Work out the hashed gradient indices of the four simplex corners
        int ii = i & 255;
        int jj = j & 255;
        int kk = k & 255;
        int gi0 = perm[ii + perm[jj + perm[kk]]] % 12;
        int gi1 = perm[ii + i1 + perm[jj + j1 + perm[kk + k1]]] % 12;
        int gi2 = perm[ii + i2 + perm[jj + j2 + perm[kk + k2]]] % 12;
        int gi3 = perm[ii + 1 + perm[jj + 1 + perm[kk + 1]]] % 12;

        // Calculate the contribution from the four corners
        float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
        if (t0 < 0) n0 = 0.0f;
        else {
            t0 *= t0;
            n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
        }

        float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
        if (t1 < 0) n1 = 0.0f;
        else {
            t1 *= t1;
            n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
        }

        float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
        if (t2 < 0) n2 = 0.0f;
        else {
            t2 *= t2;
            n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
        }

        float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
        if (t3 < 0) n3 = 0.0f;
        else {
            t3 *= t3;
            n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
        }

        // Add contributions from each corner to get the final noise value.
        // The result is scaled to stay just inside [-1,1]
        return 32.0f * (n0 + n1 + n2 + n3);
    }

    // range -1,1   but kinda busts outa bounds occasionally (could clamp later if needed but like it this way pree good)
    public static float Fractal3(Vector3 v, Vector3 offset, int octaves, float frequency, float persistence = 0.5f, float lacunarity = 2.0f) {
        double total = 0.0f;
        float amplitude = 1.0f;

        for(int i = 0; i < octaves; ++i) {
            total += Simplex3((v.x+offset.x) * frequency, (v.y+offset.y) * frequency, (v.z+offset.z) * frequency) * amplitude;
            //WorleySample ws = Worley3((v.x + offset.x) * frequency, (v.y + offset.y) * frequency, (v.z + offset.z) * frequency, 1, DistanceFunction.EUCLIDIAN);
            //total += ws.F[0] * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return (float)total;
    }

}
