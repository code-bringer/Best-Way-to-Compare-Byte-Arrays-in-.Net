using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ConsoleApplication2
{
   internal class Program
   {
      private const int CountArrays = 64;
      private const int StartArraySize = 512 * 1024;
      private const int MaxArraySize = 3 * 512 * 1024;
      private const int StepsCount = 4;

      private const int MeasurementsCount = 10;

      private static byte[][] s_arrays;
      private static Stopwatch s_stopwatch;
      private static TestTableBuilder.TableBuilder s_tableBuilder;


      private static void Main(string[] args)
      {

         s_arrays = new byte[CountArrays][];
         s_stopwatch = new Stopwatch();

         s_tableBuilder = new TestTableBuilder.TableBuilder();
         s_tableBuilder.AddRow("Размер массива", "Минимальное время", "Unsafe", "PInvoke", "Простейший метод",
                               "SequenceEqual",
                               "IStructuralEquatable");


         double sizeMultiplier = 1;
         DoInvestigationStep(sizeMultiplier);

         const int MaxMultiplier = MaxArraySize / StartArraySize;
         var stepMmltiplier = Math.Pow(MaxMultiplier, 1 / (double) StepsCount);
         for (var step = 1; step <= StepsCount; step++)
         {
            //sizeMultiplier = Math.Pow(stepMmltiplier, (double) step);
            sizeMultiplier *= stepMmltiplier;
            DoInvestigationStep(sizeMultiplier);
         }

         Console.WriteLine(s_tableBuilder.Output());
         Console.ReadLine();
      }

      private static void DoInvestigationStep(double p_SizeMultiplier)
      {
         var arraySize = (int) (StartArraySize * p_SizeMultiplier);
         Console.WriteLine("Размер массива: {0}", arraySize);

         PrepareTestData(arraySize);
         var results = DoMeasurements();

         var bestTime = results.GetBestTime();
         s_tableBuilder.AddRow(arraySize
                               , bestTime
                               , string.Format("1 : {0:000.0}", (double) results.UnsafeTime / (double) bestTime)
                               , string.Format("1 : {0:000.0}", (double) results.PInvokeTime / (double) bestTime)
                               , string.Format("1 : {0:000.0}", (double) results.SimplestTime / (double) bestTime)
                               , string.Format("1 : {0:000.0}", (double) results.SequenceEqualTime / (double) bestTime)
                               ,
                               string.Format("1 : {0:000.0}",
                                             (double) results.IStructuralEquatableTime / (double) bestTime)
               );
      }

      private struct MeasurementsResults
      {
         public long SimplestTime;
         public long SequenceEqualTime;
         public long PInvokeTime;
         public long IStructuralEquatableTime;
         public long UnsafeTime;

         public long GetBestTime()
         {
            return new[] {SimplestTime, SequenceEqualTime, PInvokeTime, IStructuralEquatableTime, UnsafeTime}.Min();
         }
      }

      private static MeasurementsResults DoMeasurements()
      {
         MeasurementsResults result;
         result.SimplestTime = 0;
         result.SequenceEqualTime = 0;
         result.PInvokeTime = 0;
         result.IStructuralEquatableTime = 0;
         result.UnsafeTime = 0;

         for (int measurementNumber = 0; measurementNumber < MeasurementsCount; measurementNumber++)
         {
            Console.WriteLine("Стартует измерение номер {0}", measurementNumber);

            result.SimplestTime = MeasureComparisonTime(CompareArraysWithSimplestMethod,
                                                        result.SimplestTime,
                                                        measurementNumber);
            /*
            result.SequenceEqualTime = MeasureComparisonTime(CompareArraysWithSequenceEqualMethod,
                                                             result.SequenceEqualTime,
                                                             measurementNumber);
            */
            result.PInvokeTime = MeasureComparisonTime(CompareArraysWithPInvokeMethod,
                                                       result.PInvokeTime,
                                                       measurementNumber);

            /*
            result.IStructuralEquatableTime = MeasureComparisonTime(CompareArraysWithIStructuralEquatableMethod,
                                                                    result.IStructuralEquatableTime,
                                                                    measurementNumber);
            */
            result.UnsafeTime = MeasureComparisonTime(CompareArraysWithUnsafeMethod,
                                                      result.UnsafeTime,
                                                      measurementNumber);

            result.SequenceEqualTime = result.IStructuralEquatableTime = result.PInvokeTime;
         }

         return result;
      }


      private static long MeasureComparisonTime(Func<bool> p_Method, long p_PreviousTime,
                                                int p_MeasurementNumber)
      {
         GC.Collect();
         GC.Collect();

         s_stopwatch.Start();
         var stubLocalVar = p_Method();
         s_stopwatch.Stop();

         if (stubLocalVar)
            throw new InvalidOperationException();

         var result = GetMinimalMesuredValue(p_MeasurementNumber, p_PreviousTime, s_stopwatch.ElapsedTicks);
         s_stopwatch.Reset();

         return result;
      }

      private static long GetMinimalMesuredValue(int p_MeasurementNumber, long p_PreviousValue,
                                                 long p_MeasuredValue)
      {
         var result = p_MeasurementNumber == 0
                            ? p_MeasuredValue
                            : Math.Min(p_PreviousValue, p_MeasuredValue);
         return result;
      }

      private static void PrepareTestData(int p_ArraySize)
      {
         for (int i = 0; i < CountArrays; i++)
         {
            var byteArray = new byte[p_ArraySize];

            for (int j = 0; j < p_ArraySize; j++)
            {
               byteArray[j] = (byte)( (i+j) & 0x000000ff);
            }

            s_arrays[i] = byteArray;
         }
      }




      #region CompareArraysWithIStructuralEquatableMethod

      private static bool CompareArraysWithIStructuralEquatableMethod()
      {
         var result = true;
         for (int i = 0; i < CountArrays; i++)
            for (int j = 0; j < CountArrays; j++)
            {
               var tmp = ByteArrayCompareWithIStructuralEquatable(s_arrays[i], s_arrays[j]);

               result = result && tmp;
            }
         return result;
      }

      private static bool ByteArrayCompareWithIStructuralEquatable(byte[] p_BytesLeft, byte[] p_BytesRight)
      {
         IStructuralEquatable eqa1 = p_BytesLeft;
         return eqa1.Equals(p_BytesRight, StructuralComparisons.StructuralEqualityComparer);
      }

      #endregion


      #region  CompareArraysWithPInvokeMethod

      [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
      private static extern int memcmp(byte[] p_BytesLeft, byte[] p_BytesRight, long p_Count);

      private static bool ByteArrayCompareWithPInvoke(byte[] p_BytesLeft, byte[] p_BytesRight)
      {
         // Validate buffers are the same length.
         // This also ensures that the count does not exceed the length of either buffer.  

         //memcmp( p_BytesLeft, p_BytesRight, p_BytesLeft.Length );

         return p_BytesLeft.Length == p_BytesRight.Length && memcmp(p_BytesLeft, p_BytesRight, p_BytesLeft.Length) == 0;
      }

      private static bool CompareArraysWithPInvokeMethod()
      {
         var result = true;
         for (int i = CountArrays - 1; i >= 0; i--)
            for (int j = 0; j < CountArrays; j++)
            {
               var tmp = ByteArrayCompareWithPInvoke(s_arrays[i], s_arrays[j]);

               result = result && tmp;
            }
         return result;
      }

      #endregion


      #region CompareArraysWithSequenceEqualMethod

      private static bool ByteArrayCompareWithSequenceEqual(byte[] p_BytesLeft, byte[] p_BytesRight)
      {
         return p_BytesLeft.SequenceEqual(p_BytesRight);
      }

      private static bool CompareArraysWithSequenceEqualMethod()
      {
         var result = true;
         for (int i = 0; i < CountArrays; i++)
            for (int j = 0; j < CountArrays; j++)
            {
               var tmp = ByteArrayCompareWithSequenceEqual(s_arrays[i], s_arrays[j]);

               result = result && tmp;
            }
         return result;
      }

      #endregion


      #region CompareArraysWithSimplestMethod

      private static bool ByteArrayCompareWithSimplest(byte[] p_BytesLeft, byte[] p_BytesRight)
      {
         //Thread.Sleep( 60 * 1000 );

         if (p_BytesLeft.Length != p_BytesRight.Length)
         {
            return false;
         }

         for (int i = 0; i < p_BytesLeft.Length; i++) //вот так, без всяких хитростей мы байты сравниваем
         {
            if (p_BytesLeft[i] != p_BytesRight[i])
               return false;
         }

         return true;
      }

      private static bool CompareArraysWithSimplestMethod()
      {
         var result = true;
         for (int i = 0; i < CountArrays; i++)
            for (int j = 0; j < CountArrays; j++)
            {
               var tmp = ByteArrayCompareWithSimplest(s_arrays[i], s_arrays[j]);

               result = result && tmp;
            }
         return result;
      }

      #endregion

      #region CompareArraysWithUnsafeMethod

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
               if (*((long*) x1) != *((long*) x2))
                  return false;

            if ((l & 4) != 0)
            {
               if (*((int*) x1) != *((int*) x2))
                  return false;
               x1 += 4;
               x2 += 4;
            }

            if ((l & 2) != 0)
            {
               if (*((short*) x1) != *((short*) x2))
                  return false;
               x1 += 2;
               x2 += 2;
            }

            if ((l & 1) != 0)
               if (*((byte*) x1) != *((byte*) x2))
                  return false;

            return true;
         }
      }

      private static bool CompareArraysWithUnsafeMethod()
      {
         var result = true;
         for (int i = 0; i < CountArrays; i++)
            for (int j = 0; j < CountArrays; j++)
            {
               var tmp = UnsafeCompare(s_arrays[i], s_arrays[j]);

               result = result && tmp;
            }
         return result;
      }

      #endregion
   }
}
