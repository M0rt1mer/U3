using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace U3
{
  public partial class Selection<TElementType, TDataType>
    where TElementType : VisualElement
  {
    #region structs

    internal struct GroupWithData
    {
      public readonly VisualElement GroupParent;
      public readonly IReadOnlyCollection<TElementType> Elements;

      public GroupWithData(VisualElement groupParent, IReadOnlyCollection<TElementType> elements)
      {
        GroupParent = groupParent;
        Elements = elements;
      }
      public GroupWithData(VisualElement groupParent, IEnumerable<TElementType> elements) : this(groupParent, elements.ToArray()){}
    }
    #endregion

    #region attributes

    private IReadOnlyCollection<GroupWithData> _groups;
    private EnterSelection<TDataType> _enterSelection;
    private Selection<TElementType,TDataType> _exitSelection;
    
    #endregion
    
    #region Properties
    
    public EnterSelection<TDataType> Enter => _enterSelection ?? new EnterSelection<TDataType>();
    public Selection<TElementType,TDataType> Exit => _exitSelection ?? new Selection<TElementType,TDataType>();
    internal IReadOnlyCollection<GroupWithData> Groups => _groups;
    #endregion

    #region constructors
    public Selection(IReadOnlyCollection<TElementType> selected)
    {
      _groups = new []{ new GroupWithData(null, selected) };
      _enterSelection = null;
      _exitSelection = null;
    }

    public Selection(IEnumerable<TElementType> selected) : this(selected.ToArray()) {}

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

    public Selection<T,object> SelectAll<T>(string name = null, string @class = null) where T : VisualElement
    {
      return new Selection<T,object>(_groups.SelectMany
        (
          group => @group.Elements.Select(
            element => new Selection<T,object>.GroupWithData(element, 
              element.Children().Is<T>().Where( child => (name==null || child.name.Equals(name) ) && (@class == null || child.ClassListContains(@class)) ).ToArray()) )
        ).ToArray()
      );
    }

    public Selection<VisualElement,object> SelectAll(string name = null, string @class = null)
    {
      return SelectAll<VisualElement>(null, null);
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
    public Selection<TElementType,TDataType> MergeFrom<T>(Selection<T,TDataType> other)
      where T: TElementType
    {
      var groupsByParent = _groups.ToDictionary(group => group.GroupParent, group => (IEnumerable<TElementType>) group.Elements );
      var otherParents = new HashSet<VisualElement>( other._groups.Select(group=>group.GroupParent) );

      IEnumerable<TElementType> GetGroupIfExists(VisualElement parent)
      {
        if (groupsByParent.ContainsKey(parent))
          foreach (var visualElement in groupsByParent[parent])
            yield return visualElement;
      }

      return new Selection<TElementType,TDataType>(
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

    #region data

    public Selection<TElementType,TNewDataType> Bind<TNewDataType>(IReadOnlyCollection<TNewDataType> bindings)
    {
      Debug.Assert(bindings != null, "Bindings mustn't be null");
      return Bind((a, b) => bindings);
    }

    public Selection<TElementType,TNewDataType> Bind<TNewDataType>( Func<object, IReadOnlyCollection<TElementType>, IReadOnlyCollection<TNewDataType>> bindingFunc)
    {
      var enters = new EnterSelection<TNewDataType>.EnterGroup[_groups.Count];
      var updates = new Selection<TElementType,TNewDataType>.GroupWithData[_groups.Count];
      var exits = new Selection<TElementType,TNewDataType>.GroupWithData[_groups.Count];

      int counter = 0;
      foreach (var groupWithData in _groups)
      {
        var tuple = Bind(groupWithData, bindingFunc(groupWithData.GroupParent.GetBoundData(), groupWithData.Elements));

        enters[counter] = tuple.Item1;
        updates[counter] = tuple.Item2;
        exits[counter] = tuple.Item3;
        counter++;
      }

      var enterSelection = new EnterSelection<TNewDataType>(enters);
      var exitSelection = new Selection<TElementType,TNewDataType>(exits);
      return new Selection<TElementType,TNewDataType>(updates) { _enterSelection = enterSelection, _exitSelection = exitSelection };
    }

    private static Tuple<EnterSelection<TNewDataType>.EnterGroup, Selection<TElementType,TNewDataType>.GroupWithData, Selection<TElementType,TNewDataType>.GroupWithData> Bind<TNewDataType>(GroupWithData group,
      IReadOnlyCollection<TNewDataType> bindings)
    {
      var dataLookup = new HashSet<TNewDataType>(bindings);
      var existingData = new HashSet<object>(group.Elements.Select(element => element.GetBoundData())); //type of old data is unknown at this point

      var exit =   group.Elements.Where(element => !(element.GetBoundData() is TNewDataType boundDataTyped) || !dataLookup.Contains( boundDataTyped ) ).ToArray();
      var update = group.Elements.Where(element =>   element.GetBoundData() is TNewDataType boundDataTyped  &&  dataLookup.Contains( boundDataTyped ) ).ToArray();

      dataLookup.ExceptWith( existingData.Is<TNewDataType>() );
      var enter = dataLookup;

      return Tuple.Create(
        new EnterSelection<TNewDataType>.EnterGroup( group.GroupParent, enter ),
        new Selection<TElementType,TNewDataType>.GroupWithData(group.GroupParent, update),
        new Selection<TElementType,TNewDataType>.GroupWithData(group.GroupParent, exit)
      );
    }

    public Selection<VisualElement,TDataType> Join( VisualTreeAsset treeAsset )
    {
      var newSelection = Enter.Append(treeAsset).MergeFrom(this);
      Exit.Remove();
      return newSelection;
    }

    public Selection<TElementType,TDataType> Join<T>()
      where T : TElementType, new()
    {
      var newSelection = this.MergeFrom(Enter.Append<T>());
      Exit.Remove();
      return newSelection;
    }

    /// <summary>
    /// Shortcut for selecting all children of given type, name and class, binding it to it's parent's data, and Joining it.
    /// </summary>
    /// <remarks>In short, this creates a single child for each selected elements</remarks>
    /// <typeparam name="TNewElementType"></typeparam>
    /// <param name="name"></param>
    /// <param name="class"></param>
    /// <returns></returns>
    public Selection<TNewElementType, TDataType> ForwardSingleData<TNewElementType>(string name = null, string @class = null)
    where TNewElementType : VisualElement, new()
    {
      return SelectAll<TNewElementType>(name,@class).Bind((o, _) => new TDataType[]{ (TDataType) o}).Join<TNewElementType>();
    }

    public Selection<TElementType, TDataType> RobustOrder(IReadOnlyCollection<TDataType> order)
    {
      foreach (var groupWithData in _groups)
      {
        Dictionary<TDataType, TElementType> existingElements = new Dictionary<TDataType, TElementType>();
        List<VisualElement> parents = new List<VisualElement>();
        foreach (var element in groupWithData.Elements)
          existingElements[(TDataType) element.GetBoundData()] = element;

        foreach (var orderItem in order)
        {
          if (!existingElements.ContainsKey(orderItem))
            continue;
          var existingElement = existingElements[orderItem];
          parents.Add(existingElement.parent);
          existingElement.parent.Remove(existingElement);
        }

        var parentEnumerator = parents.GetEnumerator();
        foreach (var orderItem in order)
        {
          if (!existingElements.ContainsKey(orderItem))
            continue;
          var existingElement = existingElements[orderItem];
          parentEnumerator.Current.Add(existingElement);
          parentEnumerator.MoveNext();
        }
        parentEnumerator.Dispose();

      }
      return this;
    }

    /// <summary>
    /// Orders the selected elements by their associated data, both in the selection itself AND in VisualElement hierarchy. If any of the following conditions are not met, behaviour is undefined:
    /// * all elements in each group have the same parent element, and it is the groups parent (this will not hold if the selection was created by Find function)
    /// * the parent doesn't have any elements other than the selected elements
    /// * the set of data, bound to selected elements, and the set of data in "order" argument, are identical (this can be ensured by calling Bind+Join on the same dataset
    /// </summary>
    /// <param name="order">A collection of data. Order of elements will correspond to order of data in this collection</param>
    /// <returns></returns>
    public Selection<TElementType, TDataType> FragileOrder(IReadOnlyCollection<TDataType> order)
    {
      return FragileOrder((_, __) => order);
    }

    /// <summary>
    /// Orders the selected elements by their associated data, both in the selection itself AND in VisualElement hierarchy. If any of the following conditions are not met, behaviour is undefined:
    /// * all elements in each group have the same parent element, and it is the groups parent (this will not hold if the selection was created by Find function)
    /// * the parent doesn't have any elements other than the selected elements
    /// * the set of data, bound to selected elements, and the set of data, provided by bindingFnc, are identical (this can be ensured by calling Bind+Join on the same dataset
    /// * data in the set are unique
    /// Additionally, the data set is expected to be small, as no acceleration structure is built for searching data set
    /// </summary>
    /// <param name="bindingFunc">A function that returns the binding set for (parent's data, element list)</param>
    /// <returns>A selection where elements are ordered correctly</returns>
    public Selection<TElementType, TDataType> FragileOrder(Func<object, IReadOnlyCollection<TElementType>, IReadOnlyCollection<TDataType>> bindingFunc)
    {
      return new Selection<TElementType, TDataType>(
        _groups.Select(groupWithData =>
          {
            var collection = bindingFunc(groupWithData.GroupParent.GetBoundData(), groupWithData.Elements);
            groupWithData.Elements.ForEach(e => e.parent.Remove(e));
            var sortedElements = collection.Select(datum =>groupWithData.Elements.First(element =>
                element.GetBoundData() is TDataType typedData && EqualityComparer<TDataType>.Default.Equals(typedData, datum))).ToArray();
            sortedElements.ForEach( element => groupWithData.GroupParent.Add(element) );
            return new GroupWithData(groupWithData.GroupParent, sortedElements);
          }
        ) 
      );
    }

    #endregion

  }
}