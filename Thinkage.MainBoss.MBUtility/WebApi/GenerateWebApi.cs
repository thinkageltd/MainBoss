using System;
using System.Collections.Generic;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Controls;
using Thinkage.MainBoss.Database;
using System.Linq;
namespace Thinkage.MainBoss.MBUtility
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
	// One use class terminates after task is done.
	internal class GenerateWebApi
	{
		public class Definition : Thinkage.Libraries.CommandLineParsing.Optable, UtilityVerbDefinition
		{
			public Definition()
				: base()
			{
				Add(WebApiProjectDirectory = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("ProjectDirectory"), KB.I("Directory where the WebApi project root is"), true));
				Add(ErrorOutputFile = new Thinkage.Libraries.CommandLineParsing.StringValueOption(KB.I("ErrorOutput"), KB.I("File containing the errors encountered during generate."), false));

				MarkAsDefaults();
			}
			public readonly StringValueOption WebApiProjectDirectory;
			public readonly StringValueOption ErrorOutputFile;
			public string Verb
			{
				[return: Thinkage.Libraries.Translation.Invariant]
				get
				{
					return "GenerateWebApi";
				}
			}
			public void RunVerb()
			{
				new GenerateWebApi(this).Run();
			}
			public Thinkage.Libraries.CommandLineParsing.Optable Optable
			{
				get
				{
					return this;
				}
			}
		}
		private GenerateWebApi(Definition options)
		{
			Options = options;
		}
		private readonly Definition Options;
		private void Run()
		{
			new ApplicationTblDefaultsNoEditing(Thinkage.Libraries.Application.Instance, new MainBossPermissionsManager(Root.Rights), Root.Rights.Table, Root.RightsSchema, Root.Rights.Action.Customize);
			var registry = new MainBossTblRegistry();
#if !NOTALL
			foreach (DBI_Table t in Root.RightsSchema.Tables) {
				GenerateApi(registry.GetEditTbl(t));
			}
#else
			GenerateApi(registry.GetEditTbl(dsMB.Schema.T.UnitUsage));
#endif
		}
		#region Generation
		private string MakeModelPath(string name)
		{
			return System.IO.Path.Combine(Options.WebApiProjectDirectory.Value, KB.I("Models"), KB.I("Tables"), Strings.IFormat("{0}Model.cs", name));
		}
		private string MakeRepositoryPath(string name)
		{
			return System.IO.Path.Combine(Options.WebApiProjectDirectory.Value, KB.I("Models"), KB.I("Tables"), Strings.IFormat("{0}Repository.cs", name));
		}
		private string MakeControllerPath(string name)
		{
			return System.IO.Path.Combine(Options.WebApiProjectDirectory.Value, KB.I("Controllers"), KB.I("Tables"), Strings.IFormat("{0}Controller.cs", name));
		}

		private void GenerateApi(Tbl editTbl)
		{
			if (editTbl == null)
				return;
			// Build the Model first
			GenerateWebApiModel(editTbl);
			GenerateWebApiRepository(editTbl);
			GenerateWebApiController(editTbl);
		}
		#region ModelGeneration
		static string ModelPreamble = KB.I(@"using System;
using System.Runtime.Serialization;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.WebApi.Models
{{
#pragma warning disable 1591
{1}	[Serializable]
	[DataContract]
	public partial class {0} : IRepositoryWrapper<{0}, dsMB.{0}Row>
	{{
");
		static string ModelPostamble = KB.I(@"	}
#pragma warning restore 1591
}");
		private System.IO.StreamWriter Writer;
		int TabLevel = 1;
		ModelMember ModelMemberCollection;
		#region ModelMember
		[System.Diagnostics.DebuggerDisplay("{Name}")]
		class ModelMember
		{
			static Dictionary<int, ModelMember> RecordSetDictionary;
			public readonly string Type;
			public readonly string Name;
			public readonly string Doc;
			public readonly bool IsReadonly;
			public readonly List<ModelMember> Members;
			public bool IsContainer
			{
				get
				{
					return Type == null;
				}
			}
			private ModelMember(string name, string type, string doc, bool isReadonly)
			{
				Name = name;
				Type = type;
				Doc = doc;
				Members = new List<ModelMember>();
				IsReadonly = isReadonly;
			}
			private ModelMember(DBI_Value c, ModelMember parent)
				: this(c.Name, c.EffectiveType.GenericMinimalNativeType().FullName, c.Doc, !c.IsWriteable)
			{
			}
			public ModelMember(DBI_Table t)
				: this(t.Name, null, t.Doc, false)
			{
				RecordSetDictionary = new Dictionary<int, ModelMember> {
					{ 0, this }
				};
				Add(new ModelMember(t.InternalId.ReferencedColumn, this));
			}

			private string IdentifyingNameAsIdentifier(TblValueNode n)
			{
				var parts = new List<string>();
				StringBuilder identifier = new StringBuilder();
				if (n is TblColumnNode) {
					DBI_Path x = ((TblColumnNode)n).Path;
					for (int i = x.AllColumns.Count - 1; i >= 0; i -= 2) {
						// We skip columns that are base/derived linkages.
						if (x.AllColumns[i].LinkageType == DBI_Relation.LinkageTypes.Base
							|| x.AllColumns[i].LinkageType == DBI_Relation.LinkageTypes.Derived
							|| x.AllColumns[i] == x.AllColumns[i].Table.Id)  // This includes the case of what a derived linkage looks like within a path.
							continue;
						if (parts.Count > 0 && i == 0 && !string.IsNullOrEmpty(x.AllColumns[i].Table.SqlQueryText))
							// Do not name linking columns that occur in a view if it is the first column in the path.
							// This removes all the linking columns in the report driver views. Note that reports sometimes contain secondary views for instance to find the Task in a WorkOrderFormReport.
							continue;
						Key k = x.AllColumns[i].LabelKey;
						if (k == null)
							continue;
						parts.Add(k.IdentifyingName);
					}
				}
				else
					parts.Add(n.Label.IdentifyingName);

				for( int i = parts.Count; --i >= 0; ) {
					if (identifier.Length > 0)
						identifier.Append("_");
					identifier.Append(parts[i].Replace(" ", "").Replace("/",""));
				}
				return identifier.ToString();
			}
			public ModelMember New(TblValueNode n)
			{
				StringBuilder comment = new StringBuilder();
				int recordSet = 0;
				DBI_Value v = n.ReferencedValue;
				if (v.Doc != null)
					comment.AppendLine(v.Doc);
				if (n is TblColumnNode cn) {
					comment.AppendLine(cn.Path.ToString());
					recordSet = cn.RecordSet;
				}
				if (!RecordSetDictionary.TryGetValue(recordSet, out ModelMember container)) {
					container = new ModelMember(Strings.IFormat("RecordSet{0}", recordSet), null, null, false);
					RecordSetDictionary.Add(recordSet, container);
					RecordSetDictionary[0].Add(container);
				}
				string identifierName = IdentifyingNameAsIdentifier(n);
				if (container.Name == identifierName)
					identifierName += "1";
				var m = new ModelMember(identifierName, v.EffectiveType.GenericMinimalNativeType().FullName, comment.ToString(), !v.IsWriteable);
				return container.Add(m);
			}
			private ModelMember Add(ModelMember mToAdd)
			{
				System.Diagnostics.Debug.Assert(this.IsContainer);
				if ( !Members.Any( m => m.Name == mToAdd.Name ))
					Members.Add(mToAdd);
				return mToAdd;
			}
		}
		#endregion
		private void GenerateWebApiModel(Tbl editTbl)
		{
			Writer = new System.IO.StreamWriter(MakeModelPath(editTbl.Schema.Name), false);
			System.Diagnostics.Debug.Assert(editTbl.Schema == editTbl.Schema);
			ModelMemberCollection = new ModelMember(editTbl.Schema);
			GenerateMembers(editTbl.Columns);
			Writer.Write(Strings.IFormat(ModelPreamble, ModelMemberCollection.Name, FormatDocSummary(ModelMemberCollection.Doc)));
			EmitCollection(editTbl.Schema);
			Writer.Write(ModelPostamble);
			Writer.Close();
			Writer = null;
			ModelMemberCollection = null;
		}
		private void GenerateMembers(TblLayoutNodeArray columns)
		{
			foreach( TblLayoutNode c in columns ) {
				if (TblLayoutNode.GetInNonDefault(c) == false)
					continue;
				ECol ecol = TblLayoutNode.GetECol(c);
				if (ecol != null) {
					if (c is TblValueNode vn) {
						Fmt fmt = new Fmt(vn.ReferencedType, vn.ReferencedValue, ecol, c);
						if (Fmt.GetShowReferences(fmt) == null) // is it a browsette reference ?
							ModelMemberCollection.New(vn);
					}
				}
			}
		}
		private void EmitCollection(DBI_Table root)
		{
			++TabLevel;
			WriteMembers(ModelMemberCollection);
			--TabLevel;
		}
		private string Tabs()
		{
			return new String('\t', TabLevel);
		}
		private void WriteLineWithTabs([Invariant]string x)
		{
			Writer.Write(Tabs());
			Writer.WriteLine(x);
		}
		private void WriteMembers(ModelMember n)
		{
			System.Diagnostics.Debug.Assert(n.IsContainer);
			foreach (ModelMember m in n.Members)
				WriteMember(m);
		}

		private void WriteMember(ModelMember n)
		{
			if (n.IsContainer)
				WriteContainerMember(n);
			else
				WriteDataMember(n);
		}
		private void WriteContainerMember(ModelMember n)
		{
			WriteLineWithTabs(Strings.IFormat("public struct _{0} {{", n.Name));
			++TabLevel;
			WriteMembers(n);
			--TabLevel;
			WriteLineWithTabs("}");
			WriteLineWithTabs("[DataMember]");
			WriteLineWithTabs(Strings.IFormat("public _{0} {0};", n.Name));
		}
		private void WriteDataMember(ModelMember n)
		{
			if( n.Doc != null )
				Writer.Write(FormatDocSummary(n.Doc));
			WriteLineWithTabs("[DataMember]");
			if (n.IsReadonly)
				WriteLineWithTabs(Strings.IFormat("public {0} {1};", n.Type, n.Name)); // For now, the XML serializer won't find the member if it has 'readonly' in front of it
			else
				WriteLineWithTabs(Strings.IFormat("public {0} {1};", n.Type, n.Name));
		}
		#endregion
		private string FormatDocSummary([Invariant]string docString)
		{
			if (String.IsNullOrEmpty(docString))
				return "";
			System.Text.StringBuilder doc = new System.Text.StringBuilder();
			doc.Append(Tabs());
			doc.AppendLine("/// <summary>");
			string[] lines = docString.Split(new char[] { '\r', '\n' });
			foreach (string l in lines) {
				if (String.IsNullOrEmpty(l) || String.IsNullOrWhiteSpace(l.Trim()))
					continue;
				doc.Append(Tabs());
				doc.Append("/// ");
				doc.AppendLine(l.Trim());
			}
			doc.Append(Tabs());
			doc.AppendLine("/// </summary>");
			return doc.ToString();
		}
		#region ControllerGeneration
		private static string ControllerTemplate = @"namespace Thinkage.MainBoss.WebApi.Controllers.Tables
{{
	/// <summary>
	/// This is the General documentation for the {0} controller
	/// </summary>
	public class {0}Controller : GenericApiController<Models.{0}>
	{{
{1}
		public {0}Controller(Models.{0}Repository r)
			: base(r)
		{{
		}
    }
}";
		private void GenerateWebApiController(Tbl editTbl)
		{
			string identification = editTbl.Schema.Name;

			Writer = new System.IO.StreamWriter(MakeControllerPath(identification), false);
			++TabLevel;
			Writer.Write(Strings.IFormat(ControllerTemplate, identification, FormatDocSummary(editTbl.Schema.Doc)));
			--TabLevel;
			Writer.Close();
			Writer = null;
		}
		// NOTE: the following applies to the Controller class
		// To get specific documentation for XmlDocumentation requires all methods in the base class to be virtual (that does nothing except call the base implementation) so there is a method on which to hang an XmlDocument that
		// will get generated specifically for the derived Get method for the API documentation
		// This is the only way to auto generate the documentation with Build XmlDocumentation output option on.
		#endregion
		#region RepositoryGeneration
		string RepositoryTemplate = KB.I(@"using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.MainBoss.Database;
namespace Thinkage.MainBoss.WebApi.Models
{{
	public partial class {0} : IRepositoryWrapper<{0}, dsMB.{0}Row>
	{{
		#region IRepositoryWrapper<{0},{0}Row> Members
		public {0} Create(dsMB.{0}Row row)
		{{
			{0} r = new {0}();
			r.UpdateFrom(row);
			return r;
		}
		public void UpdateFrom(dsMB.{0}Row row)
		{{
			throw new System.NotImplementedException();
		}
		public void UpdateTo(dsMB.{0}Row row)
		{{
			throw new System.NotImplementedException();
		}
		#endregion
	}
	public class {0}Repository : GenericRepository<{0}, dsMB.{0}Row>
	{{
		public {0}Repository() : base()
		{{
			DataSet.EnsureDataTableExists(dsMB.Schema.T.{0});
		}
		public override DBIDataTable Table
		{{
			get
			{{
				return DataSet.T.{0};
			}
		}
		private IRepositoryWrapper<{0}, dsMB.{0}Row> pRowWrapper = new {0}();
		public override IRepositoryWrapper<{0}, dsMB.{0}Row> RowWrapper
		{{
			get
			{{
				return pRowWrapper;
			}
		}
	}
}
");
		private void GenerateWebApiRepository(Tbl editTbl)
		{
			string identification = editTbl.Schema.Name;

			Writer = new System.IO.StreamWriter(MakeRepositoryPath(identification), false);
			++TabLevel;
			Writer.Write(Strings.IFormat(RepositoryTemplate, identification));
			--TabLevel;
			Writer.Close();
			Writer = null;
		}
		#endregion
		#endregion
		#region MainBossTblRegistry
		public class MainBossTblRegistry : TIGeneralMB3
		{
			public MainBossTblRegistry()
			{
			}

			public Tbl GetBrowseTbl(DBI_Table schema)
			{
				return FindBrowseTbl(schema);
			}
			public Tbl GetEditTbl(DBI_Table schema)
			{
				return FindEditTbl(schema);
			}
		}
		#endregion

	}
	#region Extensions
	public static class Extensions
	{
		private static void AddParentRelativeNodeName(this TblLayoutNode n, System.Text.StringBuilder sb)
		{
			// TODO: For layout nodes not assigned to a Tbl (e.g. unbound parameter controls in a browser) there is no container, and IndexInParent is zero in the node and any children
			// TODO: For the case of unbound parameter controls in a browser, call AssignToTbl(null) ???? so the IndexInParent gets set on all children. Technically the "container" should be all the controls
			// created for a particular command, and the name of the container should be the hierarchy of command node names to get to that command. Perhaps this could be done by creating group layout nodes appropriately named...
			TblLayoutNodeArray container = n.Parent != null ? n.Parent.Columns : n.OwnerTbl?.Columns;
			if (n.Label != null) {
				// Use the /name syntax, or if we are not the first with that name use /[n]name
				string name = n.Label.IdentifyingName;
				int duplicates = 0;
				if (container != null) {
					for (int i = n.IndexInParent; --i >= 0; )
						if (container.ColumnArray[i].Label is Key && ((Key)container.ColumnArray[i].Label).IdentifyingName == name)
							++duplicates;
					sb.Append("_");
					if (duplicates > 1)
						sb.AppendFormat(KB.I("[{0}]"), duplicates);
				}
				// TODO: Escape/quote the name
				sb.Append(name);
			}
			else if (container == null) {
			}
			else {
				// Look for the nearest named sibling, then ask for its name and step from there or just step from the start or end.
				int endDistance = container.Count - n.IndexInParent;
				for (int distance = 1; ; ++distance) {
					if (distance > n.IndexInParent) {
						// The start of the parent is closer than any named element, just use count from start
						sb.AppendFormat(KB.I("/[{0}]"), n.IndexInParent + 1);
						return;
					}
					if (distance >= endDistance) {
						// The end of the parent is closer than any named element, just use count from end
						sb.AppendFormat(KB.I("\\-[{0}]"), endDistance);
						return;
					}
					if (container.ColumnArray[n.IndexInParent - distance].Label is SimpleKey) {
						// A preceding sibling is the closest named element, count forward from there
						container.ColumnArray[n.IndexInParent - distance].AddParentRelativeNodeName(sb);
						sb.AppendFormat(KB.I("[{0}]"), distance);
						return;
					}
					if (container.ColumnArray[n.IndexInParent + distance].Label is SimpleKey) {
						// A following sibling is the closest named element, count back from there
						container.ColumnArray[n.IndexInParent + distance].AddParentRelativeNodeName(sb);
						sb.AppendFormat(KB.I("-[{0}]"), distance);
						return;
					}
				}
			}
		}
		public static void BuildNodeName(this TblLayoutNode n, System.Text.StringBuilder sb)
		{
			if (n.Parent != null)
				n.Parent.AddTblRelativeNodeName(sb);
			AddParentRelativeNodeName(n, sb);
		}
	}
	#endregion
}
