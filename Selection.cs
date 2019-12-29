using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace U3
{
  public struct Selection
  {
    #region structs
    
    private class EmptyCollection : IReadOnlyCollection<object>
    {
      IEnumerator IEnumerable.GetEnumerator() { yield break; }
      public IEnumerator<object> GetEnumerator() { yield break; }
      public int Count => 0;
      public static EmptyCollection Instance = new EmptyCollection();
    }

    internal struct GroupWithData
    {
      public readonly VisualElement GroupParent;
      public readonly IReadOnlyCollection<object> Bindings;
      public readonly IReadOnlyCollection<VisualElement> Elements;

      public GroupWithData(VisualElement groupParent, IReadOnlyCollection<object> bindings, IReadOnlyCollection<VisualElement> elements)
      {
        GroupParent = groupParent;
        Bindings = bindings ?? EmptyCollection.Instance;
        Elements = elements;
      }
    }
    #endregion

    #region attributes

    private GroupWithData[] _groups;
    private EnterSelection? _enterSelection;
    private Selection? _exitSelection;

    #endregion

    #region constructors
    public Selection(VisualElement[] selected)
    {
      _groups = new GroupWithData[]{ new GroupWithData(null, null, selected) };
      _enterSelection = null;
      _exitSelection = null;
    }

    internal Selection(GroupWithData[] groups)
    {
      _groups = groups;
      _enterSelection = null;
      _exitSelection = null;
    }
    #endregion

    #region selecting

    public Selection SelectAll<T>(string name) where T : VisualElement
    {
      return new Selection(_groups.SelectMany(
        group => group.Elements.Zip(group.Bindings.DefaultIfEmpty(), //for each group, create a pairs of (elements,bindings)
          (element, binding) => new GroupWithData(element,           //from each of these bindings, create new group with given element as parent
                                                  binding is IReadOnlyCollection<object> typedBinding ? typedBinding : null, //if this elements binding is readOnlyCollection, apply it to new group
                                                  element.Children().Where( child => child is T && (name==null || child.name.Equals(name) ) ).ToArray()))).ToArray() //expand children
      );
    }

    public Selection SelectAll()
    {
      return SelectAll<VisualElement>(null);
    }

    public Selection SelectAll<T>() where T : VisualElement
    {
      return SelectAll<T>(null);
    }

    public Selection SelectAll(string name)
    {
      return this.SelectAll<VisualElement>(name);
    }

    public Selection Find(string name)
    {
      return this.Find<VisualElement>(name);
    }

    public Selection Find<T>(string name) where T : VisualElement
    {
      return new Selection( 
        _groups.SelectMany( 
          groupWithData => groupWithData.Elements.Select(
            element =>
            {
              List<VisualElement> found = new List<VisualElement>();
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
              return new GroupWithData(element, null, found.ToArray());
            }
          )
        ).ToArray()
      );
    }

    /// <summary>
    /// Merges this selection into the other, using other's bindings
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Selection MergeInto(Selection other)
    {
      var groupsByParent = _groups.ToDictionary(group => group.GroupParent, group => (IEnumerable<VisualElement>) group.Elements );
      var otherParents = new HashSet<VisualElement>( other._groups.Select(group=>group.GroupParent) );

      return new Selection(other._groups.Select(
          group => groupsByParent.ContainsKey(@group.GroupParent)
            ? new GroupWithData(@group.GroupParent, @group.Bindings,
              @group.Elements.Concat(groupsByParent[@group.GroupParent]).ToArray())
            : @group)
        .Concat(_groups.Where(group => !otherParents.Contains(group.GroupParent))).ToArray()
      );
    }

    #endregion

    #region operations

    public delegate void CallDelegate(VisualElement element, object dataBinding, int idInSelection);

    public Selection Call(CallDelegate dlgt)
    {
      foreach (var groupWithData in _groups)
      {
        int id = 0;
        groupWithData.Elements.Zip(groupWithData.Bindings.DefaultIfEmpty(),
          (element, binding) =>
          {
            dlgt(element, binding, id++);
            return 0; // zip has to return something
          });
      }
      return this;
    }

    public Selection Label(Func<VisualElement, object, string> dataFnc)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.DoubleIterate(groupWithData.Bindings).ForEach((tuple) => { if (tuple.Item1 is Label label) label.text = dataFnc(tuple.Item1, tuple.Item2); });
      }

      return this;
    }

    #endregion operations

    #region data

    public Selection Bind(IReadOnlyCollection<object> bindings)
    {
      return Bind((a, b) => bindings);
    }

    public Selection Bind(
      Func<VisualElement, IReadOnlyCollection<VisualElement>, IReadOnlyCollection<object>> bindingFunc)
    {
      var enters = new EnterSelection.EnterGroup[_groups.Length];
      var updates = new GroupWithData[_groups.Length];
      var exits = new GroupWithData[_groups.Length];

      for (var i = 0; i < _groups.Length; ++i)
      {
        var tuple = Bind(_groups[i], bindingFunc(_groups[i].GroupParent, _groups[i].Elements));

        enters[i] = tuple.Item1;
        updates[i] = tuple.Item2;
        exits[i] = tuple.Item3;
      }

      var enterSelection = new EnterSelection(enters);
      var exitSelection = new Selection(exits);
      return new Selection(updates) { _enterSelection = enterSelection, _exitSelection = exitSelection };
    }

    private static Tuple<EnterSelection.EnterGroup, GroupWithData, GroupWithData> Bind(GroupWithData group,
      IEnumerable<object> bindings)
    {
      var dataLookup = new HashSet<object>(group.Bindings as IEnumerable<object>);
      var existingData = new HashSet<object>(group.Elements.Select(element => element.userData));

      var exit = group.Elements.Zip(bindings, Tuple.Create<VisualElement,object> ).Where( tuple => !dataLookup.Contains(tuple.Item1.userData)).Unzip();
      var update = group.Elements.Zip(bindings, Tuple.Create<VisualElement, object>).Where(tuple => dataLookup.Contains(tuple.Item1.userData)).Unzip();

      dataLookup.ExceptWith(existingData);
      var enter = dataLookup.ToArray();

      return Tuple.Create(
        new EnterSelection.EnterGroup( group.GroupParent, enter ),
        new GroupWithData(group.GroupParent, update.Item2.ToArray(), update.Item1.ToArray()),
        new GroupWithData(group.GroupParent, exit.Item2.ToArray(), exit.Item1.ToArray())
      );
    }

    public EnterSelection Enter => _enterSelection ?? new EnterSelection();
    public Selection Exit => _exitSelection ?? new Selection();

    public Selection Join( VisualTreeAsset treeAsset )
    {
      var newSelection = Enter.Append(treeAsset).MergeInto(this);
      Exit.Remove();
      return newSelection;
    }

    public Selection Join<T>()
      where T : VisualElement, new()
    {
      var newSelection = Enter.Append<T>().MergeInto(this);
      Exit.Remove();
      return newSelection;
    }

    #endregion

    #region structure

    public Selection Remove()
    {
      foreach (var groupWithData in _groups)
      {
        foreach (var visualElement in groupWithData.Elements)
        {
          visualElement.parent.Remove(visualElement);
        }
      }
      return new Selection();
    }

    public Selection Append<T>() where T:VisualElement, new()
    => new Selection(
          _groups.Select( groupWithData => 
              new GroupWithData(groupWithData.GroupParent, groupWithData.Bindings, 
                groupWithData.Elements.Select(element => element.Append(new T())).ToArray())
          ).ToArray()
      );

    public Selection Append(VisualTreeAsset asset)
    => new Selection(
      _groups.Select(groupWithData => 
        new GroupWithData(groupWithData.GroupParent, groupWithData.Bindings,
          groupWithData.Elements.Select(element => element.Append(asset.CloneTree().contentContainer)).ToArray())
        ).ToArray()
      );

    #endregion

  }
}