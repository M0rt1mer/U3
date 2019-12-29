using UnityEngine.UIElements;

namespace U3
{
  public static class U3
  {

    public static Selection SelectAll(this VisualElement from, string name)
    {
      return new Selection(new VisualElement[]{from}).SelectAll(name);
    }

    public static Selection SelectAll<T>(this VisualElement from, string name) where T:VisualElement
    {
      return new Selection(new VisualElement[] { from }).SelectAll<T>(name);
    }

    public static Selection Find(this VisualElement from, string name)
    {
      return new Selection(new VisualElement[] { from }).Find(name);
    }

    public static Selection Find<T>(this VisualElement from, string name) where T : VisualElement
    {
      return new Selection(new VisualElement[] { from }).Find<T>(name);
    }

    public static void ExitDelete(VisualElement element)
    {
      element.parent.Remove(element);
    }

    public static void EnterNoop(VisualElement element, object dataBinding) {}

    public static VisualElement Append(this VisualElement element, VisualElement newChild)
    {
      element.Add(newChild);
      return newChild;
    }

    public static VisualElement BindData(this VisualElement element, object data)
    {
      element.userData = data;
      return element;
    }

    public static object GetBoundData(this VisualElement element)
    {
      return element.userData;
    }

  }
}