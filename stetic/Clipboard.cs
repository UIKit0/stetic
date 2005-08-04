using Gtk;
using System;

namespace Stetic {

	public static class Clipboard {

		const int GladeType = 0;
		const int TextType = 1;

		static Gtk.TargetEntry[] targets;
		static Gtk.TargetEntry[] Targets {
			get {
				if (targets == null) {
					Gtk.TargetList list = new Gtk.TargetList ();
					list.Add ((string)GladeUtils.ApplicationXGladeAtom, 0, GladeType);
					list.AddTextTargets (TextType);
					targets = (Gtk.TargetEntry[])list;
				}
				return targets;
			}
		}

		static Gtk.Clipboard MainClipboard {
			get {
				return Gtk.Clipboard.Get (Gdk.Selection.Clipboard);
			}
		}

		static Gtk.Widget selection;

		static void ClipboardGet (Gtk.Clipboard clipboard, Gtk.SelectionData seldata, uint info)
		{
			if (selection != null)
				GladeUtils.Copy (selection, seldata, info == TextType);
		}

		static void ClipboardClear (Gtk.Clipboard clipboard)
		{
			selection = null;
		}

		public static void Copy (Gtk.Widget widget)
		{
			MainClipboard.SetWithData (Targets, ClipboardGet, ClipboardClear);
			selection = widget;
		}

		public static void Cut (Gtk.Widget widget)
		{
			Copy (widget);
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (widget);
			if (wrapper != null)
				wrapper.Delete ();
		}

		static Placeholder target;

		static void ClipboardPaste (Gtk.Clipboard clipboard, Gtk.SelectionData seldata)
		{
			Stetic.Wrapper.Container parent = Stetic.Wrapper.Container.LookupParent (target);
			if (parent == null)
				return;

			Stetic.Wrapper.Widget wrapper = GladeUtils.Paste (parent.Project, seldata);
			if (wrapper == null)
				return;

			parent.ReplaceChild (target, wrapper.Wrapped);
			target = null;
		}

		public static void Paste (Placeholder target)
		{
			Clipboard.target = target;
			MainClipboard.RequestContents (GladeUtils.ApplicationXGladeAtom, ClipboardPaste);
		}
	}
}