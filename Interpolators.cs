using System;
using UnityEngine;

namespace U3
{
  public static class Interpolator
  {
    
    public static float Linear(float from, float to, float coeff)
    {
      return (to - from) * coeff + from;
    }

    public static T LinearFloat<T>(T from, T to, float coeff)
    {
      return (T)(object)Linear(Convert.ToSingle(from), Convert.ToSingle(to), coeff);
    }

    public static int Linear(int from, int to, float coeff)
    {
      return Mathf.FloorToInt((to - from) * coeff) + from;
    }

    public static T LinearInt<T>(T from, T to, float coeff)
    {
      return (T)(object)Linear( Convert.ToInt32(from), Convert.ToInt32(to), coeff);
    }

    public static T HalfwayStep<T>(T from, T to, float coeff)
    {
      return coeff < 0.5f ? from : to;
    }

    public static T FullStep<T>(T from, T to, float coeff)
    {
      return coeff >= 0 ? to : from;
    }

    public static Func<T, T, float, T> GetDefaultInterpolator<T>()
    {
      switch (Type.GetTypeCode(typeof(T)))
      {
        case TypeCode.SByte:
        case TypeCode.Byte:
        case TypeCode.Int16:
        case TypeCode.UInt16:
        case TypeCode.Int32:
        case TypeCode.UInt32:
        case TypeCode.Int64:
        case TypeCode.UInt64:
        case TypeCode.Char:
          return LinearInt<T>;

        case TypeCode.Decimal:
        case TypeCode.Single:
        case TypeCode.Double:
          return LinearFloat<T>;

        case TypeCode.DateTime:
        case TypeCode.Object:
        case TypeCode.String:
        case TypeCode.Boolean:
          return FullStep<T>;

        case TypeCode.Empty:
        case TypeCode.DBNull:
          Debug.LogError($"Trying to interpolate empty type {nameof(T)}. There is not way to interpolate type which has only 1 value, that is empty.");
          return null;
        default:
          Debug.LogError($"GetTypeCode returned an unexpected type code for type {nameof(T)}");
          return null;
      }
    }


  }

}