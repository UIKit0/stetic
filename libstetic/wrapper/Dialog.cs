using System;

namespace Stetic.Wrapper {

	[ObjectWrapper ("Dialog Box", "dialog.png", ObjectWrapperType.Window)]
	public class Dialog : Window {

		public static ItemGroup DialogProperties;
		public static ItemGroup DialogMiscProperties;

		static Dialog () {
			DialogProperties = new ItemGroup ("Dialog Properties",
							  typeof (Gtk.Dialog),
							  "Title",
							  "Icon",
							  "WindowPosition",
							  "Modal",
							  "BorderWidth");
			DialogMiscProperties = new ItemGroup ("Miscellaneous Dialog Properties",
							      typeof (Gtk.Dialog),
							      "HasSeparator",
							      "AcceptFocus",
							      "Decorated",
							      "DestroyWithParent",
							      "Gravity",
							      "Role",
							      "SkipPagerHint",
							      "SkipTaskbarHint");
			RegisterWrapper (typeof (Stetic.Wrapper.Dialog),
					 DialogProperties,
					 DialogMiscProperties,
					 Window.WindowSizeProperties,
					 Widget.CommonWidgetProperties);
		}

		public Dialog (IStetic stetic) : this (stetic, new Gtk.Dialog ()) {}

		public Dialog (IStetic stetic, Gtk.Dialog dialog) : base (stetic, dialog)
		{
			WidgetSite site = CreateWidgetSite ();
			Gtk.Requisition req;
			req.Width = req.Height = 200;
			site.EmptySize = req;

			dialog.VBox.Add (site);
		}
	}
}
