
using System;
using System.Collections;
using Stetic.Wrapper;

namespace Stetic.Editor
{
	public class ActionGroupEditor: Gtk.EventBox, IMenuItemContainer
	{
		ActionGroup actionGroup;
		Gtk.Table table;
		IProject project;
		ArrayList items = new ArrayList ();
		Gtk.EventBox emptyLabel;
		EditableLabel headerLabel;
		uint columns = 2;
		bool modified;
		ObjectWrapperEventHandler changedEvent;
		IDesignArea darea;
		
		public EventHandler GroupModified;
		public EventHandler SelectionChanged;
		
		public ActionGroupEditor ()
		{
			changedEvent = new ObjectWrapperEventHandler (OnActionChanged);
			
			Gtk.Fixed fx = new Gtk.Fixed ();
			table = new Gtk.Table (0, 0, false);
			table.RowSpacing = 8;
			table.ColumnSpacing = 8;
			table.BorderWidth = 12;
			
			Gtk.EventBox ebox = new Gtk.EventBox ();
			ebox.ModifyBg (Gtk.StateType.Normal, this.Style.Backgrounds [0]);
			headerLabel = new EditableLabel ();
			headerLabel.MarkupTemplate = "<b>$TEXT</b>";
			headerLabel.Changed += OnGroupNameChanged;
			Gtk.VBox vbox = new Gtk.VBox ();
			Gtk.Label grpLabel = new Gtk.Label ();
			grpLabel.Xalign = 0;
			grpLabel.Markup = "<small><i>Action Group</i></small>";
			vbox.PackStart (grpLabel, false, false, 0);
			vbox.PackStart (headerLabel, false, false, 3);
			vbox.BorderWidth = 12;
			ebox.Add (vbox);
			
			Gtk.VBox box = new Gtk.VBox ();
			box.Spacing = 6;
			box.PackStart (ebox, false, false, 0);
			box.PackStart (table, false, false, 0);
			
			fx.Put (box, 0, 0);
			Add (fx);
			ShowAll ();
		}
		
		public ActionGroup ActionGroup {
			get { return actionGroup; }
			set {
				if (actionGroup != null) {
					actionGroup.Changed -= OnGroupChanged;
					actionGroup.ActionAdded -= OnActionAdded;
					actionGroup.ActionRemoved -= OnActionRemoved;
				}
				actionGroup = value;
				if (actionGroup != null) {
					headerLabel.Text = actionGroup.Name;
					actionGroup.Changed += OnGroupChanged;
					actionGroup.ActionAdded += OnActionAdded;
					actionGroup.ActionRemoved += OnActionRemoved;
					foreach (Action a in actionGroup.Actions)
						a.ObjectChanged += changedEvent;
				}
				Fill ();
			}
		}
		
		public IProject Project {
			get { return project; }
			set { project = value; }
		}
		
		public bool Modified {
			get { return modified; }
			set { modified = value; }
		}
		
		public Action SelectedAction {
			get {
				IDesignArea designArea = GetDesignArea ();
				IObjectSelection sel = designArea.GetSelection ();
				if (sel != null)
					return ObjectWrapper.Lookup (sel.DataObject) as Action;
				else
					return null;
			}
		}
		
		public void StartEditing ()
		{
			IDesignArea designArea = GetDesignArea ();
			designArea.SetSelection (headerLabel, null);
			headerLabel.StartEditing ();
		}
		
		void Fill ()
		{
			IDesignArea designArea = GetDesignArea ();
			if (designArea == null)
				return;

			Action selAction = null;
			
			foreach (ActionMenuItem item in items) {
				if (designArea.IsSelected (item))
					selAction = item.Node.Action;
				item.Detach ();
				item.Destroy ();
			}
			items.Clear ();
			
			if (actionGroup != null) {
				Action[] sortedActions = new Action [actionGroup.Actions.Count];
				actionGroup.Actions.CopyTo (sortedActions, 0);
				Array.Sort (sortedActions, new ActionComparer ());
				for (int n = 0; n < sortedActions.Length; n++) {
					Action action = (Action) sortedActions [n];
					ActionMenuItem item = InsertAction (action, n);
					if (selAction == action)
						item.Select ();
				}
				
				if (selAction == null)
					designArea.SetSelection (null, null);
				
				headerLabel.Sensitive = true;
				PlaceAddLabel (actionGroup.Actions.Count);
			} else {
				HideAddLabel ();
				headerLabel.Text = "<No selection>";
				headerLabel.Sensitive = false;
			}
			ShowAll ();
		}
		
		ActionMenuItem InsertAction (Action action, int n)
		{
			uint row = (uint) n / columns;
			uint col = (uint) (n % columns) * 3;
			
			IDesignArea designArea = GetDesignArea ();
			ActionTreeNode node = new ActionTreeNode (Gtk.UIManagerItemType.Menuitem, "", action);
			ActionMenuItem aitem = new ActionMenuItem (designArea, project, this, node);
			aitem.KeyPressEvent += OnItemKeyPress;
			aitem.MinWidth = 150;
			aitem.Attach (table, row, col);
			
			Gtk.Frame fr = new Gtk.Frame ();
			fr.Shadow = Gtk.ShadowType.Out;
			aitem.Add (fr);
			
			items.Add (aitem);
			return aitem;
		}
		
		void PlaceAddLabel (int n)
		{
			HideAddLabel ();

			uint r = (uint) n / columns;
			uint c = (uint) (n % columns) * 3;
			
			emptyLabel = new Gtk.EventBox ();
			emptyLabel.VisibleWindow = false;
			Gtk.Label label = new Gtk.Label ();
			label.Xalign = 0;
			label.Markup = "<i><span foreground='darkgrey'>Click to create action</span></i>";
			emptyLabel.Add (label);
			emptyLabel.ButtonPressEvent += OnAddClicked;
			table.Attach (emptyLabel, c, c+3, r, r+1);
		}
		
		void HideAddLabel ()
		{
			if (emptyLabel != null)
				table.Remove (emptyLabel);
			emptyLabel = null;
		}
		
		void OnGroupChanged (object s, EventArgs args)
		{
			headerLabel.Text = actionGroup.Name;
			NotifyModified ();
		}
		
		void OnActionAdded (object s, ActionEventArgs args)
		{
			args.Action.ObjectChanged += changedEvent;
			Fill ();
			NotifyModified ();
		}
		
		void OnActionRemoved (object s, ActionEventArgs args)
		{
			args.Action.ObjectChanged -= changedEvent;
			Fill ();
			NotifyModified ();
		}
		
		void OnActionChanged (object s, ObjectWrapperEventArgs args)
		{
			NotifyModified ();
		}
		
		void NotifyModified ()
		{
			modified = true;
			if (GroupModified != null)
				GroupModified (this, EventArgs.Empty);
		}
		
		void OnAddClicked (object s, Gtk.ButtonPressEventArgs args)
		{
			Action ac = (Action) ObjectWrapper.Create (project, new Gtk.Action ("", "", null, null));
			ActionMenuItem item = InsertAction (ac, actionGroup.Actions.Count);
			item.EditingDone += OnEditDone;
			item.Select ();
			item.StartEditing ();
			HideAddLabel ();
			ShowAll ();
		}
		
		void OnEditDone (object sender, EventArgs args)
		{
			ActionMenuItem item = (ActionMenuItem) sender;
			item.EditingDone -= OnEditDone;
			if (item.Node.Action.GtkAction.Label.Length > 0) {
				actionGroup.Actions.Add (item.Node.Action);
			} else {
				IDesignArea designArea = GetDesignArea ();
				designArea.ResetSelection (item);
				item.Detach ();
				items.Remove (item);
				PlaceAddLabel (actionGroup.Actions.Count);
				ShowAll ();
			}
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			IDesignArea designArea = GetDesignArea ();
			designArea.SetSelection (null, null);
			return true;
		}
		
		void OnItemKeyPress (object s, Gtk.KeyPressEventArgs args)
		{
			int pos = items.IndexOf (s);
			
			switch (args.Event.Key) {
				case Gdk.Key.Up:
					pos -= (int) columns;
					break;
				case Gdk.Key.Down:
					pos += (int) columns;
					break;
				case Gdk.Key.Right:
					pos ++;
					break;
				case Gdk.Key.Left:
					pos --;
					break;
			}
			if (pos >= 0 && pos < items.Count) {
				((ActionMenuItem)items[pos]).Select ();
				args.RetVal = true;
			}
			else if (pos == items.Count) {
				OnAddClicked (null, null);
				args.RetVal = true;
			}
		}
		
		void OnHeadLabelClick (object s, Gtk.ButtonPressEventArgs args)
		{
			IDesignArea d = GetDesignArea ();
			if (d.IsSelected (headerLabel)) {
				if (headerLabel.Child is Gtk.Label) {
					headerLabel.Remove (headerLabel.Child);
				}
			} else {
				d.SetSelection (headerLabel, null);
			}
		}
		
		void OnGroupNameChanged (object s, EventArgs args)
		{
			if (actionGroup != null)
				actionGroup.Name = headerLabel.Text;
		}
		
		void OnSelectionChanged (object s, EventArgs args)
		{
			Console.WriteLine ("OnSelectionChanged p1");
			if (SelectionChanged != null)
				SelectionChanged (this, args);
		}
		
		IDesignArea GetDesignArea ()
		{
			if (darea != null)
				return darea;
			
			darea = WidgetUtils.GetDesignArea (this);
			darea.SelectionChanged += OnSelectionChanged;
			return darea;
		}
		
		ActionMenu IMenuItemContainer.OpenSubmenu { 
			get { return null; } 
			set { }
		}
		
		bool IMenuItemContainer.IsTopMenu {
			get { return false; }
		}
		
		Gtk.Widget IMenuItemContainer.Widget {
			get { return this; }
		}
		
		class ActionComparer: IComparer
		{
			public int Compare (object x, object y)
			{
				return string.Compare (((Action)x).GtkAction.Label, ((Action)y).GtkAction.Label);
			}
		}
	}
	
	public class EditableLabel: Gtk.EventBox
	{
		string text;
		string markup;
		
		public EditableLabel (): this ("")
		{
		}
		
		public EditableLabel (string txt)
		{
			VisibleWindow = false;
			text = txt;
			Add (CreateLabel ());
		}
		
		public string Text {
			get { return text; }
			set {
				text = value;
				if (Child is Gtk.Entry)
					((Gtk.Entry)Child).Text = text;
				else
					((Gtk.Label)Child).Markup = Markup;
			}
		}
		
		public string MarkupTemplate {
			get { return markup; }
			set {
				markup = value;
				if (Child is Gtk.Label)
					((Gtk.Label)Child).Markup = Markup;
			}
		}
		
		public string Markup {
			get { return markup != null ? markup.Replace ("$TEXT",text) : text; }
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			IDesignArea d = WidgetUtils.GetDesignArea (this);
			if (d.IsSelected (this)) {
				if (Child is Gtk.Label) {
					StartEditing ();
					return true;
				}
			} else {
				d.SetSelection (this, null);
				return true;
			}
			return false;
		}
		
		void SelectionDisposed (object s, EventArgs args)
		{
			EndEditing ();
		}
		
		public void StartEditing ()
		{
			if (Child is Gtk.Label) {
				IDesignArea d = WidgetUtils.GetDesignArea (this);
				IObjectSelection sel = d.GetSelection (this);
				if (sel == null)
					sel = d.SetSelection (this, null);
				
				sel.Disposed += SelectionDisposed;
					
				Remove (Child);
				Add (CreateEntry ());
				ShowAll ();
				Child.GrabFocus ();
			}
		}
		
		public void EndEditing ()
		{
			if (Child is Gtk.Entry) {
				Remove (Child);
				Add (CreateLabel ());
				ShowAll ();
			}
		}
		
		Gtk.Label CreateLabel ()
		{
			Gtk.Label label = new Gtk.Label ();
			label.Markup = Markup;
			label.Xalign = 0;
			return label;
		}
		
		Gtk.Entry CreateEntry ()
		{
			Gtk.Entry e = new Gtk.Entry (text);
			e.Changed += delegate (object s, EventArgs a) {
				text = e.Text;
				if (Changed != null)
					Changed (this, a);
			};
			e.Activated += delegate (object s, EventArgs a) {
				EndEditing ();
			};
			return e;
		}
		
		public event EventHandler Changed;
	}
}
