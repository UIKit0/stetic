using Gtk;
using Gnome;
using System;
using System.Reflection;

namespace Stetic {

	public class SteticMain  {

		static Gnome.Program program;

		public static int Main (string[] args) {
			Gtk.Window win;
			Gtk.Notebook notebook;
			Stetic.Palette palette;

			program = new Gnome.Program ("Stetic", "0.0", Modules.UI, args);

			win = new Gtk.Window ("Palette");
			win.AllowGrow = false;
			win.DeleteEvent += Window_Delete;
			palette = new Stetic.Palette ();

			AssemblyName an = new AssemblyName ();
			an.Name = "libsteticwidgets";
			palette.AddWidgets (System.Reflection.Assembly.Load (an));

			win.Add (palette);
			win.ShowAll ();

			win = new Gtk.Window ("Properties");
			win.DeleteEvent += Window_Delete;
			notebook = new Gtk.Notebook ();
			win.Add (notebook);
			Properties = new Stetic.PropertyGrid ();
			Properties.Show ();
			notebook.AppendPage (Properties, new Label ("Properties"));
			ChildProperties = new Stetic.ChildPropertyGrid ();
			ChildProperties.Show ();
			notebook.AppendPage (ChildProperties, new Label ("Packing"));
			win.ShowAll ();

			program.Run ();
			return 0;
		}

		static void Window_Delete (object obj, DeleteEventArgs args) {
			program.Quit ();
			args.RetVal = true;
		}

		static Stetic.PropertyGrid Properties;
		static Stetic.ChildPropertyGrid ChildProperties;

		public static void NoSelection ()
		{
			Properties.NoSelection ();
			ChildProperties.NoSelection ();
		}

		public static void Select (IWidgetSite site)
		{
			Properties.Select (site);
			ChildProperties.Select (site);
		}

	}
}
