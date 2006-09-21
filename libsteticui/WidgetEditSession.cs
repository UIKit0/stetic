//
// WidgetEditSession.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.CodeDom;
using Mono.Unix;

namespace Stetic {
    
	internal class WidgetEditSession: MarshalByRefObject, IDisposable
	{
		string sourceWidget;
		Stetic.ProjectBackend sourceProject;
		
		Stetic.ProjectBackend gproject;
		Stetic.Wrapper.Container rootWidget;
		Stetic.WidgetDesignerBackend widget;
		Gtk.VBox designer;
		Gtk.Plug plug;
		bool autoCommitChanges;
		WidgetActionBar toolbar;
		WidgetDesignerFrontend frontend;
		bool allowBinding;
		
		public event EventHandler ModifiedChanged;
		public event EventHandler RootWidgetChanged;
		
		public WidgetEditSession (WidgetDesignerFrontend frontend, Stetic.Wrapper.Container win, Stetic.ProjectBackend editingBackend, bool autoCommitChanges)
		{
			this.frontend = frontend;
			this.autoCommitChanges = autoCommitChanges;
			
			sourceWidget = win.Wrapped.Name;
			sourceProject = (ProjectBackend) win.Project;
			gproject = editingBackend;
			
			if (!autoCommitChanges) {
				// Reuse the action groups and icon factory of the main project
				gproject = editingBackend;
				gproject.ActionGroups = win.Project.ActionGroups;
				gproject.IconFactory = win.Project.IconFactory;
				gproject.ResourceProvider = win.Project.ResourceProvider;
				gproject.WidgetLibraries = ((ProjectBackend)win.Project).WidgetLibraries;
				
				rootWidget = editingBackend.GetTopLevelWrapper (sourceWidget, false);
				if (rootWidget == null) {
					// Copy the widget to edit from the source project
					// When saving the file, this project will be merged with the main project.
					XmlElement data = Stetic.WidgetUtils.ExportWidget (win.Wrapped);
					Gtk.Widget w = Stetic.WidgetUtils.ImportWidget (gproject, data);
					gproject.AddWidget (w);
					rootWidget = Stetic.Wrapper.Container.Lookup (w);
				}
				
				gproject.Modified = false;
			}
			else {
				gproject = (Stetic.ProjectBackend) win.Project;
				rootWidget = win;
			}
			
			rootWidget.Select ();
			
			gproject.ModifiedChanged += new EventHandler (OnModifiedChanged);
			gproject.ProjectReloaded += new EventHandler (OnProjectReloaded);
//			gproject.WidgetMemberNameChanged += new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
		}
		
		public bool AllowWidgetBinding {
			get { return allowBinding; }
			set {
				allowBinding = value;
				if (toolbar != null)
					toolbar.AllowWidgetBinding = allowBinding;
			}
		}
		
		public Stetic.Wrapper.Widget GladeWidget {
			get { return rootWidget; }
		}
		
		public Stetic.Wrapper.Container RootWidget {
			get { return (Wrapper.Container) Component.GetSafeReference (rootWidget); }
		}
		
		public Gtk.Widget WrapperWidget {
			get {
				if (designer == null) {
					Gtk.Container wc = rootWidget.Wrapped as Gtk.Container;
					if (widget == null)
						widget = Stetic.UserInterface.CreateWidgetDesigner (wc, rootWidget.DesignWidth, rootWidget.DesignHeight);
					
					toolbar = new WidgetActionBar (frontend, rootWidget);
					toolbar.AllowWidgetBinding = allowBinding;
					designer = new Gtk.VBox ();
					designer.BorderWidth = 3;
					designer.PackStart (toolbar, false, false, 0);
					designer.PackStart (widget, true, true, 3);
					widget.DesignArea.SetSelection (gproject.Selection, gproject.Selection);
					widget.SelectionChanged += OnSelectionChanged;
				
				}
				return designer; 
			}
		}
		
		[NoGuiDispatch]
		public void CreateWrapperWidgetPlug (uint socketId)
		{
			Gdk.Threads.Enter ();
			plug = new Gtk.Plug (socketId);
			plug.Add (WrapperWidget);
			plug.Decorated = false;
			plug.ShowAll ();
			Gdk.Threads.Leave ();
		}
		
		public void Save ()
		{
			if (!autoCommitChanges) {
				XmlElement data = Stetic.WidgetUtils.ExportWidget (rootWidget.Wrapped);
				
				Wrapper.Widget sw = sourceProject.GetTopLevelWrapper (sourceWidget, false);
				if (sw != null)
					sw.Delete ();
				
				Gtk.Widget w = Stetic.WidgetUtils.ImportWidget (sourceProject, data);
				sourceWidget = w.Name;
				sourceProject.AddWidget (w);
				
				gproject.Modified = false;
			}
		}
		
		public ProjectBackend EditingBackend {
			get { return gproject; }
		}
		
		public void Dispose ()
		{
			if (toolbar != null)
				toolbar.Destroy ();
			
//			gproject.WidgetMemberNameChanged -= new Stetic.Wrapper.WidgetNameChangedHandler (OnWidgetNameChanged);
			
			if (!autoCommitChanges) {
				// The global action group is being managed by the real stetic project,
				// so we need to remove it from the project copy before disposing it.
				gproject.ActionGroups = null;
				gproject.Dispose ();
				if (widget != null) {
					widget.SelectionChanged -= OnSelectionChanged;
					widget.Dispose ();
					widget.Destroy ();
					widget = null;
				}
			}
			if (plug != null)
				plug.Destroy ();
			gproject = null;
			rootWidget = null;
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
		
		public void SetDesignerActive ()
		{
			widget.UpdateObjectViewers ();
		}
		
		public bool Modified {
			get { return gproject.Modified; }
		}
		
		void OnModifiedChanged (object s, EventArgs a)
		{
			frontend.NotifyModifiedChanged ();
		}
		
		void OnProjectReloaded (object s, EventArgs a)
		{
			Gtk.Widget[] tops = gproject.Toplevels;
			if (tops.Length > 0) {
				rootWidget = Stetic.Wrapper.Container.Lookup (tops[0]);
				if (rootWidget != null) {
					WidgetDesignerBackend oldWidget = widget;
					if (widget != null) {
						widget.SelectionChanged -= OnSelectionChanged;
						widget = null;
					}
					OnRootWidgetChanged ();
					if (oldWidget != null)
						oldWidget.Destroy ();
					return;
				}
			}
			SetErrorMode ();
		}
		
		void SetErrorMode ()
		{
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = "<b>" + Catalog.GetString ("The form designer could not be loaded") + "</b>";
			Gtk.EventBox box = new Gtk.EventBox ();
			box.Add (lab);
			
			widget = Stetic.UserInterface.CreateWidgetDesigner (box, 100, 100);
			
			OnRootWidgetChanged ();
		}
		
		void OnRootWidgetChanged ()
		{
			if (designer != null) {
				designer.Dispose ();
				designer = null;
			}
			
			if (RootWidgetChanged != null)
				RootWidgetChanged (this, EventArgs.Empty);
		}
		
		void OnSelectionChanged (object ob, EventArgs a)
		{
			if (frontend != null)
				frontend.NotifySelectionChanged (Component.GetSafeReference (ObjectWrapper.Lookup (widget.Selection)));
		}
		
		public string SaveState ()
		{
			return null;
		}
		
		public void RestoreState (string stateData)
		{
		}
	}
}