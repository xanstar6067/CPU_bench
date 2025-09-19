using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPU_Benchmark
{
    /// <summary>
    /// Определяет типы доступных тестов для процессора.
    /// </summary>
    public enum BenchmarkType
    {
        Integer,          // Целочисленные вычисления
        FloatingPoint,    // Вычисления с плавающей запятой
        VectorAvx2        // Векторные AVX2 вычисления
    }
}

