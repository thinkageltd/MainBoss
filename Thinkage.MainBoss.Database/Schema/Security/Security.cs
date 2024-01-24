using System.Collections.Generic;
using System.Xml;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary.Security;
using Thinkage.Libraries.Xml;

namespace Thinkage.MainBoss.Database.Security
{
	#region RoleRight
	[System.Diagnostics.DebuggerDisplay("{Class}, {Name}")]
	public class RoleRight : NameAndNameSpaceAttribute
	{
		#region Properties
		/// <summary>
		/// The security role is externally visible
		/// </summary>
	#endregion

		public RoleRight(TableRightType aClass) : base(aClass) { }
		#region Xml Input support
		public override void FromXml(XmlElement node)
		{
			base.FromXml(node);
			XmlAttribute attr = node.Attributes["id"];
			pId = attr == null ? 0 : int.Parse(attr.Value);
			attr = node.Attributes["rank"];
			pRank = attr == null ? 0 : int.Parse(attr.Value);
			XmlElement child = (XmlElement)node.FirstChild;
			for (; child != null; child = (XmlElement)child.NextSibling)
			{
				try
				{
					if (child.Name == KB.I("include"))
					{
						Include i = new Include(TableRightType.Right);
						i.FromXml(child);
						pIncludes.Add(i);
					}
					else if (child.Name == KB.I("extend"))
					{
						Include i = new Include(TableRightType.Role);
						i.FromXml(child);
						pIncludes.Add(i);
					}
					else if (child.Name == KB.I("tableright"))
					{
						TableRight tr = new TableRight();
						tr.FromXml(child);
						TableRightCollection.Add(tr);
					}
					else if (child.Name == KB.I("costrights"))
					{
						if (child.ChildNodes.Count == 0)
							throw new Thinkage.Libraries.GeneralException(Thinkage.Libraries.Translation.KB.T("empty <costrights>"));
						string content = child.FirstChild.Value;
						string[] parts = content.Split(new char[] { ' ', ',' });
						pCostRights.AddRange(parts);
					}
					else if (child.Name == KB.I("workorder"))
					{
						createTransitions("WorkOrder", child, "Open Close Draft Reopen Void Suspend");
					}
					else if (child.Name == KB.I("purchaseorder"))
					{
						createTransitions("PurchaseOrder", child, "Issue Close Draft ReActivate Void Withdraw");
					}
					else if (child.Name == KB.I("request"))
					{
						createTransitions("Request", child, "InProgress Close Reopen Void");
					}
					else if( child.Name == KB.I("action") )
					{
						if (child.ChildNodes.Count == 0)
							throw new GeneralException(KB.T("empty <action>"));
						string content = child.FirstChild.Value;
						string[] parts = content.Split(new char [] {' ',','});
						pActionRights.AddRange(parts);
					}
					else if (child.Name == KB.I("condition"))
					{
						if (child.ChildNodes.Count == 0)
							throw new GeneralException(KB.T("empty <condition>"));
						pCondition = child.FirstChild.Value;
					}
					else if (child.Name == KB.I("description"))
					{
						if (child.ChildNodes.Count == 0)
							throw new GeneralException(KB.T("empty <condition>"));
						pDescription = child.FirstChild.Value;
					}
					else if (child.Name == KB.I("comment"))
					{
						if (child.ChildNodes.Count == 0)
							throw new GeneralException(KB.T("empty <condition>"));
						pComment = child.FirstChild.Value;
					}
					else
						System.Diagnostics.Debug.Assert(false, "Invalid child element <" + child.Name + "> of <" + node.Name + ">");
				}
				catch (System.Exception ex)
				{
					throw Thinkage.Libraries.Exception.AddContext(ex, child);
				}
			}
		}
		private void createTransitions([Thinkage.Libraries.Translation.Invariant] string toWhat, XmlNode values, [Thinkage.Libraries.Translation.Invariant] string defaultPerms)
		{
			string perms = values.ChildNodes.Count == 0 ? defaultPerms : values.FirstChild.Value;
			string[] contents = perms.Split(new char[] {',', ' '});
			for( int i = 0; i < contents.Length; ++i  )
				pTransitionRights.Add(Strings.IFormat("{0}.{1}", toWhat, contents[i]));
		}
		#endregion
		#region Xml output support
		internal XmlNode ToXml(XmlDocument root) {
			XmlElement node = root.CreateElement(XmlElementName, RightSet.XmlNamespace);
			node.SetAttribute("name", Name);
			node.SetAttribute("id", pId.ToString());
			node.SetAttribute("rank", pRank.ToString());
			if (pDescription != null || pDescription != "")
			{
				XmlElement d = root.CreateElement("description", RightSet.XmlNamespace);
				d.InnerText = pDescription;
				node.AppendChild(d);
			}
			if (pDescription != null || pComment != "")
			{
				XmlElement d = root.CreateElement("comment", RightSet.XmlNamespace);
				d.InnerText = pDescription;
				node.AppendChild(d);
			}
			if (pDescription != null || pCondition != "")
			{
				XmlElement d = root.CreateElement("condition", RightSet.XmlNamespace);
				d.InnerText = pDescription;
				node.AppendChild(d);
			}
			foreach (var c in CostRights)
			{
				XmlElement n = root.CreateElement("costright", RightSet.XmlNamespace);
				n.InnerText = c;
				node.AppendChild(n);
			}
			foreach (var t in TransitionRights)
			{
				string[] parts = t.Split('.');
				XmlElement n = root.CreateElement(parts[0], RightSet.XmlNamespace);
				n.InnerText = parts[1];
				node.AppendChild(n);
			}
			foreach (var t in ActionRights)
			{
				string[] parts = t.Split('.');
				XmlElement n = root.CreateElement(parts[0], RightSet.XmlNamespace);
				n.InnerText = parts[1];
				node.AppendChild(n);
			}
			foreach (var i in Includes)
				node.AppendChild(i.ToXml(root));
			return node;
		}
		#endregion
		#region Right Properties
		private int pId;
		/// <summary>
		/// Used to set the fixed GUID in MainBoss
		/// </summary>
		public int Id
		{
			get
			{
				return pId;
			}
		}

		private int pRank;
		/// <summary>
		/// Used to Order and Group the Error Messages From permission erorrs
		/// </summary>
		public int Rank
		{
			get { return pRank;}
		}
		public readonly NameAndNameSpaceCollection TableRightCollection = new NameAndNameSpaceCollection();
		/// <summary>
		/// List of ViewCost rights associated with this role
		/// </summary>
		public List<string> CostRights
		{
			get
			{
				return pCostRights;
			}
		}
		private List<string> pCostRights = new List<string>();
		/// <summary>
		/// List of State Transition rights associated with this role
		/// </summary>
		public List<string> TransitionRights
		{
			get
			{
				return pTransitionRights;
			}
		}
		private List<string> pTransitionRights = new List<string>();
		/// <summary>
		/// List of Action rights associated with this role
		/// </summary>
		public List<string> ActionRights
		{
			get
			{
				return pActionRights;
			}
		}
		private List<string> pActionRights = new List<string>();

		/// <summary>
		/// List of other security roles that this role includes rights from.
		/// </summary>
		public List<Include> Includes
		{
			get
			{
				return pIncludes;
			}
		}
		private List<Include> pIncludes = new List<Include>();

		private string pCondition;
		/// <summary>
		/// The enabler function to return whether this right is enabled based on the condition implemented by the function.
		/// </summary>
		public string Condition
		{
			get
			{
				return pCondition;
			}
		}

		private string pDescription;
		/// <summary>
		/// MainBoss user level Description field
		/// </summary>
		public string Description
		{
			get
			{
				return pDescription;
			}
		}

		private string pComment;
		/// <summary>
		/// MainBoss user level Comment field
		/// </summary>
		public string Comment
		{
			get
			{
				return pComment;
			}
		}
		#endregion
	}
	#endregion

	#region Include
	/// <summary>
	/// Represent the include element of a right; the namespace attribute indicates what name space to look up the include elements in.
	/// </summary>
	public class Include : NameSpaceAttribute
	{
		public Include(TableRightType from)
			: base(TableRightType.Include)
		{
			From = from;
		}
		internal virtual void FromXml(XmlElement node)
		{
			XmlAttribute attr;
			attr = node.Attributes["demote"];
			pDemote = attr != null && attr.Value == KB.I("true");

			attr = node.Attributes["mask"];
			if (attr != null)
			{
				pMask = TableRight.MapTableRights(attr.Value);
			}
			else
				pMask = TableRightName.All;

			if (node.ChildNodes.Count == 0)
				throw new GeneralException(KB.T("<include> is missing list of rights to include"));
			string[] rights = node.FirstChild.Value.Split(' ');
			pRights = new List<string>(rights);
		}
		internal XmlNode ToXml(XmlDocument root)
		{
			XmlElement node = root.CreateElement(XmlElementName, RightSet.XmlNamespace);
			if (Mask != TableRightName.All)
				node.SetAttribute("Mask", pMask.ToString());
			if (pDemote)
				node.SetAttribute("demote", "true");
			System.Text.StringBuilder text = new System.Text.StringBuilder("");
			foreach (var i in pRights)
			{
				text.Append(i);
				text.Append(" ");
			}
			node.InnerText = text.ToString().TrimEnd();
			return node;
		}
		#region Properties
		private TableRightName pMask;
		public TableRightName Mask
		{
			get
			{
				return pMask;
			}
		}
		private List<string> pRights;
		public List<string> Rights
		{
			get
			{
				return pRights;
			}
		}
		private bool pDemote;
		public bool Demote
		{
			get
			{
				return pDemote;
			}
		}
		public TableRightType From;
		#endregion
	}
	#endregion

	#region RightSet
	public class RightSet
	{
		#region Properties
		public NameAndNameSpaceCollection Rights;
		private Thinkage.Libraries.DBILibrary.DBI_Database pDatabaseSchema;
		#endregion
		#region Constructor
		public RightSet(Thinkage.Libraries.DBILibrary.DBI_Database databaseSchema, string xmlInputUri)
		{
			pDatabaseSchema = databaseSchema;
#if DEBUG
			foreach (Thinkage.Libraries.DBILibrary.DBI_Table t in pDatabaseSchema.Tables)
				TablesWithoutAnyPermissions.Add(t.Name);
			TablesWithoutAnyPermissions.Remove("__Variables");
#endif
			Rights = new NameAndNameSpaceCollection();
			Complete(xmlInputUri);
		}
		#endregion
		#region Xml processing
		/// <summary>
		/// The Xml namespace uri used for the representation of security objects in XML.
		/// </summary>
		public static readonly string XmlNamespace = KB.I("http://www.thinkage.ca/XmlNamespaces/XAF");
		/// <summary>
		/// The URI of the Xml document defining the schema within this assembly.
		/// </summary>
		public static readonly string XmlSchemaURI = KB.I("manifest://localhost/Thinkage/MainBoss/Database/Schema/Security/SecuritySchema.xsd");
		/// <summary>
		/// Load the security definitions from the xml document specified in the parameter
		/// </summary>
		/// <param name="xmlInput">uri where the security input resides</param>
		/// <returns></returns>
		public void Complete(string xmlInputUri)
		{
			Thinkage.Libraries.Xml.ManifestXmlResolver res = new Thinkage.Libraries.Xml.ManifestXmlResolver(System.Reflection.Assembly.GetExecutingAssembly());
			SetupResolver(res);
			LoadFromXml(xmlInputUri, res);
		}
		#region XML Input
		/// <summary>
		/// Prepare a <see cref="ManifestXmlResolver"/> to be able to locate the schema and xml types which define the Xml representation of a <c>Database</c> object.
		/// </summary>
		/// <param name="res">The Xml Resolver to prepare.</param>
		public static void SetupResolver(ManifestXmlResolver res)
		{
			res.RegisterAssembly(System.Reflection.Assembly.GetExecutingAssembly());
		}
		/// <summary>
		/// This class is used by the Xml input process. It stores information to assist in the inclusion process.
		/// </summary>
		protected class XmlInputContext
		{
			/// <summary>
			/// Create a new XmlInputContext which uses the given XmlResolver to resolve URI's, including the one for the
			/// initial database to read.
			/// </summary>
			/// <param name="res"></param>
			public XmlInputContext(XmlResolver res)
			{
				Resolver = res;
			}
			/// <summary>
			/// The XmlResolver used to get documents from uri's
			/// </summary>
			public XmlResolver Resolver;
		}
		/// <summary>
		/// Read the Xml description of a <c>Security description</c> from an Xml <see cref="TextReader"/> into the current object.
		/// The root element of the Xml file should be a <c>&lt;security&gt;</c> as described in SecuritySchema.xsd
		/// </summary>
		/// <param name="uri">the URI of the document for resolution of relative URIs contained within</param>
		/// <param name="res">the XmlResolver used to locate references within the document</param>
		/// 
		public void LoadFromXml([Thinkage.Libraries.Translation.Invariant]string uri, XmlResolver res)
		{
			using (new Thinkage.Libraries.Diagnostics.DebugTimer("Security.LoadFromXml"))
			{
				System.Xml.Schema.XmlSchemaSet schemaCollection = Statics.CreateSchemaValidationSchemaSet(new ManifestXmlResolver(System.Reflection.Assembly.GetExecutingAssembly()), XmlSchemaURI);
				XmlDocument doc;
				using (new Thinkage.Libraries.Diagnostics.DebugTimer("...XmlDocument load"))
				{
					doc = Statics.ReadXmlDocument(schemaCollection, res, uri, "rightset", XmlNamespace);
				}
				if (doc == null)
					throw new GeneralException(KB.T("Cannot find rightset definition document at uri '{0}'"), uri);
				FromXml(doc.DocumentElement, res);
			}
		}
		/// <summary>
		/// Read the Xml description of a <c>Database</c> from an <see cref="XmlNode"/> and its children into the current object.
		/// The children of the node should conform to the <c>database_contents</c> Xml type as described in SchemaTypes.xsd
		/// </summary>
		/// <param name="node">the root element node of the database description</param>
		/// <param name="res">an XmlResolver used to read &lt;include&gt;d documents other than lexical inclusions.
		/// If this is null, includes will not be allowed.</param>
		public void FromXml(XmlNode node, XmlResolver res)
		{
			FromXml(node, new XmlInputContext(res));
		}
		private void FromXml(XmlNode node, XmlInputContext context)
		{
			using (new Thinkage.Libraries.Diagnostics.DebugTimer("RightSet.FromXml (XmlDocument parse)"))
			{
				int i = 0;

				FromXmlMainLoop(ref i, node.ChildNodes, context);
				// Check for any unprocessed childnodes
				for (; i < node.ChildNodes.Count; i++)
				{
					XmlElement child = (XmlElement)node.ChildNodes[i];
					try
					{
						System.Diagnostics.Debug.Assert(false, "Invalid child element <" + child.Name + "> of <" + node.Name + ">");
					}
					catch (System.Exception ex)
					{
						Thinkage.Libraries.Exception.AddContext(ex, child as IExceptionContext);
						throw ex;
					}
				}
			}
		}
		private void FromXmlMainLoop(ref int i, XmlNodeList nodes, XmlInputContext context)
		{
			for (; i < nodes.Count; i++)
			{
				XmlElement child = (XmlElement)nodes[i];
				try
				{
					RoleRight sr;
					if (child.Name == KB.I("right"))
						sr = new RoleRight(TableRightType.Right);
					else if( child.Name == KB.I("role"))
						sr = new RoleRight(TableRightType.Role);
					else if (child.Name == KB.I("internal"))
						sr = new RoleRight(TableRightType.Internal);
					else
						throw new GeneralException(KB.T("Invalid child element <{0}> of <{1}>"),child.Name, child.ParentNode.Name);
					sr.FromXml(child);
					if (Rights.Lookup(sr.Class, sr.Name) != null)
						throw new GeneralException(KB.T("Duplicate {0} name '{1}' in \"{1}\""), sr.Class, sr.Name);
					Rights.Add(sr);
				}
				catch (System.Exception ex)
				{
					throw Thinkage.Libraries.Exception.AddContext(ex, child);
				}
			}
		}
		#endregion
		internal XmlNode ToXml(XmlDocument root)
		{
			XmlElement node = root.CreateElement(KB.I("rightset"), KB.I(RightSet.XmlNamespace));
			foreach (var r in Rights)
				if( r is RoleRight )
					((RoleRight)r).ToXml(root);
			return node;
		}
		#endregion
		// Common demotion while processing include directives
		private TableRightName DemoteFunction(TableRightName currentRight)
		{
			// if currentRight has any of Create,Delete,Edit,View,Browse,EditDefault return View,Browse
			if ((currentRight & ~TableRightName.VB) != TableRightName.None)
				return currentRight & TableRightName.VB;
			// if currentRight has Browse,View, return View
			if ((currentRight & TableRightName.VB) == TableRightName.VB)
				return TableRightName.View;
			return TableRightName.None;
		}
		public class RolePermission
		{
			/// <summary>
			/// Return list of TableOperation permissions based on the TableRight associated with a Table
			/// </summary>
			/// <param name="t"></param>
			/// <param name="right"></param>
			/// <returns></returns>
			internal List<string> TablePermission(string tableName, TableRightName right)
			{
				System.Collections.Generic.List<string> permissions = new System.Collections.Generic.List<string>();
				string prefix = Strings.IFormat("{0}.", tableName);
				if (right == TableRightName.All)
					permissions.Add(prefix + "*");
				else
				{
					if ((right & TableRightName.Browse) != 0)
						permissions.Add(prefix + KB.I("Browse"));
					if ((right & TableRightName.Create) != 0)
						permissions.Add(prefix + KB.I("Create"));
					if ((right & TableRightName.Delete) != 0)
						permissions.Add(prefix + KB.I("Delete"));
					if ((right & TableRightName.Edit) != 0)
						permissions.Add(prefix + KB.I("Edit"));
					if ((right & TableRightName.EditDefault) != 0)
						permissions.Add(prefix + KB.I("EditDefault"));
					if ((right & TableRightName.View) != 0)
						permissions.Add(prefix + KB.I("View"));
				}
				return permissions;
			}
			public RolePermission()
			{
				TableRights = new Dictionary<TableRight,TableRightName>();
				ViewCostPermissions = new List<string>();
				TransitionPermissions = new List<string>();
				ActionPermissions = new List<string>();
			}
			public readonly Dictionary<TableRight, TableRightName> TableRights;
			public List<string> TableRightsAsTablePermissions
			{
				get
				{
					List<string> tablePermissions = new List<string>();
					foreach (var kvp in TableRights)
					{
						if (kvp.Key.Class != TableRightType.Table)
							continue;
						tablePermissions.AddRange(TablePermission(kvp.Key.Name, kvp.Value));
					}
					return tablePermissions;
				}
			}
			public readonly List<string> ViewCostPermissions;
			public readonly List<string> TransitionPermissions;
			public readonly List<string> ActionPermissions;
		}
		/// <summary>
		/// Return the String names for Rights and Permissions associated with a RolePermission with an IEnumerator implementation
		/// </summary>
		public class RolePermissionStrings : System.Collections.IEnumerable
		{
			private readonly RolePermission rp;
			public RolePermissionStrings(RolePermission rp)
			{
				this.rp = rp;
			}
			#region IEnumerable Members
			public System.Collections.IEnumerator GetEnumerator()
			{
				foreach (string r in rp.TableRightsAsTablePermissions)
					yield return KB.I("Table.") + r;
				foreach (string r in rp.ViewCostPermissions)
					yield return KB.I("ViewCost.") + r;
				foreach (string r in rp.TransitionPermissions)
					yield return KB.I("Transition.") + r;
				foreach (string r in rp.ActionPermissions)
					yield return KB.I("Action.") + r;
			}
			#endregion
		}
		Dictionary<RoleRight, RolePermission> RoleToPermissions
		{
			get
			{
				if (pRoleToPermissions == null)
				{
					pRoleToPermissions = new Dictionary<RoleRight, RolePermission>();

					// Now make the RoleToPermissions based on the basic rights associated with each Right
					foreach (RoleRight r in Rights)
					{
						RolePermission rolePerms = new RolePermission();
						rolePerms.ViewCostPermissions.AddRange(r.CostRights);
						rolePerms.TransitionPermissions.AddRange(r.TransitionRights);
						rolePerms.ActionPermissions.AddRange(r.ActionRights);
						foreach (TableRight n in r.TableRightCollection)
							rolePerms.TableRights.Add(new TableRight(n.Name), n.Rights);
						pRoleToPermissions.Add(r, rolePerms);
					}
					// given basic rights associated with all roles, now add the rights associated with included roles on Rights in the default namespace only

					foreach (RoleRight r in Rights)
					{
						RolePermission thisRolePermission = pRoleToPermissions[r];
						if (r.Class == TableRightType.Include || r.Class == TableRightType.Table)
							continue;
						foreach (Include include in r.Includes)
						{	
							foreach (string includeRightName in include.Rights)
							{
								try 
								{
									RolePermission included;
									included = lookupInclude(r, includeRightName, include.From == TableRightType.Role ? TableRightType.Role: TableRightType.Right);
									if( included == null && include.From != TableRightType.Role )
										included = lookupInclude(r, includeRightName, TableRightType.Internal);
									if (included == null)
										throw new GeneralException(KB.T("Undefined or forward reference of \"{0}\" included from \"{1} {2}\""), includeRightName,r.ClassName, r.Name);
									if (!include.Demote)
									{
										thisRolePermission.ViewCostPermissions.AddRange(from p in included.ViewCostPermissions where !thisRolePermission.ViewCostPermissions.Contains(p) select p);
										thisRolePermission.TransitionPermissions.AddRange(from p in included.TransitionPermissions where !thisRolePermission.TransitionPermissions.Contains(p) select p);
										thisRolePermission.ActionPermissions.AddRange(from p in included.ActionPermissions where !thisRolePermission.ActionPermissions.Contains(p) select p);
									}
									foreach (KeyValuePair<TableRight, TableRightName> trp in included.TableRights)
									{
#if DEBUG
										if( !trp.Key.Verified ) {
											// Members of this list are non standard Tables, and don't occur in the Schema
											if (!(new string[] { "CompanyInformation", "Session", "*", "AssignedWorkOrder", "AssignedPurchaseOrder", "AssignedRequest", "UserPrincipal", "CultureInfo", "UnassignedWorkOrder", "UnassignedPurchaseOrder", "UnassignedRequest" }).Contains(trp.Key.Name)
												&& pDatabaseSchema.Tables[trp.Key.Name] == null)
												System.Diagnostics.Debug.Assert(false, Strings.IFormat("\"TableRight {0}\" does not name a known table", trp.Key.Name));
											trp.Key.Verified = true;
										}
#endif
										TableRightName resultingRight = (trp.Value & include.Mask & trp.Key.Rights);
										if (include.Demote)
											resultingRight = DemoteFunction(resultingRight);
										if (resultingRight == TableRightName.None)
											continue;
										var found = thisRolePermission.TableRights.Where(p=>p.Key.Name == trp.Key.Name).Select(p=>p.Key).FirstOrDefault();
										if (found != null)
											thisRolePermission.TableRights[found] |= resultingRight;
										else {
											var ni = new TableRight(trp.Key.Name);
#if DEBUG
											ni.Verified = trp.Key.Verified;
											TablesWithoutAnyPermissions.Remove(trp.Key.Name);
											if ((resultingRight & TableRightName.EditDefault) != 0) {
												Thinkage.Libraries.DBILibrary.DBI_Table t = pDatabaseSchema.Tables[trp.Key.Name];
												if (t != null && t.HasDefaults)
													TablesWithoutAnyPermissions.Remove(t.Default.Name);
											}
#endif
											thisRolePermission.TableRights.Add(new TableRight(trp.Key.Name), resultingRight);
										}
									}
								}
								catch (System.Exception e)
								{
									if (e is KeyNotFoundException || e is System.ArgumentNullException)
										Thinkage.Libraries.Exception.AddContext(e, new MessageExceptionContext(KB.T("<{0} name=\"{1}\"> with <include >{2}</include>"), r.ClassName, r.Name,includeRightName));
									throw;
								}
							}
						}
						TableRightName starperm = thisRolePermission.TableRights.Where(p => p.Key.Name == "*").Select(p => p.Value).FirstOrDefault();
						if (starperm != 0) {
							var todel = thisRolePermission.TableRights.Where(p => p.Key.Name != "*" && (p.Value & ~starperm) == 0).Select(p => p.Key).ToArray();
							foreach (var p in todel)
								thisRolePermission.TableRights.Remove(p);
						}
					}
#if DEBUG
					if(TablesWithoutAnyPermissions.Count > 0)
					{
						var e = new GeneralException(KB.T(Strings.IFormat("{0} tables exist that do not have any associated Permissions", TablesWithoutAnyPermissions.Count)));
						foreach( string n in TablesWithoutAnyPermissions )
							Exception.AddContext(e, new MessageExceptionContext(KB.T(n)));
						Application.Instance.DisplayError(e);
					}
#endif
				}
				return pRoleToPermissions;
			}
		}
		private RolePermission lookupInclude(RoleRight r, string includename, TableRightType aclass)
		{
			RoleRight ir = (RoleRight)Rights.Lookup(aclass, includename);
			if (ir == null || (r.Class == ir.Class && r.Name == ir.Name) )
				return null;
			RolePermission includedRolePermission = RoleToPermissions[ir];
			if (includedRolePermission.TableRights.Count == 0 && includedRolePermission.ViewCostPermissions.Count == 0 && includedRolePermission.TransitionPermissions.Count == 0)
				return null;
			return includedRolePermission;
		}
		Dictionary<RoleRight, RolePermission> pRoleToPermissions;
		/// <summary>
		/// Return a List of all rolenames defined in the Schema
		/// </summary>
		public IEnumerable<string> RightNames
		{
			get
			{
				return from r in Rights where r.Class == TableRightType.Right orderby r.Name select r.Name;
			}
		}
		/// <summary>
		/// Return a List of all rolenames defined in the Schema
		/// </summary>
		public IEnumerable<string> RoleNames
		{
			get
			{
				return from r in Rights where r.Class == TableRightType.Role orderby r.Name select r.Name;
			}
		}
		/// <summary>
		/// Public information about a particular RoleRight and its associated permissions
		/// </summary>
		public struct RoleAndPermission
		{
			public readonly RoleRight Role;
			public readonly RolePermission Permission;
			public readonly bool IsRole; // Versus a Right
			public RoleAndPermission(bool isRole, RoleRight rr, RolePermission rp)
			{
				Role = rr;
				Permission = rp;
				IsRole = isRole;
			}
		}
		/// <summary>
		/// Return a list of RoleAndPermission for each RoleRight that is a Role
		/// </summary>
		public IEnumerable<RoleAndPermission> RolesAndPermissions
		{
			get
			{
				return from p in Rights let r = (RoleRight) p where p.Class == TableRightType.Right || p.Class == TableRightType.Role 
					select new RoleAndPermission(p.Class == TableRightType.Role, r, RoleToPermissions[r]);
			}
		}
		/// <summary>
		/// Role id numbers of various usefull Security Role IDs
		/// not a complete set see Thinkage.MainBoss.Database/Schema/rights.xml
		/// </summary>
		public enum SecurityRoleIDs
		{
			Request = 12,
			WorkOrder = 15,
			Item = 19,
			Unit = 23,
			GeneratePlannedMaintenance = 26,
			PurchaseOrder = 29,
			Accounting = 33,
			AccountingView = 34,
			CodingDefinitions = 36,
			CodingDefinitionsView = 37,
			Administration = 38,
			Contact = 40
		}
		#region Predefined User Roles
		/// <summary>
		/// A role solely for Windows Administrators of the SQL database to permit the most basic of operations.
		/// </summary>
		static public readonly SecurityRoleIDs[] ITAdminUser = {
			SecurityRoleIDs.Administration
		};
		/// <summary>
		/// Security Roles Assigned to a New Database Creator
		/// </summary>
		static public readonly SecurityRoleIDs[] AdminUser = {
			SecurityRoleIDs.Request,
			SecurityRoleIDs.WorkOrder,
			SecurityRoleIDs.Item,
			SecurityRoleIDs.Unit,
			SecurityRoleIDs.GeneratePlannedMaintenance,
			SecurityRoleIDs.PurchaseOrder,
			SecurityRoleIDs.Accounting,
			SecurityRoleIDs.CodingDefinitions,
			SecurityRoleIDs.Administration,
			SecurityRoleIDs.Contact,
		};

		/// <summary>
		/// Security Roles Assigned to a Upgrade to 3.2 Users
		/// </summary>
		static public readonly SecurityRoleIDs[] UpgradeUser = {
			SecurityRoleIDs.Request,
			SecurityRoleIDs.WorkOrder,
			SecurityRoleIDs.Item,
			SecurityRoleIDs.Unit,
			SecurityRoleIDs.GeneratePlannedMaintenance,
			SecurityRoleIDs.PurchaseOrder,
			SecurityRoleIDs.AccountingView,
			SecurityRoleIDs.CodingDefinitionsView,
			SecurityRoleIDs.Contact,
		};
		#endregion
		/// <summary>
		/// Return a list of RoleAndPermission reserved for the DatabaseCreator
		/// </summary>
		public IEnumerable<RoleAndPermission> RolesAndPermissionsFor(SecurityRoleIDs[] RoleIDs)
		{
			return from p in Rights let r = (RoleRight)p  where  p.Class == TableRightType.Role && System.Array.Exists(RoleIDs,(a => (int)a == r.Id ))
				select new RoleAndPermission(true, r, RoleToPermissions[r]);
		}

		public RolePermission RolePermissions(TableRightType aClass, string role)
		{
			return RoleToPermissions[(RoleRight)Rights.Lookup(aClass, role)];
		}
		#region FindRoles
		/// <summary>
		/// Find all roles that have the given permissions
		/// </summary>
		/// <param name="permissionName">Permission to be found</param>
		/// <returns> A list of Roles into groups by separated by null strings</returns>
		public List<string> FindRoles(string permissionName)
		{
			string[] pieces = permissionName.Split('.');
			string what = pieces[0];
			string name = pieces[1];
			List<string> rolenames;
			if (what == KB.I("Table"))
			{
				string right = Strings.IFormat("{0}.{1}", name, pieces[2]);
				string full = Strings.IFormat("{0}.*", name);
				rolenames = FindRoles(p => RoleToPermissions[p].TableRightsAsTablePermissions, right, full);
#if !DEBUG
				// Roles All, allows access to all the tables but no user should
				// every be forced to assign that role to a user, but during
				// developement we want to note when we are required to use it
				if (rolenames.Count() == 0) {
					right = Strings.IFormat("*.{1}", pieces[2]);
					rolenames = FindRoles(p => RoleToPermissions[p].TableRightsAsTablePermissions, right, "*.*");
				}
#endif
			}
			else if (what == KB.I("Transition"))
			{
				string transition = Strings.IFormat("{0}.{1}", name, pieces[2]);
				rolenames = FindRoles(p => RoleToPermissions[p].TransitionPermissions, transition, transition);
			}
			else if (what == KB.I("Action"))
				rolenames = FindRoles(p => RoleToPermissions[p].ActionPermissions, name, name);
			else if (what == KB.I("ViewCost"))
				rolenames = FindRoles(p => RoleToPermissions[p].ViewCostPermissions, name, name);
			else
				throw new GeneralException(KB.K("No Right type for \"{0}\""), permissionName, name);
			if (rolenames.Count() == 0)
				throw new GeneralException(KB.K("No Right has permissions for \"{0}\""), permissionName);
			return rolenames;
		}

		private delegate IEnumerable<string> FindTable(RoleRight r);
		private List<string> FindRoles(FindTable t, [Thinkage.Libraries.Translation.Invariant]string perm, [Thinkage.Libraries.Translation.Invariant]string full)
		{
			int lastrank = 10000;
			var result = new List<string>();
			// kludge skipping "All" and "AllView"
			foreach( var rr in from r in Rights.OfType<RoleRight>()
				   where r.Class == TableRightType.Role && r.Name != KB.I("All") && r.Name != KB.I("AllView") && t(r).Any(p => p == perm || p == full)
				   let tr = KB.K(r.Name).Translate()
				   orderby r.Rank, tr
				   select new { Rank = r.Rank, Role = tr }) {
				if( rr.Rank-lastrank > 10 )
					result.Add("");
				result.Add(rr.Role);
				lastrank = rr.Rank;
			}
			return result;
		}
		#endregion
#if DEBUG
		// These members provide a means to retain settings during a mainboss session for debugging purposes
		public Thinkage.Libraries.Collections.Set<object> SelectedRoles = new Thinkage.Libraries.Collections.Set<object>(); // for allowing persistence of selections across debug picker
		public Thinkage.Libraries.Collections.Set<object> SelectedViewCostRoles = new Thinkage.Libraries.Collections.Set<object>(); // for allowing persistence of selections across debug picker
		public Thinkage.Libraries.Collections.Set<object> SelectedViewCosts= new Thinkage.Libraries.Collections.Set<object>(); // for allowing persistence of selections across debug picker
		private Thinkage.Libraries.Collections.Set<string> TablesWithoutAnyPermissions = new Libraries.Collections.Set<string>();
#endif
	}
	#endregion
}
