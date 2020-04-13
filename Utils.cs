using System;
using System.Collections.Generic;
using _4Game;

namespace U3 
{
  public static class Utils
  {

    public static void ChangeValue<TType>(ref TType location, TType newValue, Action<TType> callback)
    {
      if (EqualityComparer<TType>.Default.Equals(newValue, location)) 
        return;
      
      var oldValue = location;
      location = oldValue;
      callback(oldValue);
    }

  }
}