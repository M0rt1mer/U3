using System;
using System.Collections.Generic;

namespace U3
{
  internal static class Linq
  {
    public static T PopFirstOrDefault<T>(this IList<T> lst, Predicate<T> pred)
    {
      int index = lst.FirstIndex(pred);
      if (index == -1) return default(T);
      T item = lst[index];
      lst.RemoveAt(index);
      return item;
    }

    public static bool RemoveIfPossible<T>(this IList<T> lst, T element)
    {
      int index = lst.IndexOf(element);
      if (index == -1) return false;
      lst.RemoveAt(index);
      return true;
    }

    /// <summary>
    /// Returns index of first item that satisfies the predicate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="pred"></param>
    /// <returns></returns>
    public static int FirstIndex<T>(this IEnumerable<T> list, Predicate<T> pred)
    {
      int index = 0;
      foreach (var item in list)
      {
        if (pred(item))
          return index;
        ++index;
      }
      return -1;
    }

    /*
    public static IEnumerable<Tuple<T1, T2>> DoubleIterate<T1, T2>(this IEnumerable<T1> e1, IEnumerable<T2> e2)
    {
      return e1.Zip(e2, ((arg1, arg2) => new Tuple<T1, T2>(arg1, arg2)));
    }*/

    public static void ForEach<T>(this IEnumerable<T> e, Action<T> action)
    {
      foreach (var item in e)
        action(item);
    }

  }
}