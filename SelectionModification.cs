using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace U3
{
  public delegate DelayedDataChange<TValueType> OnChangeCallSimple<TValueType>(VisualElement element, TValueType oldValue, TValueType newValue);

  #region type-specific-extensions
  public static class SelectionModification
  { 
    ///<summary>Sets label for each element in selection.</summary>
    ///<param name="dataFunc">A function that is called for each element. It is given the element, the bound datum, and expected to return the label.</param>
    public static Selection<TElementType, TDataType, TParentDataType> Label<TElementType,TDataType, TParentDataType>(this Selection<TElementType, TDataType, TParentDataType> sel
                                                                                                                    , Func<TElementType,TDataType,string> dataFunc
                                                                                                                    , Selection<TElementType, TDataType, TParentDataType>.OnChangeCall<string> onChange = null)
      where TElementType : TextElement
    {
      sel.ChangeValue(LabelAccessor.Instance, dataFunc, onChange);
      return sel;
    }

    public static Selection<TElementType, TDataType, TParentDataType> Label<TElementType, TDataType, TParentDataType>(this Selection<TElementType, TDataType, TParentDataType> sel, string data, Selection<TElementType, TDataType, TParentDataType>.OnChangeCall<string> onChange = null)
      where TElementType : TextElement
    {
      sel.ChangeValue(LabelAccessor.Instance, data, onChange);
      return sel;
    }

    ///<summary>Sets label for each element in selection.</summary>
    ///<param name="dataFunc">A function that is called for each element. It is given the element, the bound datum, and expected to return the label.</param>
    public static Selection<TElementType, TDataType, TParentDataType> IntegerLabel<TElementType, TDataType, TParentDataType>(this Selection<TElementType, TDataType, TParentDataType> sel, Func<TElementType, TDataType, int> dataFunc, Selection<TElementType, TDataType, TParentDataType>.OnChangeCall<int> onChange = null)
      where TElementType : TextElement
    {
      sel.ChangeValue(IntegerLabelAccessor.Instance, dataFunc, onChange);
      return sel;
    }

    public static Selection<TElementType, TDataType, TParentDataType> IntegerLabel<TElementType, TDataType, TParentDataType>(this Selection<TElementType, TDataType, TParentDataType> sel, int data, Selection<TElementType, TDataType, TParentDataType>.OnChangeCall<int> onChange = null)
      where TElementType : TextElement
    {
      sel.ChangeValue(IntegerLabelAccessor.Instance, data, onChange);
      return sel;
    }

    public static Selection<TElementType, TDataType, TParentDataType> Texture<TElementType, TDataType, TParentDataType>(this Selection<TElementType, TDataType, TParentDataType> sel, Func<TElementType, TDataType, Texture> dataFunc)
      where TElementType : Image
    {
      sel.Groups.ForEach(group => group.Elements.ForEach(image => image.image = dataFunc(image, (TDataType)image.GetBoundData())) );
      return sel;
    }

    public static Selection<TElementType, TDataType, TParentDataType> Texture<TElementType, TDataType, TParentDataType>(this Selection<TElementType, TDataType, TParentDataType> sel, Texture texture)
      where TElementType : Image
    {
      sel.Groups.ForEach(group => group.Elements.ForEach(image => image.image = texture));
      return sel;
    }

  }
  #endregion

  public partial class Selection<TElementType,TDataType,TParentDataType>
    where TElementType : VisualElement
  {
    public delegate void CallDelegate(TElementType element, TDataType dataBinding, int idInSelection);

    #region dataOperations

    ///<summary>Calls a function for each element in selection.</summary>
    ///<param name="dataFunc">A function that is called for each element. It is given the element, the bound datum, and the element's order within it's group.</param>
    public Selection<TElementType,TDataType,TParentDataType> Call(CallDelegate dlgt)
    {
      foreach (var groupWithData in _groups)
      {
        var id = 0;
        foreach (var visualElement in groupWithData.Elements)
        {
          dlgt(visualElement, (TDataType)visualElement.GetBoundData(), id++);
        }
      }
      return this;
    }


    public delegate DelayedDataChange<TValueType> OnChangeCall<TValueType>(TElementType element, TDataType data, TParentDataType parentData, TValueType oldValue, TValueType newValue);

    /// <summary>For each element in selection either adds or removes given class.</summary>
    /// <param name="ussClass">The class that should be changed</param>
    /// <param name="hasClass">Whether the class should be added or removed.</param>
    /// <param name="onChange">Function called when the value is different from the new value. Return value can be used to delay the actual change.</param>
    public Selection<TElementType,TDataType,TParentDataType> Classed(string ussClass, bool hasClass, OnChangeCall<bool> onChange)
    {
      ChangeValue(new ClassAccessor(ussClass), hasClass, onChange);
      return this;
    }

    public Selection<TElementType, TDataType, TParentDataType> Classed(string ussClass, bool hasClass, OnChangeCallSimple<bool> onChange = null)
    {
      ChangeValue(new ClassAccessor(ussClass), hasClass, onChange);
      return this;
    }

    /// <summary>For each element in selection either adds or removes given class.</summary>
    /// <param name="hasClassFunc">A function called for each element, indicating the class should be added or removed. It is passed element, it's darum, and the element's order in it's group.</param>
    /// <param name="onChanged">A callback function called for each element where the class changes. Params are parent, element, data</param>
    /// <param name="onChange">Function called when the value is different from the new value. Return value can be used to delay the actual change.</param>
    public Selection<TElementType, TDataType,TParentDataType> Classed(string ussClass, Func<TElementType, TDataType, bool> hasClassFunc, OnChangeCall<bool> onChange)
    {
      ChangeValue(new ClassAccessor(ussClass), hasClassFunc, onChange);
      return this;
    }

    public Selection<TElementType, TDataType, TParentDataType> Classed(string ussClass, Func<TElementType, TDataType, bool> hasClassFunc, OnChangeCallSimple<bool> onChange = null)
    {
      ChangeValue(new ClassAccessor(ussClass), hasClassFunc, onChange);
      return this;
    }

    public Selection<TElementType, TDataType, TParentDataType> ChangeValue<TValueType>(Accessor<TValueType> accessor, TValueType value, OnChangeCallSimple<TValueType> onChangeFunc = null)
    {
      Groups.ForEach(group =>
      {
        @group.Elements.ForEach(element =>
        {
          element.GetOrCreateDataBinding().ChangeValue(accessor, value, (oldValue, newValue) => onChangeFunc?.Invoke(element, oldValue, newValue));
        });
      });
      return this;
    }

    public Selection<TElementType, TDataType, TParentDataType> ChangeValue<T>(Accessor<T> accessor, Func<TElementType, TDataType, T> valueFunc, OnChangeCallSimple<T> onChangeFunc = null)
    {
      Groups.ForEach(group =>
      {
        @group.Elements.ForEach(element =>
        {
          TDataType data = (TDataType)element.GetBoundData();
          element.GetOrCreateDataBinding().ChangeValue(accessor, valueFunc(element, data), (oldValue, newValue) => onChangeFunc?.Invoke(element, oldValue, newValue));
        });
      });
      return this;
    }

    /// <summary>
    /// Generic method for changing a value of all elements of this selection
    /// </summary>
    /// <typeparam name="T">Type of the value that is about to be changed.</typeparam>
    /// <param name="accessor">An <see cref="Accessor{T}"/>, which changes the required value</param>
    /// <param name="valueFunc">Function that returns new value for each element. </param>
    /// <param name="onChangeFunc">Function that is called whenever the new value is different from current value of an element. It can return a <see cref="DelayedDataChange{T}"/>, which is used to delay the actual change.</param>
    /// <returns></returns>
    public Selection<TElementType, TDataType, TParentDataType> ChangeValue<T>(Accessor<T> accessor, Func<TElementType, TDataType, T> valueFunc, OnChangeCall<T> onChangeFunc = null)
    {
      Groups.ForEach(group =>
      {
        TParentDataType parentData = (TParentDataType)group.GroupParent.GetBoundData();
        @group.Elements.ForEach(element =>
        {
          TDataType data = (TDataType) element.GetBoundData();
          element.GetOrCreateDataBinding().ChangeValue(accessor, valueFunc(element, data), (oldValue, newValue) => onChangeFunc?.Invoke(element, data, parentData, oldValue, newValue));
        });
      });
      return this;
    }

    /// <summary>
    /// Generic method for changing a value of all elements of this selection to a single value.
    /// </summary>
    /// <typeparam name="T">Type of the value that is about to be changed.</typeparam>
    /// <param name="accessor">An <see cref="Accessor{T}"/>, which changes the required value</param>
    /// <param name="value">A value that is set for each element.</param>
    /// <param name="onChangeFunc">Function that is called whenever the new value is different from current value of an element. It can return a <see cref="DelayedDataChange{T}"/>, which is used to delay the actual change.</param>
    /// <returns></returns>
    public Selection<TElementType, TDataType, TParentDataType> ChangeValue<T>(Accessor<T> accessor, T value, OnChangeCall<T> onChangeFunc = null)
    {
      Groups.ForEach(group =>
      {
        TParentDataType parentData = (TParentDataType)group.GroupParent.GetBoundData();
        @group.Elements.ForEach(element =>
        {
          TDataType data = (TDataType) element.GetBoundData();
          element.GetOrCreateDataBinding().ChangeValue(accessor, value, (oldValue,newValue) => onChangeFunc?.Invoke(element, data, parentData, oldValue, newValue ));
        });
      });
      return this;
    }

    ///<summary>Sets each element in selection to either Enabled or Disabled.</summary>
    public Selection<TElementType,TDataType, TParentDataType> SetEnabled(bool enabled)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.SetEnabled(enabled));
      }
      return this;
    }

    ///<summary>Sets each element in selection to either Visible or Hidden.</summary>
    public Selection<TElementType, TDataType, TParentDataType> SetVisible(bool visible)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.visible = visible);
      }
      return this;
    }

    ///<summary>Sets each element in selection to either Visible and Enabled; or Hidden and Disabled.</summary>
    public Selection<TElementType, TDataType, TParentDataType> SetVisibleAndEnabled(bool enabled)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element =>
        {
          element.SetEnabled(enabled);
          element.visible = enabled;
        });
      }
      return this;
    }

    ///<summary>Sets a callback for each element in selection.</summary>
    ///<remarks>Only one callback function for each element can be set.</remarks>
    ///<remarks>Events only react to AtTarget phase of event resolution.</remarks>
    ///<param name="callback">The callback function that is called when the event is fired. It is passed the element, it's bound datum, it's order in it's group and the original event.</param>
    public Selection<TElementType, TDataType, TParentDataType> OnEvent<TEventType>(Action<TElementType, TDataType, int, TEventType> callback, TrickleDown trickle = TrickleDown.NoTrickleDown)
      where TEventType : EventBase<TEventType>, new()
    {
      foreach (var groupWithData in _groups)
      {
        var id = 0;
        foreach (var visualElement in groupWithData.Elements)
        {
          // by using UnifiedCallbackDelegate, callback can be unregistered (as opposed to lambda)
          visualElement.UnregisterCallback<TEventType, Action<TEventType>>
            (U3SelectionOperationsHelper.UnifiedCallbackDelegatable);

          visualElement.RegisterCallback<TEventType, Action<TEventType>>
            ( U3SelectionOperationsHelper.UnifiedCallbackDelegatable
            , @event => callback(visualElement, (TDataType)visualElement.GetBoundData(), id++, @event)
            , trickle );
        }
      }
      return this;
    }

    #endregion

    #region structureOperations

    ///<summary>Deletes all selected elements from their hierarchies.</summary>
    public Selection<TElementType,TDataType, TParentDataType> Remove()
    {
      foreach (var groupWithData in _groups)
      {
        foreach (var visualElement in groupWithData.Elements)
        {
          visualElement.parent.Remove(visualElement);
        }
      }
      return new Selection<TElementType,TDataType, TParentDataType>();
    }

    ///<summary>For each element, a child of given type is created.</summary>
    ///<returns>A new selection containing the newly created children.</returns>
    public Selection<T,object,TDataType> Append<T>() where T:VisualElement, new()
      => new Selection<T,object,TDataType>(
        _groups.Select( groupWithData => 
          new  Selection<T,object,TDataType>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Elements.Select(element => element.Append(new T())).ToArray())
        ).ToArray()
      );

    ///<summary>For each element, the given VisualTreeAsset is instantiated and added as a child.</summary>
    ///<returns>A new selection containing the newly created children.</returns>
    public Selection<VisualElement,object, TParentDataType> Append(VisualTreeAsset asset)
      => new Selection<VisualElement,object, TParentDataType>(
        _groups.Select(groupWithData => 
          new Selection<VisualElement,object, TParentDataType>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Elements.Select(element => element.Append(asset.CloneTree().contentContainer)).ToArray())
        ).ToArray()
      );
      
    #endregion

  }

  internal static class U3SelectionOperationsHelper
  {
    public static void UnifiedCallbackDelegatable<TEventType>(TEventType @event, Action<TEventType> action)
    where TEventType : EventBase<TEventType>, new()
    {
      if (@event.propagationPhase != PropagationPhase.AtTarget && @event.propagationPhase != PropagationPhase.BubbleUp)
        return;
      if (@event.isPropagationStopped)
        return;
      try
      {
        action.Invoke(@event);
        @event.StopPropagation();
      }
      catch (Exception e)
      {
        Debug.LogError(e);
      }
    }
  }

}
