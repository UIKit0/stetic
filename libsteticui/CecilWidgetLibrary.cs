using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Xml;
using Mono.Cecil;

namespace Stetic
{
	internal class CecilWidgetLibrary: WidgetLibrary
	{
		AssemblyDefinition assembly;
		DateTime timestamp;
		string name;
		string fileName;
		XmlDocument objects;
		XmlDocument steticGui;
		IAssemblyResolver resolver;
		string[] dependencies;
		ImportContext importContext;
		bool canGenerateCode;
		
		public CecilWidgetLibrary (ImportContext importContext, string assemblyPath)
		{
			this.importContext = importContext;
			if (File.Exists (assemblyPath))
				timestamp = System.IO.File.GetLastWriteTime (assemblyPath);
			else
				timestamp = DateTime.MinValue;

			this.name = assemblyPath;
			fileName = assemblyPath;

			ScanDependencies ();
		}
		
		public override string Name {
			get { return name; }
		}
		
		public override bool NeedsReload {
			get {
				if (!System.IO.File.Exists (fileName))
					return false;
				return System.IO.File.GetLastWriteTime (fileName) != timestamp;
			}
		}
		
		public override bool CanReload {
			get { return true; }
		}
		
		public override bool CanGenerateCode {
			get { return canGenerateCode; }
		}
		
		public DateTime TimeStamp {
			get { return timestamp; }
		}
		
		public override void Load ()
		{
			// Assume that it can generate code
			canGenerateCode = true;
			
			if (assembly == null) {
				if (!File.Exists (fileName)) {
					base.Load (new XmlDocument ());
					return;
				}
				
				timestamp = System.IO.File.GetLastWriteTime (fileName);
				
				assembly = AssemblyFactory.GetAssembly (fileName);
			}
			
			foreach (Resource res in assembly.MainModule.Resources) {
				EmbeddedResource eres = res as EmbeddedResource;
				if (eres == null) continue;
				
				if (eres.Name == "objects.xml") {
					MemoryStream ms = new MemoryStream (eres.Data);
					objects = new XmlDocument ();
					objects.Load (ms);
				}
				
				if (eres.Name == "gui.stetic") {
					MemoryStream ms = new MemoryStream (eres.Data);
					steticGui = new XmlDocument ();
					steticGui.Load (ms);
				}
			}

			Load (objects);
			
			if (canGenerateCode) {
				// If it depends on libraries which can't generate code,
				// this one can't
				foreach (string dlib in GetLibraryDependencies ()) {
					WidgetLibrary lib = Registry.GetWidgetLibrary (dlib);
					if (lib != null && !lib.CanGenerateCode) {
						canGenerateCode = false;
						break;
					}
				}
			}
			
			// This information is not needed after loading
			assembly = null;
			objects = null;
			steticGui = null;
			resolver = null;
		}

		protected override ClassDescriptor LoadClassDescriptor (XmlElement element)
		{
			string name = element.GetAttribute ("type");
			
			TypeDefinition cls = assembly.MainModule.Types [name];
			if (cls == null)
				return null;
			
			
			// Find the nearest type that can be loaded
			Stetic.ClassDescriptor typeClassDescriptor = FindType (assembly, cls);

			if (typeClassDescriptor == null) {
				Console.WriteLine ("Descriptor not found: " + cls.Name);
				return null;
			}
			
			XmlElement steticDefinition = null;
			
			if (steticGui != null) {
				string wrappedTypeName = element.GetAttribute ("type");
				steticDefinition = (XmlElement) steticGui.DocumentElement.SelectSingleNode ("widget[@id='" + wrappedTypeName + "']");
			}
			
			CecilClassDescriptor cd = new CecilClassDescriptor (this, element, typeClassDescriptor, steticDefinition, cls);
			
			if (canGenerateCode && !cd.CanGenerateCode)
				canGenerateCode = false;
			return cd;
		}
		
		Stetic.ClassDescriptor FindType (AssemblyDefinition asm, TypeDefinition cls)
		{
			Stetic.ClassDescriptor klass = Stetic.Registry.LookupClassByName (cls.BaseType.FullName);
			if (klass != null) return klass;
			
			TypeDefinition bcls = FindTypeDefinition (cls.BaseType.FullName);
			if (bcls == null)
				return null;

			return FindType (asm, bcls);
		}
		
		AssemblyDefinition ResolveAssembly (AssemblyNameReference aref, out string filePath)
		{
			if (resolver == null)
				resolver = new DefaultAssemblyResolver ();
			
			filePath = null;
			string bpath = Path.Combine (Path.GetDirectoryName (fileName), aref.Name);
			if (File.Exists (bpath + ".dll"))
				filePath = bpath + ".dll";
			if (File.Exists (bpath + ".exe"))
				filePath = bpath + ".exe";
				
			if (filePath != null) {
				return AssemblyFactory.GetAssembly (filePath);
			}

			try {
				return resolver.Resolve (aref);
			} catch {
				// If can't resolve, just return null
				return null;
			}
		}
		
		internal TypeDefinition FindTypeDefinition (string fullName)
		{
			return FindTypeDefinition (new Hashtable (), assembly, fullName);
		}
		
		TypeDefinition FindTypeDefinition (Hashtable visited, AssemblyDefinition asm, string fullName)
		{
			if (visited.Contains (asm))
				return null;
				
			visited [asm] = asm;
			
			TypeDefinition cls = asm.MainModule.Types [fullName];
			if (cls != null)
				return cls;
			
			foreach (AssemblyNameReference aref in asm.MainModule.AssemblyReferences) {
				string file;
				AssemblyDefinition basm = ResolveAssembly (aref, out file);
				if (basm != null) {
					cls = FindTypeDefinition (visited, basm, fullName);
					if (cls != null)
						return cls;
				}
			}
			return null;
		}
		
		public override string[] GetLibraryDependencies ()
		{
			if (NeedsReload || dependencies == null)
				ScanDependencies ();
			return dependencies;
		}
		
		void ScanDependencies ()
		{
			if (assembly == null) {
				if (!File.Exists (fileName)) {
					dependencies = new string [0];
					return;
				}
				assembly = AssemblyFactory.GetAssembly (fileName);
			}
			ArrayList list = new ArrayList ();
			ScanDependencies (list, assembly);
			dependencies = (string[]) list.ToArray (typeof(string));
		}
		
		void ScanDependencies (ArrayList list, AssemblyDefinition asm)
		{
			string basePath = Path.GetDirectoryName (fileName);
			foreach (AssemblyNameReference aref in asm.MainModule.AssemblyReferences) {
				string file = FindAssembly (importContext, aref.FullName, basePath);
				if (file != null && Application.InternalIsWidgetLibrary (importContext, file))
					list.Add (file);
			}
		}
		
		public static bool IsWidgetLibrary (string path)
		{
			try {
				AssemblyDefinition adef = AssemblyFactory.GetAssembly (path);
				
				foreach (Resource res in adef.MainModule.Resources) {
					EmbeddedResource eres = res as EmbeddedResource;
					if (eres == null) continue;
					if (eres.Name == "objects.xml")
						return true;
				}
			} catch {
			}
			return false;
		}
		
		public static string FindAssembly (ImportContext importContext, string assemblyName, string basePath)
		{
			StringCollection col = new StringCollection ();
			col.Add (basePath);
			if (importContext != null) {
				foreach (string s in importContext.Directories)
					col.Add (s);
			}
			
			AssemblyResolver res = new AssemblyResolver ();
			try {
				return res.Resolve (assemblyName, col);
			} catch {
			}
			return null;
		}
	}
}