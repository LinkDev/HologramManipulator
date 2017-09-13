using System;
using System.Collections.Generic;
using System.Reflection;

namespace LinkDev
{
    public static class IEnumerableExtensions
    {
        public static int FindIndex<TSource>(this IList<TSource> sequence, Func<TSource, bool> predicate)
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                if (predicate(sequence[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static TSource FindFirstOrDefaultAndRemove<TSource>(this IList<TSource> sequence, Func<TSource, bool> predicate)
        {
            TSource requiredItem;
            for (int i = 0; i < sequence.Count; i++)
            {
                if (predicate(sequence[i]))
                {
                    requiredItem = sequence[i];
                    sequence.RemoveAt(i);
                    return requiredItem;
                }
            }
            return default(TSource);
        }
        public static int MaxIndex<T>(this IEnumerable<T> sequence) where T : IComparable<T>
        {
            int maxIndex = -1;
            T maxValue = default(T); // Immediately overwritten anyway

            int index = 0;
            foreach (T value in sequence)
            {
                if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
                {
                    maxIndex = index;
                    maxValue = value;
                }
                index++;
            }
            return maxIndex;
        }

        public static int MinIndex<T>(this IEnumerable<T> sequence) where T : IComparable<T>
        {
            int minIndex = -1;
            T minValue = default(T); // Immediately overwritten anyway

            int index = 0;
            foreach (T value in sequence)
            {
                if (value.CompareTo(minValue) < 0 || minIndex == -1)
                {
                    minIndex = index;
                    minValue = value;
                }
                index++;
            }
            return minIndex;
        }

        public static int IndexOfMaxBy<TSource, TProjected>(this IEnumerable<TSource> source, Func<TSource, TProjected> selector, IComparer<TProjected> comparer = null)
        {
            //null-checks here
            using (var erator = source.GetEnumerator())
            {
                if (!erator.MoveNext())
                    throw new InvalidOperationException("Sequence is empty.");

                if (comparer == null)
                    comparer = Comparer<TProjected>.Default;

                int index = 0, maxIndex = 0;
                var maxProjection = selector(erator.Current);

                while (erator.MoveNext())
                {
                    index++;
                    var projectedItem = selector(erator.Current);

                    if (comparer.Compare(projectedItem, maxProjection) > 0)
                    {
                        maxIndex = index;
                        maxProjection = projectedItem;
                    }
                }
                return maxIndex;
            }
        }

        public static int IndexOfMinBy<TSource, TProjected>(this IEnumerable<TSource> source, Func<TSource, TProjected> selector, IComparer<TProjected> comparer = null)
        {
            //null-checks here
            using (var erator = source.GetEnumerator())
            {
                if (!erator.MoveNext())
                    throw new InvalidOperationException("Sequence is empty.");

                if (comparer == null)
                    comparer = Comparer<TProjected>.Default;

                int index = 0, minIndex = 0;
                var minProjection = selector(erator.Current);

                while (erator.MoveNext())
                {
                    index++;
                    var projectedItem = selector(erator.Current);

                    if (comparer.Compare(projectedItem, minProjection) < 0)
                    {
                        minIndex = index;
                        minProjection = projectedItem;
                    }
                }
                return minIndex;
            }
        }

        public static int IndexOfMinBy<TSource, TProjected>(this IEnumerable<TSource> source, Func<TSource, TProjected> selector, out TProjected value, IComparer<TProjected> comparer = null)
        {
            //null-checks here
            using (var erator = source.GetEnumerator())
            {
                if (!erator.MoveNext())
                    throw new InvalidOperationException("Sequence is empty.");

                if (comparer == null)
                    comparer = Comparer<TProjected>.Default;

                int index = 0, minIndex = 0;
                var minProjection = selector(erator.Current);

                while (erator.MoveNext())
                {
                    index++;
                    var projectedItem = selector(erator.Current);

                    if (comparer.Compare(projectedItem, minProjection) < 0)
                    {
                        minIndex = index;
                        minProjection = projectedItem;
                    }
                }
                value = minProjection;
                return minIndex;
            }
        }
    }
}