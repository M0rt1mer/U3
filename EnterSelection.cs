using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace U3
{
  public class EnterSelection<DataType>
  {

    public struct EnterGroup
    {
      public readonly VisualElement GroupParent;
      public readonly IReadOnlyCollection<DataType> Bindings;
      public EnterGroup(VisualElement groupParent, IReadOnlyCollection<DataType> bindings)
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

    public EnterSelection() {}

    public Selection<T,DataType> Append<T>() where T : VisualElement, new()
      => new Selection<T,DataType>(
        Groups.Select(groupWithData =>
          new Selection<T,DataType>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Bindings.Select(dataBind => groupWithData.GroupParent.Append(new T()).BindData(dataBind)).ToArray())
        ).ToArray()
      );

    public Selection<VisualElement,DataType> Append(VisualTreeAsset asset)
      => new Selection<VisualElement,DataType>(
            Groups.Select(groupWithData =>
              new Selection<VisualElement,DataType>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Bindings.Select(dataBind => groupWithData.GroupParent.Append(asset.CloneTree().contentContainer.FirstChild()).BindData(dataBind)).ToArray())
            ).ToArray()
    );
    
  }
}