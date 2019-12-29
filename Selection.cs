using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace U3
{
  public class Selection
  {
    #region structs

    internal struct GroupWithData
    {
      public readonly VisualElement GroupParent;
      public readonly IReadOnlyCollection<VisualElement> Elements;

      public GroupWithData(VisualElement groupParent, IReadOnlyCollection<VisualElement> elements)
      {
        GroupParent = groupParent;
        Elements = elements;
      }
    }
    #endregion

    #region attributes

    private GroupWithData[] _groups;
    private EnterSelection _enterSelection;
    private Selection _exitSelection;

    
    #endregion
    
    #region constructors
    public Selection(VisualElement[] selected)
    {
      _groups = new GroupWithData[]{ new GroupWithData(null, selected) };
      _enterSelection = null;
      _exitSelection = null;
    }

    internal Selection(GroupWithData[] groups)
    {
      _groups = groups;
      _enterSelection = null;
      _exitSelection = null;
    }

    public Selection(){}
    #endregion
    
    #region selecting

    public Selection SelectAll<T>(string name) where T : VisualElement
    {
      return new Selection(_groups.SelectMany
        (
          group => group.Elements.Select(
           element => new GroupWithData(element, element.Children().Where( child => child is T && (name==null || child.name.Equals(name) ) ).ToArray())
          )
        ).ToArray() //expand children
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
              return new GroupWithData(element, found.ToArray());
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
            ? new GroupWithData(@group.GroupParent,
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
        var id = 0;
        foreach (var visualElement in groupWithData.Elements)
        {
          dlgt(visualElement, visualElement.GetBoundData(), id++);
        }
      }
      return this;
    }

    public Selection Label(Func<VisualElement, object, string> dataFnc)
    {
      foreach (var groupWithData in _groups)
      {
        groupWithData.Elements.ForEach( element => { if (element is Label label) label.text = dataFnc(element, element.GetBoundData()); });
      }
      return this;
    }

    #endregion operations

    #region data

    public Selection Bind(IReadOnlyCollection<object> bindings)
    {
      return Bind((a, b) => bindings);
    }

    public Selection Bind( Func<object, IReadOnlyCollection<VisualElement>, IReadOnlyCollection<object>> bindingFunc)
    {
      var enters = new EnterSelection.EnterGroup[_groups.Length];
      var updates = new GroupWithData[_groups.Length];
      var exits = new GroupWithData[_groups.Length];

      for (var i = 0; i < _groups.Length; ++i)
      {
        var tuple = Bind(_groups[i], bindingFunc(_groups[i].GroupParent.GetBoundData(), _groups[i].Elements));

        enters[i] = tuple.Item1;
        updates[i] = tuple.Item2;
        exits[i] = tuple.Item3;
      }

      var enterSelection = new EnterSelection(enters);
      var exitSelection = new Selection(exits);
      return new Selection(updates) { _enterSelection = enterSelection, _exitSelection = exitSelection };
    }

    private static Tuple<EnterSelection.EnterGroup, GroupWithData, GroupWithData> Bind(GroupWithData group,
      IReadOnlyCollection<object> bindings)
    {
      var dataLookup = new HashSet<object>(bindings as IEnumerable<object>);
      var existingData = new HashSet<object>(group.Elements.Select(element => element.userData));

      var exit = group.Elements.Where(element => element.GetBoundData() == null || !dataLookup.Contains( element.GetBoundData())).ToArray();
      var update = group.Elements.Where(element => element.GetBoundData() != null && dataLookup.Contains( element.GetBoundData() )).ToArray();

      dataLookup.ExceptWith(existingData);
      var enter = dataLookup.ToArray();

      return Tuple.Create(
        new EnterSelection.EnterGroup( group.GroupParent, enter ),
        new GroupWithData(group.GroupParent, update),
        new GroupWithData(group.GroupParent, exit)
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
              new GroupWithData(groupWithData.GroupParent,
                groupWithData.Elements.Select(element => element.Append(new T())).ToArray())
          ).ToArray()
      );

    public Selection Append(VisualTreeAsset asset)
    => new Selection(
      _groups.Select(groupWithData => 
        new GroupWithData(groupWithData.GroupParent,
          groupWithData.Elements.Select(element => element.Append(asset.CloneTree().contentContainer)).ToArray())
        ).ToArray()
      );
      
    #endregion
    
  }
}