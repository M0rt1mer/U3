using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace U3
{
  public class EnterSelection<DataType,TParentDataType>
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

    public Selection<T,DataType,TParentDataType> Append<T>(string name = null) where T : VisualElement, new()
      => new Selection<T,DataType,TParentDataType>(
        Groups.Select(groupWithData =>
          new Selection<T,DataType,TParentDataType>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Bindings.Select(dataBind => groupWithData.GroupParent.Append(new T(), name).BindData(dataBind)).ToArray())
        ).ToArray()
      );

    public Selection<VisualElement,DataType,TParentDataType> Append(VisualTreeAsset asset)
      => new Selection<VisualElement,DataType,TParentDataType>(
            Groups.Select(groupWithData =>
              new Selection<VisualElement,DataType,TParentDataType>.GroupWithData(groupWithData.GroupParent,
            groupWithData.Bindings.Select(dataBind => groupWithData.GroupParent.Append(asset.CloneTree().contentContainer.FirstChild()).BindData(dataBind)).ToArray())
            ).ToArray()
    );
    
  }
}