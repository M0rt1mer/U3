using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace U3
{
  ///<summary>
  /// Selection consits of several groups
  /// Each groups represents a simple query into the visual element hierarchy, defined by a parent object and it's descendants. This selection can be bound to a collection of data, with each descendant corresponding to a single datum in the collection.
  ///</summary>
  ///<typeparam name="TElementType">The type of elements in this selection. Functions like Find and Select create selection of correct type.</typeparam>
  ///<typeparam name="TDataType">Type of bound data. Unless Bind is called, this should be 'object'.</typeparam>
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

    private readonly IReadOnlyCollection<GroupWithData> _groups;
    private readonly EnterSelection<TDataType> _enterSelection;
    private readonly Selection<TElementType,TDataType> _exitSelection;
    
    #endregion
    
    #region Properties
    
    ///<summary>An EnterSelection represents data points, for which there is no selected element. It is usually used to create those missing elements.</summary>
    ///<remarks>In a typical case, you would use <see href="Selection{E,T}.Join{T2}" />Join function, which handles Enter and Exit selections for you.</remarks>
    public EnterSelection<TDataType> Enter => _enterSelection ?? new EnterSelection<TDataType>();
    ///<summary>An Exit selection represents elements, for which there is no data point. It is usually used to delete those elements.</summary>
    ///<remarks>In a typical case, you would use <see href="Selection{E,T}.Join{T2}" />Join function, which handles Enter and Exit selections for you.</remarks>
    public Selection<TElementType,TDataType> Exit => _exitSelection ?? new Selection<TElementType,TDataType>();
    internal IReadOnlyCollection<GroupWithData> Groups => _groups;
    #endregion

    #region constructors
    ///<summary>Creates a selection from a read-only collection. The collection is not copied, so Selection expects it not to change during it's existence.</summary>
    public Selection(IReadOnlyCollection<TElementType> selected)
    {
      _groups = new []{ new GroupWithData(null, selected) };
      _enterSelection = null;
      _exitSelection = null;
    }

    ///<summary>Creates a selection from an IEnumerable. The enumerable is enumerated when constructing to create an array.</summary>
    public Selection(IEnumerable<TElementType> selected) : this(selected.ToArray()) {}

    internal Selection(IReadOnlyCollection<GroupWithData> groups)
    {
      _groups = groups;
      _enterSelection = null;
      _exitSelection = null;
    }

    internal Selection(IReadOnlyCollection<GroupWithData> groups, EnterSelection<TDataType> enter, Selection<TElementType, TDataType> exit)
    {
      _groups = groups;
      _enterSelection = enter;
      _exitSelection = exit;
    }

    internal Selection(IEnumerable<GroupWithData> groups) : this(groups.ToArray()) {}

    public Selection()
    {}

    #endregion
    
    #region selecting

    ///<summary>Selects children of currently selected elements, with given class type and name.</summary>
    ///<remarks>This function returns a newly constructed selection, with a separate group for each selected element in the original group.</remarks>
    public Selection<T,object> SelectAll<T>(string name = null, string @class = null) where T : VisualElement
    {
      return new Selection<T,object>(_groups.SelectMany
        (
          group => @group.Elements.Select(
            element => new Selection<T,object>.GroupWithData(element, 
              element.Children().OfType<T>().Where( child => (name==null || child.name.Equals(name) ) && (@class == null || child.ClassListContains(@class)) ).ToArray()) )
        ).ToArray()
      );
    }

    ///<summary>Selects children of currently selected elements, with given name.</summary>
    ///<remarks>This function returns a newly constructed selection, with a separate group for each selected element in the original group.</remarks>
    public Selection<VisualElement,object> SelectAll(string name)
    {
      return this.SelectAll<VisualElement>(name);
    }

    ///<summary>Finds a descendant of currently selected elements, with given name.</summary>
    ///<remarks>This function returns a newly constructed selection, with a separate group for each selected element in the original group.</remarks>
    public Selection<VisualElement,object> Find(string name)
    {
      return this.Find<VisualElement>(name);
    }

    ///<summary>Finds a descendant of currently selected elements, with given class type and name.</summary>
    ///<remarks>This function returns a newly constructed selection, with a separate group for each selected element in the original group.</remarks>
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
    /// Merges this selection into the other, using this selection's ElementType, which has to be more generic than other's ElementType type
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
    ///<summary>Binds this selection to a collection of data. This creates a new selection with Enter and Exit selections, which can then be used to create missing elements and/or delete excess elements.</summary>
    ///<remarks>In a typical case, you would use <see href="Selection{E,T}.Join{T2}" />Join function, which handles Enter and Exit selections for you.</remarks>
    ///<param name="binding">The data collection that should be bound to all groups.</param>
    public Selection<TElementType,TNewDataType> Bind<TNewDataType>(IReadOnlyCollection<TNewDataType> bindings)
    {
      Debug.Assert(bindings != null, "Bindings mustn't be null");
      return Bind((a, b) => bindings);
    }

    ///<summary>Binds this selection to a collection of data. This creates a new selection with Enter and Exit selections, which can then be used to create missing elements and/or delete excess elements.</summary>
    ///<remarks>In a typical case, you would use <see href="Selection{E,T}.Join{T2}" />Join function, which handles Enter and Exit selections for you.</remarks>
    ///<param name="bindingFunc">A callback function that is called once for each group, and should provide the data collection for this group. It's parameters are parent's data object and the collection of elements.</param>
    public Selection<TElementType,TNewDataType> Bind<TNewDataType>( Func<object, IReadOnlyCollection<TElementType>, IEnumerable<TNewDataType>> bindingFunc)
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
      return new Selection<TElementType,TNewDataType>(updates, enterSelection, exitSelection);
    }

    private static Tuple<EnterSelection<TNewDataType>.EnterGroup, Selection<TElementType,TNewDataType>.GroupWithData, Selection<TElementType,TNewDataType>.GroupWithData> Bind<TNewDataType>(GroupWithData group,
      IEnumerable<TNewDataType> bindings)
    {
      var remainingUnboundData = bindings.ToList(); //create a copy

      var update = group.Elements.Where( visualElement => visualElement.GetBoundData() is TNewDataType boundDataTyped && remainingUnboundData.RemoveIfPossible(boundDataTyped) ).ToArray();
      var exit = group.Elements.Except(update);

      return Tuple.Create(
        new EnterSelection<TNewDataType>.EnterGroup( group.GroupParent, remainingUnboundData),
        new Selection<TElementType,TNewDataType>.GroupWithData(group.GroupParent, update),
        new Selection<TElementType,TNewDataType>.GroupWithData(group.GroupParent, exit)
      );
    }

    ///<summary>Creates all elements that were missing during Bind, deletes all excess elements. Then it merges newly created elements with those already existing, and returns this merged selection.</summary>
    ///<remarks>This function can only be called after calling Bind.</remarks>
    ///<param name="treeAsset">A subtree that is inserted at given position. Only it's first root child is used, to avoid creating an extra layer of elements.</param>
    public Selection<VisualElement,TDataType> Join( VisualTreeAsset treeAsset )
    {
      var newSelection = Enter.Append(treeAsset).MergeFrom(this);
      Exit.Remove();
      return newSelection;
    }


    ///<summary>Creates all elements that were missing during Bind, deletes all excess elements. Then it merges newly created elements with those already existing, and returns this merged selection.</summary>
    ///<remarks>This function can only be called after calling Bind.</remarks>
    ///<typeparam name="T">Type of element to be created.</typeparam>
    public Selection<TElementType,TDataType> Join<T>(string name = null)
      where T : TElementType, new()
    {
      var newSelection = this.MergeFrom(Enter.Append<T>(name));
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
      return SelectAll<TNewElementType>(name,@class).Bind((o, _) => new TDataType[]{ (TDataType) o}).Join<TNewElementType>(name);
    }

    ///<summary>Orders all elements in this selection based on the provided order of data elements.</summary>
    ///<remarks>In most cases, FragileOrder can be used, and it's much faster.</remarks>
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
    /// Additionally, the data set is expected to be small, as no acceleration structure is built for searching data set
    /// </summary>
    /// <param name="bindingFunc">A function that returns the binding set for (parent's data, element list)</param>
    /// <returns>A selection where elements are ordered correctly</returns>
    public Selection<TElementType, TDataType> FragileOrder(Func<object, IReadOnlyCollection<TElementType>, IEnumerable<TDataType>> bindingFunc)
    {
      return new Selection<TElementType, TDataType>(
        _groups.Select(groupWithData =>
          {
            var collection = bindingFunc(groupWithData.GroupParent.GetBoundData(), groupWithData.Elements);
            groupWithData.Elements.ForEach(e => e.parent.Remove(e));

            var elementsCopy = groupWithData.Elements.ToList();

            var sortedElements = collection.Select(datum => elementsCopy.PopFirstOrDefault(element =>
                element.GetBoundData() is TDataType typedData && EqualityComparer<TDataType>.Default.Equals(typedData, datum)))
              .ToArray();


            sortedElements.ForEach( element => groupWithData.GroupParent.Add(element) );
            return new GroupWithData(groupWithData.GroupParent, sortedElements);
          }
        ) 
      );
    }

    #endregion

  }
}
