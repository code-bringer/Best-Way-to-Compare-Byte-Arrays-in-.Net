using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet;

namespace Benchmarks
{
    public sealed class ByteArrayComparsionBenchmarkCompetition
    {
        private readonly int _arraySize;
        private byte[] _firstArray;
        private byte[] _secondArray;

        public ByteArrayComparsionBenchmarkCompetition(int arraySize)
        {
            if (arraySize < 0)
            {
                throw new ArgumentOutOfRangeException("arraySize", arraySize, "Array size cannot be less than zero");
            }

            _arraySize = arraySize;
        }

        public void Run()
        {
            var competition = new BenchmarkCompetition();
            competition.AddTask("Simplest", PrepareArrays, () => CompareBySimplestMethod());
            competition.AddTask("SequenceEqual", PrepareArrays, () => CompareBySequenceEqualMethod());
            competition.AddTask("IStructuralEquatable", PrepareArrays, () => CompareByIStructuralEquatableMethod());
            competition.AddTask("Unsafe", PrepareArrays, () => CompareByUnsafeMethod());
            competition.AddTask("PInvoke", PrepareArrays, () => CompareByPInvokeMethod());

            competition.Run();
        }

        private void PrepareArrays()
        {
            _firstArray = new byte[_arraySize];
            _firstArray[_arraySize / 2] = (byte)(10 & 0x000000ff);

            _secondArray = new byte[_arraySize];
            _secondArray[_arraySize / 2] = (byte)(10 & 0x000000ff);
        }

        private bool CompareByIStructuralEquatableMethod()
        {
            IStructuralEquatable equatable = _firstArray;
            return equatable.Equals(_secondArray, StructuralComparisons.StructuralEqualityComparer);
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] left, byte[] right, long count);

        private bool CompareByPInvokeMethod()
        {
            return memcmp(_firstArray, _secondArray, _firstArray.Length) == 0;
        }

        private bool CompareBySequenceEqualMethod()
        {
            return _firstArray.SequenceEqual(_secondArray);
        }

        private bool CompareBySimplestMethod()
        {
            if (_firstArray.Length != _secondArray.Length)
            {
                return false;
            }

            for (int i = 0; i < _secondArray.Length; i++)
            {
                if (_firstArray[i] != _secondArray[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareByUnsafeMethod()
        {
            return UnsafeCompare(_firstArray, _secondArray);
        }

        // Copyright (c) 2008-2013 Hafthor Stefansson
        // Distributed under the MIT/X11 software license
        // Ref: http://www.opensource.org/licenses/mit-license.php.
        private static unsafe bool UnsafeCompare(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;

            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2))
                        return false;

                if ((l & 4) != 0)
                {
                    if (*((int*)x1) != *((int*)x2))
                        return false;
                    x1 += 4;
                    x2 += 4;
                }

                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2))
                        return false;
                    x1 += 2;
                    x2 += 2;
                }

                if ((l & 1) != 0)
                    if (*((byte*)x1) != *((byte*)x2))
                        return false;

                return true;
            }
        }
    }
}
