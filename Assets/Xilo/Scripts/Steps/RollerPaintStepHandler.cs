using UnityEngine;
using Xilo.Objects;

namespace Xilo.Steps
{
    // Matrix inking: same as a painting step, but only a loaded roller leaves ink.
    public sealed class RollerPaintStepHandler : PaintStepHandler
    {
        [SerializeField] RollerInk rollerInk;

        protected override bool CanPaint() => rollerInk != null && rollerInk.IsLoaded;
    }
}
