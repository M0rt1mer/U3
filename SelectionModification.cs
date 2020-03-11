using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace U3
{
  #region type-specific-extensions
  public static class SelectionModification
  { 
    ///<summary>Sets label for each element in selection.</summary>
    ///<param name="dataFunc">A function that is called for each element. It is given the element, the bound datum, and expected to return the label.</param>
    public static Selection<TElementType, TDataType> Label<TElementType,TDataType>(this Selection<TElementType, TDataType> sel, Func<TElementType,TDataType,string> dataFunc)
      where TElementType : TextElement
    {
      sel.Groups.ForEach( group => group.Elements.ForEach( label => label.text = dataFunc(label, (TDataType)label.GetBoundData()) ) );
      return sel;
    }

    public static Selection<TElementType, TDataType> Label<TElementType, TDataType>(this Selection<TElementType, TDataType> sel, string data)
      where TElementType : TextElement
    {
      sel.Groups.ForEach(group => group.Elements.ForEach(label => label.text = data));
      return sel;
    }

    public static Selection<TElementType, TDataType> Texture<TElementType, TDataType>(this Selection<TElementType, TDataType> sel, Func<TElementType, TDataType, Texture> dataFunc)
      where TElementType : Image
    {
      sel.Groups.ForEach(group => group.Elements.ForEach(image => image.image = dataFunc(image, (TDataType)image.GetBoundData())) );
      return sel;
    }

    public static Selection<TElementType, TDataType> Texture<TElementType, TDataType>(this Selection<TElementType, TDataType> sel, Texture texture)
      where TElementType : Image
    {
      sel.Groups.ForEach(group => group.Elements.ForEach(image => image.image = texture));
      return sel;
    }

  }
  #endregion

  public partial class Selection<TElementType, TDataType>
    where TElementType : VisualElement
  {
    public delegate void CallDelegate(TElementType element, TDataType dataBinding, int idInSelection);

    #region dataOperations

    ///<summary>Calls a function for each element in selection.</summary>
    ///<param name="dataFunc">A function that is called for each element. It is given the element, the bound datum, and the element's order within it's group.</param>
    public Selection<TElementType,TDataType> Call(CallDelegate dlgt)
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

    ///<summary>Adds a class for each element in selection.</summary>
    ///<remarks>You probably want to use the Classed function, which is way stronger</remarks>
    public Selection<TElementType,TDataType> AddClass(string className)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.AddToClassList(className));
      }
      return this;
    }

    ///<summary>For each element in selection either adds or removes given class.</summary>
    ///<param name="hasClass">Whether the class should be added or removed.</param>
    public Selection<TElementType, TDataType> Classed(string className, bool hasClass)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.EnableInClassList(className, hasClass));
      }
      return this;
    } 
    
    ///<summary>For each element in selection either adds or removes given class.</summary>
    ///<param name="hasClassFunc">A function called for each element, indicating the class should be added or removed. It is passed element, it's darum, and the element's order in it's group.</param>
    public Selection<TElementType, TDataType> Classed(string className, Func<TElementType, TDataType, int, bool> hasClassFunc)
    {
      foreach (var groupWithData in _groups)
      {
        var id = 0;
        foreach (var visualElement in groupWithData.Elements)
        {
          visualElement.EnableInClassList(className, hasClassFunc(visualElement, (TDataType) visualElement.GetBoundData(), id++));
        }
      }
      return this;
    }

    ///<summary>Sets each element in selection to either Enabled or Disabled.</summary>
    public Selection<TElementType,TDataType> SetEnabled(bool enabled)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.SetEnabled(enabled));
      }
      return this;
    }

    ///<summary>Sets each element in selection to either Visible or Hidden.</summary>
    public Selection<TElementType, TDataType> SetVisible(bool visible)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.visible = visible);
      }
      return this;
    }

    ///<summary>Sets each element in selection to either Visible and Enabled; or Hidden and Disabled.</summary>
    public Selection<TElementType, TDataType> SetVisibleAndEnabled(bool enabled)
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
    public Selection<TElementType, TDataType> OnEvent<TEventType>(Action<TElementType, TDataType, int, TEventType> callback, TrickleDown trickle = TrickleDown.NoTrickleDown)
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
    public Selection<TElementType,TDataType> Remove()
    {
      foreach (var groupWithData in _groups)
      {
        foreach (var visualElement in groupWithData.Elements)
        {
          visualElement.parent.Remove(visualElement);
        }
      }
      return new Selection<TElementType,TDataType>();
    }

    ///<summary>For each element, a child of given type is created.</summary>
    ///<returns>A new selection containing the newly created children.</returns>
    public Selection<T,object> Append<T>() where T:VisualElement, new()
      => new Selection<T,object>(
        _groups.Select( groupWithData => 
          new  Selection<T,object>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Elements.Select(element => element.Append(new T())).ToArray())
        ).ToArray()
      );

    ///<summary>For each element, the given VisualTreeAsset is instantiated and added as a child.</summary>
    ///<returns>A new selection containing the newly created children.</returns>
    public Selection<VisualElement,object> Append(VisualTreeAsset asset)
      => new Selection<VisualElement,object>(
        _groups.Select(groupWithData => 
          new Selection<VisualElement,object>.GroupWithData(groupWithData.GroupParent,
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
      if (@event.propagationPhase != PropagationPhase.AtTarget)
        return;
      try
      {
        action.Invoke(@event);
      }
      catch (Exception e)
      {
        Debug.LogError(e);
      }
    }
  }

}
