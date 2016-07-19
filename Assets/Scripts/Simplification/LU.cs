﻿

public class LU {
    public const double EPSILON = 1e-7;

    public readonly int n;
    public readonly double[] a;
    public readonly double[] lu;
    public readonly int[] pivot;

    public LU(double[] A, int N) {
        this.n = N;
        this.a = A;
        this.lu = new double[N * N];
        this.pivot = new int[N];
        for (var i = 0; i < N; i++)
            this.pivot[i] = i;
        Decompose();
    }

    public void Decompose() {
        for (var j = 0; j < n; j++) {
            for (var i = 0; i < j; i++) {
                var beta = a[ij2lin(pivot[i], j)];
                for (var k = 0; k < i; k++) {
                    beta -= lu[ij2lin(i, k)] * lu[ij2lin(k, j)];
                }
                lu[ij2lin(i, j)] = beta;
            }

            {
                var maxBetajj = 0.0;
                var pivotI = j;
                for (var i = j; i < n; i++) {
                    var tmpBetajj = a[ij2lin(pivot[i], j)];
                    for (var k = 0; k < j; k++) {
                        tmpBetajj -= lu[ij2lin(i, k)] * lu[ij2lin(k, j)];
                    }
                    tmpBetajj = System.Math.Abs(tmpBetajj);
                    if (maxBetajj < tmpBetajj) {
                        maxBetajj = tmpBetajj;
                        pivotI = i;
                    }
                }
                if (maxBetajj < EPSILON)
                    throw new SingularMatrixException();
                var tmp = pivot[j]; pivot[j] = pivot[pivotI]; pivot[pivotI] = tmp;
                SwapRow(lu, j, pivotI, n);
            }

            {
                var i = j;
                var beta = a[ij2lin(pivot[i], j)];
                for (var k = 0; k < i; k++) {
                    beta -= lu[ij2lin(i, k)] * lu[ij2lin(k, j)];
                }
                lu[ij2lin(i, j)] = beta;
            }

            for (var i = j + 1; i < n; i++) {
                var alpha = a[ij2lin(pivot[i], j)];
                for (var k = 0; k < j; k++) {
                    alpha -= lu[ij2lin(i, k)] * lu[ij2lin(k, j)];
                }
                lu[ij2lin(i, j)] = alpha / lu[ij2lin(j, j)];
            }
        }
    }

    public void Solve(double[] b, ref double[] x) {
        for (var i = 0; i < n; i++) {
            var y_i = b[pivot[i]];
            for (var k = 0; k < i; k++) {
                y_i -= lu[ij2lin(i, k)] * x[k];
            }
            x[i] = y_i;
        }

        for (var i = n - 1; i >= 0; i--) {
            var x_i = x[i];
            var rBetajj = 1.0 / lu[ij2lin(i, i)];
            for (var k = i + 1; k < n; k++) {
                x_i -= lu[ij2lin(i, k)] * x[k];
            }
            x[i] = x_i * rBetajj;
        }
    }

    public int ij2lin(int i, int j) {
        return i * n + j;
    }

    public static void SwapRow(double[] a, int row0, int row1, int n) {
        var i0 = row0 * n;
        var i1 = row1 * n;
        for (var j = 0; j < n; j++) {
            var tmp = a[i0 + j]; a[i0 + j] = a[i1 + j]; a[i1 + j] = tmp;
        }
    }
}

public class SingularMatrixException : System.Exception {
}