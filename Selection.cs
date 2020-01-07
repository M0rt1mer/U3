using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace U3
{
  public class Selection<ElementType, DataType>
    where ElementType : VisualElement
  {
    #region structs

    internal struct GroupWithData
    {
      public readonly VisualElement GroupParent;
      public readonly IReadOnlyCollection<ElementType> Elements;

      public GroupWithData(VisualElement groupParent, IReadOnlyCollection<ElementType> elements)
      {
        GroupParent = groupParent;
        Elements = elements;
      }
      public GroupWithData(VisualElement groupParent, IEnumerable<ElementType> elements) : this(groupParent, elements.ToArray()){}
    }
    #endregion

    #region attributes

    private IReadOnlyCollection<GroupWithData> _groups;
    private EnterSelection<DataType> _enterSelection;
    private Selection<ElementType,DataType> _exitSelection;

    
    #endregion
    
    #region constructors
    public Selection(IReadOnlyCollection<ElementType> selected)
    {
      _groups = new []{ new GroupWithData(null, selected) };
      _enterSelection = null;
      _exitSelection = null;
    }

    public Selection(IEnumerable<ElementType> selected) : this(selected.ToArray()) {}

    internal Selection(IReadOnlyCollection<GroupWithData> groups)
    {
      _groups = groups;
      _enterSelection = null;
      _exitSelection = null;
    }

    internal Selection(IEnumerable<GroupWithData> groups) : this(groups.ToArray()) {}

    public Selection()
    {}

    #endregion
    
    #region selecting

    public Selection<T,object> SelectAll<T>(string name) where T : VisualElement
    {
      return new Selection<T,object>(_groups.SelectMany
        (
          group => @group.Elements.Select(
            element => new Selection<T,object>.GroupWithData(element, element.Children().Is<T>().Where( child => (name==null || child.name.Equals(name) ) ).ToArray()) )
        ).ToArray()
      );
    }

    public Selection<VisualElement,object> SelectAll()
    {
      return SelectAll<VisualElement>(null);
    }

    public Selection<T,object> SelectAll<T>() where T : VisualElement
    {
      return SelectAll<T>(null);
    }

    public Selection<VisualElement,object> SelectAll(string name)
    {
      return this.SelectAll<VisualElement>(name);
    }

    public Selection<VisualElement,object> Find(string name)
    {
      return this.Find<VisualElement>(name);
    }

    public Selection<T,object> Find<T>(string name) where T : VisualElement
    {
      return new Selection<T,object>( 
        _groups.SelectMany( 
          groupWithData => groupWithData.Elements.Select(
            element =>
            {
              List<T> found = new List<T>();
              Queue<VisualElement> searchIn = new Queue<VisualElement>();
              searchIn.Enqueue(element);
              while (searchIn.Count > 0)
              {
                var thisElem = searchIn.Dequeue();
                if (thisElem is T thisElemtTyped && (name == null || thisElemtTyped.name.Equals(name)))
                {
                  found.Add(thisElemtTyped);
                }
                foreach (var child in thisElem.Children())
                  searchIn.Enqueue(child);
              }
              return new Selection<T,object>.GroupWithData(element, found);
            }
          )
        )
      );
    }
    
    /// <summary>
    /// Merges this selection into the other, using my ElementType, which is more generic than other's generic type
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Selection<ElementType,DataType> MergeFrom<T>(Selection<T,DataType> other)
      where T: ElementType
    {
      var groupsByParent = _groups.ToDictionary(group => group.GroupParent, group => (IEnumerable<ElementType>) group.Elements );
      var otherParents = new HashSet<VisualElement>( other._groups.Select(group=>group.GroupParent) );

      IEnumerable<ElementType> GetGroupIfExists(VisualElement parent)
      {
        if (groupsByParent.ContainsKey(parent))
          foreach (var visualElement in groupsByParent[parent])
            yield return visualElement;
      }

      return new Selection<ElementType,DataType>(
          other._groups
            .Select( //groups in other, merged with groups in this, matched by parent (or unmerged, IF no parent matched)
              othersGroup => new GroupWithData(othersGroup.GroupParent,
                  othersGroup.Elements.Concat( GetGroupIfExists(othersGroup.GroupParent) ) )
                )
            .Concat( _groups // groups in this that weren't merged in previous step
              .Where(  group => !otherParents.Contains(@group.GroupParent) )
              .Select( group => new GroupWithData(@group.GroupParent, @group.Elements) )
            )
      );
    }

     #endregion

    #region operations

    public delegate void CallDelegate(VisualElement element, object dataBinding, int idInSelection);

    public Selection<ElementType,DataType> Call(CallDelegate dlgt)
    {
      foreach (var groupWithData in _groups)
      {
        var id = 0;
        foreach (var visualElement in groupWithData.Elements)
        {
          dlgt(visualElement, visualElement.GetBoundData(), id++);
        }
      }
      return this;
    }

    public Selection<ElementType,DataType> Label(Func<ElementType, DataType, string> dataFnc)
    {
      Debug.Assert(typeof(ElementType) == typeof(Label));
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach( element => { (element as Label).text = dataFnc(element, (DataType) element.GetBoundData()); });
      }
      return this;
    }

    public Selection<ElementType,DataType> AddClass(string className)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.AddToClassList(className));
      }
      return this;
    }

    public Selection<ElementType,DataType> SetEnabled(bool enabled)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach(element => element.SetEnabled(enabled));
      }

      return this;
    }

    #endregion operations

    #region data

    public Selection<ElementType,NewDataType> Bind<NewDataType>(IReadOnlyCollection<NewDataType> bindings)
    {
      return Bind((a, b) => bindings);
    }

    public Selection<ElementType,NewDataType> Bind<NewDataType>( Func<object, IReadOnlyCollection<ElementType>, IReadOnlyCollection<NewDataType>> bindingFunc)
    {
      var enters = new EnterSelection<NewDataType>.EnterGroup[_groups.Count];
      var updates = new Selection<ElementType,NewDataType>.GroupWithData[_groups.Count];
      var exits = new Selection<ElementType,NewDataType>.GroupWithData[_groups.Count];

      int counter = 0;
      foreach (var groupWithData in _groups)
      {
        var tuple = Bind(groupWithData, bindingFunc(groupWithData.GroupParent.GetBoundData(), groupWithData.Elements));

        enters[counter] = tuple.Item1;
        updates[counter] = tuple.Item2;
        exits[counter] = tuple.Item3;
      }

      var enterSelection = new EnterSelection<NewDataType>(enters);
      var exitSelection = new Selection<ElementType,NewDataType>(exits);
      return new Selection<ElementType,NewDataType>(updates) { _enterSelection = enterSelection, _exitSelection = exitSelection };
    }

    private static Tuple<EnterSelection<NewDataType>.EnterGroup, Selection<ElementType,NewDataType>.GroupWithData, Selection<ElementType,NewDataType>.GroupWithData> Bind<NewDataType>(GroupWithData group,
      IReadOnlyCollection<NewDataType> bindings)
    {
      var dataLookup = new HashSet<NewDataType>(bindings);
      var existingData = new HashSet<object>(group.Elements.Select(element => element.GetBoundData())); //type of old data is unknown at this point

      var exit =   group.Elements.Where(element => !(element.GetBoundData() is NewDataType boundDataTyped) || !dataLookup.Contains( boundDataTyped ) ).ToArray();
      var update = group.Elements.Where(element =>   element.GetBoundData() is NewDataType boundDataTyped  &&  dataLookup.Contains( boundDataTyped ) ).ToArray();

      dataLookup.ExceptWith( existingData.Is<NewDataType>() );
      var enter = dataLookup.ToArray();

      return Tuple.Create(
        new EnterSelection<NewDataType>.EnterGroup( group.GroupParent, enter ),
        new Selection<ElementType,NewDataType>.GroupWithData(group.GroupParent, update),
        new Selection<ElementType,NewDataType>.GroupWithData(group.GroupParent, exit)
      );
    }

    public EnterSelection<DataType> Enter => _enterSelection ?? new EnterSelection<DataType>();
    public Selection<ElementType,DataType> Exit => _exitSelection ?? new Selection<ElementType,DataType>();

    public Selection<VisualElement,DataType> Join( VisualTreeAsset treeAsset )
    {
      var newSelection = Enter.Append(treeAsset).MergeFrom(this);
      Exit.Remove();
      return newSelection;
    }

    public Selection<ElementType,DataType> Join<T>()
      where T : ElementType, new()
    {
      var newSelection = this.MergeFrom(Enter.Append<T>());
      Exit.Remove();
      return newSelection;
    }

    #endregion

    #region structure

    public Selection<ElementType,DataType> Remove()
    {
      foreach (var groupWithData in _groups)
      {
        foreach (var visualElement in groupWithData.Elements)
        {
          visualElement.parent.Remove(visualElement);
        }
      }
      return new Selection<ElementType,DataType>();
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
}