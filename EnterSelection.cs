using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace U3
{
  public struct EnterSelection
  {

    public struct EnterGroup
    {
      public readonly VisualElement GroupParent;
      public readonly IReadOnlyCollection<object> Bindings;
      public EnterGroup(VisualElement groupParent, IReadOnlyCollection<object> bindings)
      {
        GroupParent = groupParent;
        Bindings = bindings;
      }
    }

    internal IReadOnlyCollection<EnterGroup> Groups;

    public EnterSelection(IReadOnlyCollection<EnterGroup> groups)
    {
      Groups = groups;
    }

    public Selection Append<T>() where T : VisualElement, new()
      => new Selection(
        Groups.Select(groupWithData =>
          new Selection.GroupWithData(groupWithData.GroupParent, null,
            groupWithData.Bindings.Select(dataBind => groupWithData.GroupParent.Append(new T()).BindData(dataBind)).ToArray())
        ).ToArray()
      );

    public Selection Append(VisualTreeAsset asset)
      => new Selection(
            Groups.Select(groupWithData =>
              new Selection.GroupWithData(groupWithData.GroupParent, null,
            groupWithData.Bindings.Select(dataBind => groupWithData.GroupParent.Append(asset.CloneTree().contentContainer).BindData(dataBind)).ToArray())
            ).ToArray()
    );

  }
}