using System.Linq;
using UnityEngine.UIElements;

namespace U3
{
  public static class U3
  {

    public static Selection<VisualElement,object> SelectAll(this VisualElement from, string name)
    {
      return new Selection<VisualElement,object>(new VisualElement[]{from}).SelectAll(name);
    }

    public static Selection<T,object> SelectAll<T>(this VisualElement from, string name) where T:VisualElement
    {
      return new Selection<VisualElement, object>(new VisualElement[] { from }).SelectAll<T>(name);
    }

    public static Selection<VisualElement,object> Find(this VisualElement from, string name)
    {
      return new Selection<VisualElement,object>(new VisualElement[] { from }).Find(name);
    }

    public static Selection<T,object> Find<T>(this VisualElement from, string name) where T : VisualElement
    {
      return new Selection<VisualElement, object>(new VisualElement[] { from }).Find<T>(name);
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
      element.userData = data;
      return element;
    }

    public static object GetBoundData(this VisualElement element)
    {
      return element.userData;
    }

    public static VisualElement FirstChild(this VisualElement element)
    {
      return element.childCount == 0 ? null : element.Children().First();
    }

  }
}