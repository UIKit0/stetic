using Gtk;
using Gdk;
using GLib;
using System;

namespace Stetic.Wrapper {

	[WidgetWrapper ("Check Box", "checkbutton.png")]
	public class CheckButton : Gtk.CheckButton, Stetic.IObjectWrapper {
		static PropertyGroup[] groups;
		public PropertyGroup[] PropertyGroups { get { return groups; } }

		public static PropertyGroup CheckButtonProperties;
		public static PropertyGroup CheckButtonExtraProperties;

		static CheckButton () {
			PropertyDescriptor[] props;

			props = new PropertyDescriptor[] {
				new PropertyDescriptor (typeof (Gtk.Button), "Label"),
				new PropertyDescriptor (typeof (Gtk.ToggleButton), "Active"),
				new PropertyDescriptor (typeof (Gtk.ToggleButton), "Inconsistent"),
				new PropertyDescriptor (typeof (Gtk.ToggleButton), "DrawIndicator"),
			};				
			CheckButtonProperties = new PropertyGroup ("Check Button Properties", props);

			groups = new PropertyGroup[] {
				CheckButtonProperties, Button.ButtonExtraProperties,
				Widget.CommonWidgetProperties
			};
		}

		public CheckButton () : base ("Check Box") {}
	}
}
