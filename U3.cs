using System.Linq;
using UnityEngine.UIElements;

namespace U3
{
  public static class U3
  {

    public static Selection<VisualElement,object, object> SelectAll(this VisualElement from, string name)
    {
      return new Selection<VisualElement,object, object>( new[]{from} ).SelectAll(name);
    }

    public static Selection<T,object, object> SelectAll<T>(this VisualElement from, string name) where T:VisualElement
    {
      return new Selection<VisualElement, object, object>( new[] { from }).SelectAll<T>(name);
    }

    public static Selection<VisualElement,object, object> Find(this VisualElement from, string name)
    {
      return new Selection<VisualElement,object, object>( new[] { from } ).Find(name);
    }

    public static Selection<T,object, object> Find<T>(this VisualElement from, string name) where T : VisualElement
    {
      return new Selection<VisualElement, object, object>( new[] { from } ).Find<T>(name);
    }
    
    public static T Append<T>(this VisualElement element, T newChild, string name = null)
      where T: VisualElement
    {
      element.Add(newChild);
      if (name != null)
        newChild.name = name;
      return newChild;
    }

    public static T BindData<T>(this T element, object data)
      where T:VisualElement
    {
      element.userData = new DataBinding(data, element);
      return element;
    }

    internal static DataBinding GetOrCreateDataBinding(this VisualElement element)
    {
      DataBinding dBinding = element.userData as DataBinding;
      if (dBinding == null)
        element.userData = dBinding = new DataBinding(null, element);
      return dBinding;
    }

    internal static object GetBoundData(this VisualElement element)
    {
      return element.GetOrCreateDataBinding()?.BoundData;
    }

    public static VisualElement FirstChild(this VisualElement element)
    {
      return element.childCount == 0 ? null : element.Children().First();
    }

  }
}