
namespace BacklogMaintainer.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T item in sequence)
                action(item);
        }
    }
}
