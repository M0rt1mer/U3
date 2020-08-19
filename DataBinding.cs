using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace U3
{
  public class DataBinding
  {
    public object BoundData { get; }
    public VisualElement Element { get; }

    private readonly List<IDelayedDataChange> _delayedChanges = new List<IDelayedDataChange>();

    public DataBinding(object boundData, VisualElement element)
    {
      BoundData = boundData;
      Element = element;
    }

    internal delegate DelayedDataChange<TValueType> OnChangeCall<TValueType>(TValueType oldValue, TValueType newValue);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValueType"></typeparam>
    /// <param name="acc"></param>
    /// <param name="newValue"></param>
    /// <param name="onChange">Function (oldValue,newValue) => DDC</param>
   internal void ChangeValue<TValueType>(Accessor<TValueType> acc, TValueType newValue, OnChangeCall<TValueType> onChange)
   {
     //current value is equal to new value -> ignore
     TValueType finalExpectedValue = acc.GetValue(Element);
     
     //Change to this value is scheduled ?
     var lastRelevantDelayedDataChange = _delayedChanges.LastOrDefault(delayedChange => delayedChange.GetAccessor().Equals(acc));
     if (lastRelevantDelayedDataChange != null)
       finalExpectedValue = ((DelayedDataChange<TValueType>) lastRelevantDelayedDataChange).NewValue;

     if ( EqualityComparer<TValueType>.Default.Equals(finalExpectedValue, newValue))
       return;

     var delayed = onChange?.Invoke(finalExpectedValue,newValue);
     if (delayed != null)
     {
       delayed.Initialize(acc, Element, finalExpectedValue,newValue);
       _delayedChanges.Add(delayed);
       if (lastRelevantDelayedDataChange == null)
         delayed.Start();
     }
     else
       acc.SetValue(Element, newValue);
   }

    /// <summary>
    /// Registers a standalone animation (one that does not correspond to a scheduled value change). It is expected to be initialized
    /// </summary>
    /// <typeparam name="TValueType"></typeparam>
    /// <param name="acc"></param>
    /// <param name="delayed"></param>
    public void RegisterAnimation<TValueType>(Accessor<TValueType> acc, DelayedDataChange<TValueType> delayed)
    {
      var lastRelevantDelayedDataChange = _delayedChanges.LastOrDefault(delayedChange => delayedChange.GetAccessor().Equals(acc));
      _delayedChanges.Add(delayed);
      if(lastRelevantDelayedDataChange == null)
        delayed.Start();
    }

    public void ConfirmDelayedDataCompletion<TValueType>(DelayedDataChange<TValueType> ddc)
   {
      Debug.Assert( _delayedChanges.Contains(ddc), "Confirming DelayedDataChange that was never scheduled." );
      Debug.Assert( EqualityComparer<TValueType>.Default.Equals( ddc.NewValue, ddc.Accessor.GetValue(Element) ), "DelayedDataChange was confirmed, but value was not changed correctly" );
      _delayedChanges.Remove(ddc);
   }

  }

  /// <summary>
  /// Represents a generic delayed change. Do NOT inherit from this, inherit from <see cref="DelayedDataChange{T}"/>
  /// </summary>
  internal interface IDelayedDataChange
  {
    IAccessor GetAccessor();
    /// <summary>
    /// Starts the delaying. If there is a delayed changed with same accessor, this is called after previous change is resolved.
    /// </summary>
    void Start();
  }

  public abstract class DelayedDataChange<T> : IDelayedDataChange
  {
    public Accessor<T> Accessor { get; private set; }
    /// <summary>
    /// Value of this DDC. It should never be changed, outside of ChangeValue on DataBinding
    /// </summary>
    public T NewValue { get; private set; }
    public VisualElement Element { get; private set; }
    private bool _initialized = false;

    public IAccessor GetAccessor() => Accessor;

    public virtual void Initialize(Accessor<T> accessor, VisualElement elem, T oldValue, T newValue)
    {
      Debug.Assert(_initialized == false, "Double initialization of DelayedDataChange");
      _initialized = true;
      NewValue = newValue;
      Accessor = accessor;
      Element = elem;
    }

    protected void Finished()
    {
      Element.GetOrCreateDataBinding().ConfirmDelayedDataCompletion(this);
    }

    public abstract void Start();
  }

  [Obsolete]
  public class SimpleTimedDataChange<T> : DelayedDataChange<T>
  {
    private readonly long _delayMs;

    public SimpleTimedDataChange(long delayMs) => this._delayMs = delayMs;

    public override void Start()
    {
      Element.schedule.Execute(() =>
      {
        Accessor.SetValue(Element, NewValue);
        Finished();
      }).ExecuteLater(_delayMs);
    }
  }

  public class ValueAnimatedDataChange<T> : DelayedDataChange<T>
  {
    public ValueAnimation<T> ValueAnimation { get; }
    public bool toValueAlreadySet = false;

    public ValueAnimatedDataChange(VisualElement elem, int durationMs)
    {
      ValueAnimation = ValueAnimation<T>.Create(elem, Interpolator.GetDefaultInterpolator<T>());
      ValueAnimation.durationMs = durationMs;
    }

    public override void Initialize(Accessor<T> accessor, VisualElement elem, T oldValue, T newValue)
    {
      base.Initialize(accessor, elem, oldValue, newValue);
      ValueAnimation.from = oldValue;
      if(!toValueAlreadySet)
        ValueAnimation.to = newValue;
      ValueAnimation.valueUpdated = accessor.SetValue;
      ValueAnimation.onAnimationCompleted = Finished;
    }

    public override void Start()
    {
      ValueAnimation.Start();
    }

  }

  public interface IAccessor : IEquatable<IAccessor>
  {
    object GetValue(VisualElement elem);
  }

  public abstract class Accessor<T> : IAccessor
  {  
    public abstract T GetValue(VisualElement elem);
    public abstract void SetValue(VisualElement elem, T value);
    public virtual bool Equals(IAccessor other) => ReferenceEquals(this, other);
    object IAccessor.GetValue(VisualElement elem) => GetValue(elem);
  }

}