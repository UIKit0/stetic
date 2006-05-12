using System;
using System.Collections;
using System.CodeDom;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

namespace Stetic {

	public enum FileFormat {
		Native,
		Glade
	}

	public abstract class ObjectWrapper : IDisposable {

		static Hashtable wrappers = new Hashtable ();

		protected IProject proj;
		protected object wrapped;
		protected ClassDescriptor classDescriptor;
		SignalCollection signals;

		public SignalCollection Signals {
			get {
				if (signals == null)
					signals = new SignalCollection (this);
				return signals;
			}
		}
		
		public virtual void Wrap (object obj, bool initialized)
		{
			this.wrapped = obj;
			wrappers [GetIndentityObject (obj)] = this;
		}

		public virtual void Dispose ()
		{
			wrappers.Remove (GetIndentityObject (wrapped));
		}

		public static ObjectWrapper Create (IProject proj, object wrapped)
		{
			ClassDescriptor klass = Registry.LookupClassByName (wrapped.GetType ().FullName);
			ObjectWrapper wrapper = klass.CreateWrapper ();
			wrapper.proj = proj;
			wrapper.classDescriptor = klass;
			wrapper.Wrap (wrapped, true);
			return wrapper;
		}

		internal static void Bind (IProject proj, ClassDescriptor klass, ObjectWrapper wrapper, object wrapped, bool initialized)
		{
			wrapper.proj = proj;
			wrapper.classDescriptor = klass;
			wrapper.Wrap (wrapped, initialized);
		}
		
		public virtual void Read (XmlElement elem, FileFormat format)
		{
			throw new System.NotSupportedException ();
		}
		
		public virtual XmlElement Write (XmlDocument doc, FileFormat format)
		{
			throw new System.NotSupportedException ();
		}

		public static ObjectWrapper Read (IProject proj, XmlElement elem, FileFormat format)
		{
			string className = elem.GetAttribute ("class");
			ClassDescriptor klass;
			if (format == FileFormat.Native)
				klass = Registry.LookupClassByName (className);
			else
				klass = Registry.LookupClassByCName (className);
			
			if (klass == null) {
				ErrorWidget we = new ErrorWidget (className);
				ErrorWidgetWrapper wrap = (ErrorWidgetWrapper) Create (proj, we);
				wrap.Read (elem, format);
				return wrap;
			}

			ObjectWrapper wrapper = klass.CreateWrapper ();
			wrapper.proj = proj;
			try {
				wrapper.Read (elem, format);
			} catch (Exception ex) {
				ErrorWidget we = new ErrorWidget (ex);
				ErrorWidgetWrapper wrap = (ErrorWidgetWrapper) Create (proj, we);
				wrap.Read (elem, format);
				Console.WriteLine (ex);
				return wrap;
			}
			return wrapper;
		}
		
		internal protected virtual void GenerateBuildCode (GeneratorContext ctx, string varName)
		{
			CodeVariableReferenceExpression var = new CodeVariableReferenceExpression (varName);
			
			// Write the widget properties
			foreach (ItemGroup group in ClassDescriptor.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					PropertyDescriptor prop = item as PropertyDescriptor;
					if (prop == null || !prop.IsRuntimeProperty)
						continue;
					GeneratePropertySet (ctx, var, prop);
				}
			}
		}
		
		internal protected virtual void GeneratePostBuildCode (GeneratorContext ctx, string varName)
		{
		}
		
		internal protected virtual CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			if (ClassDescriptor.InitializationProperties != null) {
				CodeExpression[] paramters = new CodeExpression [ClassDescriptor.InitializationProperties.Length];
				for (int n=0; n < paramters.Length; n++) {
					PropertyDescriptor prop = ClassDescriptor.InitializationProperties [n];
					paramters [n] = ctx.GenerateValue (prop.GetValue (Wrapped), prop.RuntimePropertyType);
				}
				return new CodeObjectCreateExpression (ClassDescriptor.WrappedTypeName, paramters);
			} else
				return new CodeObjectCreateExpression (ClassDescriptor.WrappedTypeName);
		}
		
		protected virtual void GeneratePropertySet (GeneratorContext ctx, CodeVariableReferenceExpression var, PropertyDescriptor prop)
		{
			if (ClassDescriptor.InitializationProperties != null && Array.IndexOf (ClassDescriptor.InitializationProperties, prop) != -1)
				return;
			
			object oval = prop.GetValue (Wrapped);
			if (oval == null || (prop.HasDefault && prop.IsDefaultValue (oval)))
				return;

			CodeExpression val = ctx.GenerateValue (oval, prop.RuntimePropertyType);
			CodeExpression cprop;
			
			TypedPropertyDescriptor tprop = prop as TypedPropertyDescriptor;
			if (tprop == null || tprop.GladeProperty == prop) {
				cprop = new CodePropertyReferenceExpression (var, prop.Name);
			} else {
				cprop = new CodePropertyReferenceExpression (var, tprop.GladeProperty.Name);
				cprop = new CodePropertyReferenceExpression (cprop, prop.Name);
			}
			ctx.Statements.Add (new CodeAssignStatement (cprop, val));
		}
		
		public static ObjectWrapper Lookup (object obj)
		{
			if (obj == null)
				return null;
			else
				return wrappers [GetIndentityObject (obj)] as Stetic.ObjectWrapper;
		}

		public object Wrapped {
			get {
				return wrapped;
			}
		}

		public IProject Project {
			get {
				return proj;
			}
		}

		public ClassDescriptor ClassDescriptor {
			get { return classDescriptor; }
		}
		
		public void NotifyChanged ()
		{
			OnObjectChanged (new ObjectWrapperEventArgs (this));
		}
		
		static object GetIndentityObject (object ob)
		{
			if (ob is Gtk.Container.ContainerChild) {
				// We handle ContainerChild in a special way here since
				// the Gtk.Container indexer always returns a new ContainerChild
				// instance. We register its wrapper using ContainerChildHashItem
				// to make sure that two different instance of the same ContainerChild
				// can be found equal.
				ContainerChildHashItem p = new ContainerChildHashItem ();
				p.ContainerChild = (Gtk.Container.ContainerChild) ob;
				return p;
			}
			else
				return ob;
		}
		
		public delegate void WrapperNotificationDelegate (object obj, string propertyName);
		
		public event WrapperNotificationDelegate Notify;
		public event SignalEventHandler SignalAdded;
		public event SignalEventHandler SignalRemoved;
		public event SignalChangedEventHandler SignalChanged;
		
		// Fired when any information of the object changes.
		public event ObjectWrapperEventHandler ObjectChanged;

		internal protected virtual void OnObjectChanged (ObjectWrapperEventArgs args)
		{
			if (ObjectChanged != null)
				ObjectChanged (this, args);
		}
		
		protected virtual void EmitNotify (string propertyName)
		{
			if (Notify != null)
				Notify (this, propertyName);
		}

		internal protected virtual void OnSignalAdded (SignalEventArgs args)
		{
			OnObjectChanged (args);
			if (SignalAdded != null)
				SignalAdded (this, args);
		}
		
		internal protected virtual void OnSignalRemoved (SignalEventArgs args)
		{
			OnObjectChanged (args);
			if (SignalRemoved != null)
				SignalRemoved (this, args);
		}
		
		internal protected virtual void OnSignalChanged (SignalChangedEventArgs args)
		{
			OnObjectChanged (args);
			if (SignalChanged != null)
				SignalChanged (this, args);
		}
		
		internal protected virtual void OnDesignerAttach (IDesignArea designer)
		{
		}
		
		internal protected virtual void OnDesignerDetach (IDesignArea designer)
		{
		}
	}
	
	// Wraps a ContainerChild, and properly implements GetHashCode() and Equals()
	struct ContainerChildHashItem
	{
		public Gtk.Container.ContainerChild ContainerChild;
		
		public override int GetHashCode ()
		{
			return ContainerChild.Parent.GetHashCode () + ContainerChild.Child.GetHashCode ();
		}
		
		public override bool Equals (object ob)
		{
			ContainerChildHashItem ot = (ContainerChildHashItem) ob;
			return ot.ContainerChild.Child == ContainerChild.Child && ot.ContainerChild.Parent == ContainerChild.Parent;
		}
	}
}
