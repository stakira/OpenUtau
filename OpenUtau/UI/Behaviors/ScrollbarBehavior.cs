using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;

namespace OpenUtau.UI.Behaviors
{
    class ScrollbarBehavior : Behavior<ScrollBar>
    {
        ScrollBar scrollbar;

        protected override void OnAttached()
        {
            scrollbar = AssociatedObject;
            scrollbar.MouseWheel += scrollbar_MouseWheel;
        }

        void scrollbar_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            scrollbar.Value = Math.Max(scrollbar.Minimum, Math.Min(scrollbar.Maximum, scrollbar.Value - 0.01 * scrollbar.SmallChange * e.Delta));
        }
    }
}
