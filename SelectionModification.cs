﻿using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace U3
{
  #region type-specific-extensions
  public static class SelectionModification
  { 
    public static Selection<TElementType, TDataType> Label<TElementType,TDataType>(this Selection<TElementType, TDataType> sel, Func<TElementType,TDataType,string> dataFunc)
      where TElementType : TextElement
    {
      sel.Groups.ForEach( group => group.Elements.ForEach( label => label.text = dataFunc(label, (TDataType)label.GetBoundData()) ) );
      return sel;
    }

    public static Selection<TElementType, TDataType> Texture<TElementType, TDataType>(this Selection<TElementType, TDataType> sel, Func<TElementType, TDataType, Texture> dataFunc)
      where TElementType : Image
    {
      sel.Groups.ForEach(group => group.Elements.ForEach(image => image.image = dataFunc(image, (TDataType)image.GetBoundData())) );
      return sel;
    }

  }
  #endregion

  public partial class Selection<TElementType, TDataType>
    where TElementType : VisualElement
  {
    public delegate void CallDelegate(TElementType element, TDataType dataBinding, int idInSelection);

    #region dataOperations

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

    public Selection<TElementType,TDataType> AddClass(string className)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.AddToClassList(className));
      }
      return this;
    }

    public Selection<TElementType, TDataType> Classed(string className, bool hasClass)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.EnableInClassList(className, hasClass));
      }
      return this;
    } 
    
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

    public Selection<TElementType,TDataType> SetEnabled(bool enabled)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.SetEnabled(enabled));
      }
      return this;
    }

    public Selection<TElementType, TDataType> SetVisible(bool visible)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.visible = visible);
      }
      return this;
    }

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

    public Selection<T,object> Append<T>() where T:VisualElement, new()
      => new Selection<T,object>(
        _groups.Select( groupWithData => 
          new  Selection<T,object>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Elements.Select(element => element.Append(new T())).ToArray())
        ).ToArray()
      );

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
    public static void UnifiedCallbackDelegatable<TEventType>(TEventType eventType, Action<TEventType> action)
    {
      try
      {
        action.Invoke(eventType);
      }
      catch (Exception e)
      {
        Debug.LogError(e);
      }
    }
  }

}