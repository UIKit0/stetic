using Gtk;
using System;
using System.Collections;
using System.Reflection;

namespace Stetic {

	public class ContextMenu : Gtk.Menu {

		IWidgetSite site;

		public ContextMenu (IWidgetSite site) : this (site, site) {}

		public ContextMenu (IWidgetSite site, IWidgetSite top)
		{
			MenuItem item;

			this.site = site;

			if (top == site) {
				item = LabelItem (site.Occupied ? site.Contents.Name : "Placeholder");
				item.Sensitive = false;
				Add (item);
			}

			item = new MenuItem ("_Select");
			item.Activated += DoSelect;
			Add (item);

			Stetic.Wrapper.Object wrapper = Stetic.Wrapper.Object.Lookup (site.Contents);
			if (wrapper is IContextMenuProvider) {
				foreach (ContextMenuItem cmi in ((IContextMenuProvider)wrapper).ContextMenuItems (top)) {
					item = new MenuItem (cmi.Label);
					if (cmi.Enabled)
						item.Activated += new Stupid69614Workaround (top, cmi.Callback).Activate;
					else
						item.Sensitive = false;
					Add (item);
				}
			}

			item = new MenuItem ("Cu_t");
			if (!site.Occupied)
				item.Sensitive = false;
			Add (item);
			item = new MenuItem ("_Copy");
			if (!site.Occupied)
				item.Sensitive = false;
			Add (item);
			item = new MenuItem ("_Paste");
			if (site.Occupied)
				item.Sensitive = false;
			Add (item);
			item = new MenuItem ("_Delete");
			if (site.Occupied)
				item.Activated += DoDelete;
			else
				item.Sensitive = false;
			Add (item);

			if (site == top) {
				for (site = site.ParentSite; site != null; site = site.ParentSite) {
					Add (new SeparatorMenuItem ());

					item = LabelItem (site.Contents.Name);
					item.Submenu = new ContextMenu (site, top);
					Add (item);
				}
			}

			ShowAll ();
		}

		private class Stupid69614Workaround {
			IWidgetSite top;
			ContextMenuItemDelegate callback;

			public Stupid69614Workaround (IWidgetSite top, ContextMenuItemDelegate callback) {
				this.top = top;
				this.callback = callback;
			}

			public void Activate (object obj, EventArgs args) {
				callback (top);
			}
		}

		void DoSelect (object obj, EventArgs args)
		{
			site.Select ();
		}

		void DoDelete (object obj, EventArgs args)
		{
			site.Delete ();
		}

		static MenuItem LabelItem (string labelString)
		{
			MenuItem item;
			Label label;

			label = new Label (labelString);
			label.UseUnderline = false;
			label.SetAlignment (0.0f, 0.5f);
			item = new MenuItem ();
			item.Add (label);

			return item;
		}
	}
}
