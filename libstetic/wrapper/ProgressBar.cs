using System;

namespace Stetic.Wrapper {

	[ObjectWrapper ("ProgressBar", "progressbar.png", ObjectWrapperType.Widget)]
	public class ProgressBar : Widget {

		public static new Type WrappedType = typeof (Gtk.ProgressBar);

		static new void Register (Type type)
		{
			AddItemGroup (type, "Progress Bar Properties",
				      "Orientation",
				      "Text",
				      "PulseStep");
		}
	}
}
