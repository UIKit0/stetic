using System;

namespace Stetic.Wrapper {

	[ObjectWrapper ("VBox", "vbox.png", ObjectWrapperType.Container)]
	public class VBox : Box {
		public VBox (IStetic stetic) : this (stetic, new Gtk.VBox (false, 0)) {}

		public VBox (IStetic stetic, Gtk.VBox vbox) : base (stetic, vbox) {}

		public override bool HExpandable {
			get {
				foreach (WidgetSite site in Sites) {
					if (!site.HExpandable)
						return false;
				}
				return true;
			}
		}

		public override bool VExpandable {
			get {
				foreach (WidgetSite site in Sites) {
					if (site.VExpandable)
						return true;
				}
				return false;
			}
		}
	}
}