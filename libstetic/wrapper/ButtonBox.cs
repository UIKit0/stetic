using System;
using System.Xml;
using System.Collections;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class ButtonBox : Box {

		Dialog actionDialog;
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			foreach (Gtk.Widget child in buttonbox.Children) {
				if (child is Placeholder)
					ReplaceChild (child, NewButton ());
			}
		}
		
		public void SetActionDialog (Dialog dialog)
		{
			actionDialog = dialog;
		}

		Gtk.Button NewButton ()
		{
			Gtk.Button button = (Gtk.Button)Registry.NewInstance ("Gtk.Button", proj);
			if (InternalChildProperty != null && InternalChildProperty.Name == "ActionArea")
				((Button)Widget.Lookup (button)).IsDialogButton = true;
			return button;
		}

		protected Gtk.ButtonBox buttonbox {
			get {
				return (Gtk.ButtonBox)Wrapped;
			}
		}

		protected override bool AllowPlaceholders {
			get {
				return false;
			}
		}
		internal new void InsertBefore (Gtk.Widget context)
		{
			int position;
			bool secondary;

			if (context == buttonbox) {
				position = 0;
				secondary = false;
			} else {
				Gtk.ButtonBox.ButtonBoxChild bbc = (Gtk.ButtonBox.ButtonBoxChild)ContextChildProps (context);
				position = bbc.Position;
				secondary = bbc.Secondary;
			}

			Gtk.Button button = NewButton ();
			buttonbox.PackStart (button, false, false, 0);
			buttonbox.ReorderChild (button, position);
			buttonbox.SetChildSecondary (button, secondary);
			EmitContentsChanged ();
		}

		internal new void InsertAfter (Gtk.Widget context)
		{
			int position;
			bool secondary;

			if (context == buttonbox) {
				position = buttonbox.Children.Length - 1;
				secondary = false;
			} else {
				Gtk.ButtonBox.ButtonBoxChild bbc = (Gtk.ButtonBox.ButtonBoxChild)ContextChildProps (context);
				position = bbc.Position;
				secondary = bbc.Secondary;
			}

			Gtk.Button button = NewButton ();
			buttonbox.PackStart (button, false, false, 0);
			buttonbox.ReorderChild (button, position + 1);
			buttonbox.SetChildSecondary (button, secondary);
			EmitContentsChanged ();
		}

		public int Size {
			get {
				return buttonbox.Children.Length;
			}
			set {
				Gtk.Widget[] children = buttonbox.Children;
				int cursize = children.Length;

				while (cursize > value) {
					Gtk.Widget w = children[--cursize];
					buttonbox.Remove (w);
					w.Destroy ();
				}
				while (cursize < value) {
					buttonbox.PackStart (NewButton (), false, false, 0);
					cursize++;
				}
			}
		}
		
		protected override void ReadChildren (XmlElement elem, FileFormat format)
		{
			// Reset the button count
			Size = 0;
			base.ReadChildren (elem, format);
		}
		
		protected override void GenerateChildBuildCode (GeneratorContext ctx, string parentVar, Widget wrapper)
		{
			if (actionDialog != null && wrapper is Button) {
			
				// If this is the action area of a dialog, buttons must be added using AddActionWidget,
				// so they are properly registered.
				
				ObjectWrapper childwrapper = ChildWrapper (wrapper);
				Button button = wrapper as Button;
				
				if (childwrapper != null) {
					string dialogVarName = ctx.WidgetMap.GetWidgetId (actionDialog);
					ctx.Statements.Add (new CodeCommentStatement ("Container child " + Wrapped.Name + "." + childwrapper.Wrapped.GetType ()));
					string varName = ctx.GenerateNewInstanceCode (wrapper);
					CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression (
						new CodeVariableReferenceExpression (dialogVarName),
						"AddActionWidget",
						new CodeVariableReferenceExpression (varName),
						new CodePrimitiveExpression (button.ResponseId)
					);
					ctx.Statements.Add (invoke);
					GenerateSetPacking (ctx, parentVar, varName, childwrapper);
				}
			} else
				base.GenerateChildBuildCode (ctx, parentVar, wrapper);
		}

		public class ButtonBoxChild : Box.BoxChild {

			public bool InDialog {
				get {
					if (ParentWrapper == null)
						return false;
					return ParentWrapper.InternalChildProperty != null && ParentWrapper.InternalChildProperty.Name == "ActionArea";
				}
			}
		}
	}
}
