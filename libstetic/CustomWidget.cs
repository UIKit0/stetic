
using System;

namespace Stetic
{
	// This widget is used at design-time to represent a Gtk.Bin container.
	// Gtk.Bin is the base class for custom widgets.
	
	internal class CustomWidget: Gtk.EventBox
	{
		public CustomWidget ()
		{
			this.VisibleWindow = false;
			this.Events |= Gdk.EventMask.ButtonPressMask;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			// Avoid forwarding event to parent widget
			return true;
		}
	}
}
