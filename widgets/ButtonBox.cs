using Gtk;
using System;

namespace Stetic.Widget {

	public static class ButtonBox {
		public static PropertyGroup ButtonBoxProperties;
		public static PropertyGroup ButtonBoxChildProperties;

		static ButtonBox () {
			ButtonBoxProperties = new PropertyGroup ("Button Box Properties",
								 typeof (Gtk.ButtonBox),
								 "LayoutStyle",
								 "Homogeneous",
								 "Spacing",
								 "BorderWidth");
			ButtonBoxChildProperties = new PropertyGroup ("Button Box Child Layout",
								      typeof (Gtk.ButtonBox.ButtonBoxChild),
								      "Secondary");
		}
	}
}