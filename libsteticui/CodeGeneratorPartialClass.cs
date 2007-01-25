
using System;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace Stetic
{
	internal static class CodeGeneratorPartialClass
	{
		public static void GenerateProjectGuiCode (SteticCompilationUnit globalUnit, CodeNamespace globalNs, CodeTypeDeclaration globalType, GenerationOptions options, List<SteticCompilationUnit> units, ProjectBackend[] projects, ArrayList warnings)
		{
			// Generate code for each project
			foreach (ProjectBackend gp in projects) {
			
				// Generate top levels
				foreach (Gtk.Widget w in gp.Toplevels)
					GenerateWidgetCode (globalUnit, globalNs, options, units, w, warnings);
					
				// Generate global action groups
				foreach (Wrapper.ActionGroup agroup in gp.ActionGroups)
					GenerateGlobalActionGroupCode (globalUnit, globalNs, options, units, agroup, warnings);
			}
		}
		
		static CodeTypeDeclaration CreatePartialClass (SteticCompilationUnit globalUnit, List<SteticCompilationUnit> units, GenerationOptions options, string name)
		{
			SteticCompilationUnit unit;
			
			if (options.GenerateSingleFile)
				unit = globalUnit;
			else {
				unit = new SteticCompilationUnit (name);
				units.Add (unit);
			}
			
			string ns = "";
			int i = name.LastIndexOf ('.');
			if (i != -1) {
				ns = name.Substring (0, i);
				name = name.Substring (i+1);
			}
			
			CodeTypeDeclaration type = new CodeTypeDeclaration (name);
			type.IsPartial = true;
			type.Attributes = MemberAttributes.Public;
			type.TypeAttributes = TypeAttributes.Public;
			
			CodeNamespace cns = new CodeNamespace (ns);
			cns.Types.Add (type);
			unit.Namespaces.Add (cns);
			return type;
		}
		
		
		static void GenerateWidgetCode (SteticCompilationUnit globalUnit, CodeNamespace globalNs, GenerationOptions options, List<SteticCompilationUnit> units, Gtk.Widget w, ArrayList warnings)
		{
			// Generate the build method
			
			CodeTypeDeclaration type = CreatePartialClass (globalUnit, units, options, w.Name);
			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = "Build";
			type.Members.Add (met);
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Attributes = MemberAttributes.Family;
			
			if (options.GenerateEmptyBuildMethod) {
				GenerateWrapperFields (type, Wrapper.Widget.Lookup (w));
				return;
			}

			met.Statements.Add (
					new CodeMethodInvokeExpression (
						new CodeTypeReferenceExpression (globalNs.Name + ".Gui"),
						"Initialize"
					)
			);

			Stetic.Wrapper.Widget wwidget = Stetic.Wrapper.Widget.Lookup (w);
			Stetic.WidgetMap map = Stetic.CodeGenerator.GenerateCreationCode (globalNs, type, w, new CodeThisReferenceExpression (), met.Statements, options, warnings);
			CodeGenerator.BindSignalHandlers (new CodeThisReferenceExpression (), wwidget, map, met.Statements, options);
		}
		
		static void GenerateWrapperFields (CodeTypeDeclaration type, ObjectWrapper wrapper)
		{
			foreach (ObjectBindInfo binfo in CodeGenerator.GetFieldsToBind (wrapper)) {
				type.Members.Add (
					new CodeMemberField (
						binfo.TypeName,
						binfo.Name
					)
				);
			}
		}

		
		static void GenerateGlobalActionGroupCode (SteticCompilationUnit globalUnit, CodeNamespace globalNs, GenerationOptions options, List<SteticCompilationUnit> units, Wrapper.ActionGroup agroup, ArrayList warnings)
		{
			CodeTypeDeclaration type = CreatePartialClass (globalUnit, units, options, agroup.Name);
			
			// Generate the build method
			
			CodeMemberMethod met = new CodeMemberMethod ();
			met.Name = "Build";
			type.Members.Add (met);
			met.ReturnType = new CodeTypeReference (typeof(void));
			met.Attributes = MemberAttributes.Public;
			
			met.Statements.Add (
					new CodeMethodInvokeExpression (
						new CodeTypeReferenceExpression (globalNs.Name + ".Gui"),
						"Initialize"
					)
			);

			Stetic.WidgetMap map = Stetic.CodeGenerator.GenerateCreationCode (globalNs, type, agroup, new CodeThisReferenceExpression (), met.Statements, options, warnings);
			
			foreach (Wrapper.Action ac in agroup.Actions)
				CodeGenerator.BindSignalHandlers (new CodeThisReferenceExpression (), ac, map, met.Statements, options);
		}
	}
}
