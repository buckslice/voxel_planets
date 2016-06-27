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

    static double atx, aty, atz;
    static DistanceFunction distFunc;

    // max_order must be > 0
    public static WorleySample Worley3(float x, float y, float z, uint max_order, DistanceFunction distanceFunction) {
        distFunc = distanceFunction;

        double[] F = new double[max_order];
        uint[] ID = new uint[max_order];

        for (int i = 0; i < max_order; ++i) {
            F[i] = 999999.9;
        }

        atx = x;
        aty = y;
        atz = z;
        int iatx = fastfloor(atx);
        int iaty = fastfloor(aty);
        int iatz = fastfloor(atz);

        // test center cube first
        AddSamples(iatx, iaty, iatz, max_order, F, ID);
        // check if neighbor cubes are even possible by checking square distances
        double x2 = atx - iatx;
        double y2 = aty - iaty;
        double z2 = atz - iatz;
        double mx2 = (1.0 - x2) * (1.0 - x2);
        double my2 = (1.0 - y2) * (1.0 - y2);
        double mz2 = (1.0 - z2) * (1.0 - z2);
        x2 *= x2;
        y2 *= y2;
        z2 *= z2;

        //6 facing neighbors of center cube are closest
        // so they have greatest chance for feature point
        if (x2 < F[max_order - 1]) AddSamples(iatx - 1, iaty, iatz, max_order, F, ID);
        if (y2 < F[max_order - 1]) AddSamples(iatx, iaty - 1, iatz, max_order, F, ID);
        if (z2 < F[max_order - 1]) AddSamples(iatx, iaty, iatz - 1, max_order, F, ID);
        if (mx2 < F[max_order - 1]) AddSamples(iatx + 1, iaty, iatz, max_order, F, ID);
        if (my2 < F[max_order - 1]) AddSamples(iatx, iaty + 1, iatz, max_order, F, ID);
        if (mz2 < F[max_order - 1]) AddSamples(iatx, iaty, iatz + 1, max_order, F, ID);

        // next closest is 12 edge cubes
        if (x2 + y2 < F[max_order - 1]) AddSamples(iatx - 1, iaty - 1, iatz, max_order, F, ID);
        if (x2 + z2 < F[max_order - 1]) AddSamples(iatx - 1, iaty, iatz - 1, max_order, F, ID);
        if (y2 + z2 < F[max_order - 1]) AddSamples(iatx, iaty - 1, iatz - 1, max_order, F, ID);
        if (mx2 + my2 < F[max_order - 1]) AddSamples(iatx + 1, iaty + 1, iatz, max_order, F, ID);
        if (mx2 + mz2 < F[max_order - 1]) AddSamples(iatx + 1, iaty, iatz + 1, max_order, F, ID);
        if (my2 + mz2 < F[max_order - 1]) AddSamples(iatx, iaty + 1, iatz + 1, max_order, F, ID);
        if (x2 + my2 < F[max_order - 1]) AddSamples(iatx - 1, iaty + 1, iatz, max_order, F, ID);
        if (x2 + mz2 < F[max_order - 1]) AddSamples(iatx - 1, iaty, iatz + 1, max_order, F, ID);
        if (y2 + mz2 < F[max_order - 1]) AddSamples(iatx, iaty - 1, iatz + 1, max_order, F, ID);
        if (mx2 + y2 < F[max_order - 1]) AddSamples(iatx + 1, iaty - 1, iatz, max_order, F, ID);
        if (mx2 + z2 < F[max_order - 1]) AddSamples(iatx + 1, iaty, iatz - 1, max_order, F, ID);
        if (my2 + z2 < F[max_order - 1]) AddSamples(iatx, iaty + 1, iatz - 1, max_order, F, ID);

        // final 8 corners
        if (x2 + y2 + z2 < F[max_order - 1]) AddSamples(iatx - 1, iaty - 1, iatz - 1, max_order, F, ID);
        if (x2 + y2 + mz2 < F[max_order - 1]) AddSamples(iatx - 1, iaty - 1, iatz + 1, max_order, F, ID);
        if (x2 + my2 + z2 < F[max_order - 1]) AddSamples(iatx - 1, iaty + 1, iatz - 1, max_order, F, ID);
        if (x2 + my2 + mz2 < F[max_order - 1]) AddSamples(iatx - 1, iaty + 1, iatz + 1, max_order, F, ID);
        if (mx2 + y2 + z2 < F[max_order - 1]) AddSamples(iatx + 1, iaty - 1, iatz - 1, max_order, F, ID);
        if (mx2 + y2 + mz2 < F[max_order - 1]) AddSamples(iatx + 1, iaty - 1, iatz + 1, max_order, F, ID);
        if (mx2 + my2 + z2 < F[max_order - 1]) AddSamples(iatx + 1, iaty + 1, iatz - 1, max_order, F, ID);
        if (mx2 + my2 + mz2 < F[max_order - 1]) AddSamples(iatx + 1, iaty + 1, iatz + 1, max_order, F, ID);

        // We're done! Convert to right size scale
        for (int i = 0; i < max_order; i++) {
            F[i] = Math.Sqrt(F[i]) / DENSITY_ADJUSTMENT;
        }

        return new WorleySample(F, ID);
    }

    private static void AddSamples(int xi, int yi, int zi, uint max_order, double[] F, uint[] ID) {
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

            dx = xi + fx - atx;
            dy = yi + fy - aty;
            dz = zi + fz - atz;

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

}
