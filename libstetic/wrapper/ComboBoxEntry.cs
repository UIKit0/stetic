using System;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class ComboBoxEntry : ComboBox {

		public static new Gtk.ComboBoxEntry CreateInstance ()
		{
			Gtk.ComboBoxEntry c = Gtk.ComboBoxEntry.NewText ();
			// Make sure all children are created, so the mouse events can be
			// bound and the widget can be selected.
			c.EnsureStyle ();
			return c;
		}
		
		internal protected override CodeExpression GenerateWidgetCreation (GeneratorContext ctx)
		{
			if (Items != null && Items.Length > 0) {
				return new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression ("Gtk.ComboBoxEntry"),
					"NewText"
				);
			} else
				return base.GenerateWidgetCreation (ctx);
		}
	}
}
