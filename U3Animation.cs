using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace U3
{
  public static class Animation
  {

    public static ValueAnimation<float> CreateFromCurve(VisualElement e, AnimationCurve curve)
    {
      var animation = ValueAnimation<float>.Create(e, null );
      animation.durationMs = 10000;
      //animation.Ease( curve.Evaluate );
      animation.Start();
      animation.to = 1;
      animation.interpolator = (f, f1, arg3) => (f1 - f) * arg3 + f;
      var a = e.panel;
      return animation;
    }

    public static ValueAnimation<float> MoveX(this ValueAnimation<float> anim, float multiplier)
    {
      anim.valueUpdated = 
        (element, f) =>
        {
          element.style.left = f * multiplier;
        };
      return anim;
    }

  }
}