using UnityEngine;
using UnityEngine.UIElements;

namespace U3
{
  public static class AnimationHelpers
  {

    public static AnimationBuilderOnChange<T> Animate<T>(int duration)
    {
      return new AnimationBuilderOnChange<T>(duration);
    }

    public static AnimationBuilderManual<T> Animate<T>(this VisualElement element, Accessor<T> acc, T from, T to, int duration)
    {
      return new AnimationBuilderManual<T>(element, acc, from, to, duration);
    }

  }

  public abstract class AnimationBuilderBase<TValueType>
  {
    public delegate void Modify(TValueType oldValue, TValueType newValue, ValueAnimatedDataChange<TValueType> vadc);
    protected Modify _actions;
    protected readonly int _duration;

    protected AnimationBuilderBase(int duration)
    {
      _actions = null;
      _duration = duration;
    }

    /// <summary>
    /// Adds a modifier to the list. Modifier is a callback that is called with the created <see cref="ValueAnimatedDataChange{T}"/>, IFF it is created at all.
    /// Modifiers are called before <see cref="ValueAnimatedDataChange{T}"/> is initialized, e.g. it's properties are NOT filled in.
    /// </summary>
    /// <param name="modifier">The callback function</param>
    public void AddModifier(Modify modifier)
    {
      if (_actions == null)
        _actions = modifier;
      else
        _actions += modifier;
    }
  }

  public class AnimationBuilderManual<TValueType> : AnimationBuilderBase<TValueType>
  {
    private readonly VisualElement _element;
    private readonly Accessor<TValueType> _accessor;
    private readonly TValueType _from;
    private readonly TValueType _to;
    private bool _started;

    internal AnimationBuilderManual(VisualElement element, Accessor<TValueType> accessor, TValueType @from, TValueType to, int duration) : base(duration)
    {
      _element = element;
      _accessor = accessor;
      _from = @from;
      _to = to;
      _started = false;
    }

    public void Start()
    {
      Debug.Assert(_started == false, "Animation can only be started once!");
      _started = true;

      var valueAnimatedDataChange = new ValueAnimatedDataChange<TValueType>(_element, _duration);
      _actions?.Invoke(_from, _to, valueAnimatedDataChange);
      valueAnimatedDataChange.Initialize( _accessor, _element, _from, _to );
      _element.GetOrCreateDataBinding().RegisterAnimation(_accessor, valueAnimatedDataChange);
    }

    ~AnimationBuilderManual()
    {
      Debug.Assert(_started, "Animation was constructed but never started!");
    }

  }

  public class AnimationBuilderOnChange<TValueType> : AnimationBuilderBase<TValueType>
  {
    public AnimationBuilderOnChange(int duration) : base(duration) { }

    internal ValueAnimatedDataChange<TValueType> Build(VisualElement element, TValueType oldValue, TValueType newValue)
    {
      var valueAnimatedDataChange = new ValueAnimatedDataChange<TValueType>(element, _duration);
      _actions?.Invoke(oldValue, newValue, valueAnimatedDataChange);
      return valueAnimatedDataChange;
    }

    public Selection<TElementType, TDataType, TParentDataType>.OnChangeCall<TValueType> Prepare<TElementType, TDataType, TParentDataType>()
      where TElementType:VisualElement
    {
      return (element, data, parentData, value, newValue) => Build(element, value, newValue);
    }
  }

  public static class AnimationBuilderHelpers
  {
    public static AnimBuilderType Curve<AnimBuilderType>(this AnimBuilderType builder, AnimationCurve curve)
      where AnimBuilderType : AnimationBuilderBase<float>
    {
      builder.AddModifier(((oldValue, newValue, vadc) => vadc.ValueAnimation.Ease(curve.Evaluate)));
      return builder;
    }
  }

}