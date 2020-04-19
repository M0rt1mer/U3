using UnityEngine.UIElements;

namespace U3
{
  public class ClassAccessor : Accessor<bool>
  {
    private string UssClass { get; }
    public ClassAccessor(string ussClass) { UssClass = ussClass; }

    public override bool Equals(IAccessor other) => other is ClassAccessor clsAccessor && UssClass.Equals(clsAccessor.UssClass);
    public override bool GetValue(VisualElement elem) => elem.ClassListContains(UssClass);
    public override void SetValue(VisualElement elem, bool value) { elem.EnableInClassList(UssClass, value); }
  }

  public class LabelAccessor : Accessor<string>
  {
    public override string GetValue(VisualElement elem) => ((TextElement)elem).text;
    public override void SetValue(VisualElement elem, string value) { ((TextElement)elem).text = value; }

    public static LabelAccessor Instance { get; } = new LabelAccessor();
    protected LabelAccessor() { }
  }

  public class IntegerLabelAccessor : Accessor<int>
  {
    public override int GetValue(VisualElement elem)
    {
      int.TryParse(((TextElement) elem).text, out var result);
      return result;
    }

    public override void SetValue(VisualElement elem, int value) => ((TextElement)elem).text = value.ToString();

    public static IntegerLabelAccessor Instance { get; } = new IntegerLabelAccessor();
    protected IntegerLabelAccessor() { }
  }

  public class StyleLeftAccessor : Accessor<float>
  {
    public override float GetValue(VisualElement elem) => elem.style.left.value.value;
    public override void SetValue(VisualElement elem, float value) => elem.style.left = value;

    public static StyleLeftAccessor Instance { get; } = new StyleLeftAccessor();
    protected StyleLeftAccessor() { }
  }

}