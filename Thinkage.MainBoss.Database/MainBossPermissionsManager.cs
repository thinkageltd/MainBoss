using System.Collections.Generic;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries;
using System;

namespace Thinkage.MainBoss.Database {
	#region MainBossPermissionDisabler - A derivation of XAF.UI.PermissionDisabler whose disabled tip tries to describe the role(s) that grant the right.
	/// <summary>
	/// A PermissionDisabler with a customized DisablerTip that indicates 'what' role a user requires to obtain the permission the disabler is guarding.
	/// </summary>
	public class MainBossPermissionDisabler : PermissionDisabler {
		public MainBossPermissionDisabler(MainBossPermissionsManager permsManager, PermissionsGroup owner, [Invariant]string name)
			: base(owner, name) {
				MainBossPermissionsManager = permsManager;
		}
		private readonly MainBossPermissionsManager MainBossPermissionsManager;
		public override string DisablerTip {
			get {
				try {
					var rolenames = MainBossPermissionsManager.MapPermissionToRoles(base.QualifiedName);
					System.Text.StringBuilder result = new System.Text.StringBuilder();
#if DEBUG
					result.AppendFormat(KB.I("Debug {0}: "), base.QualifiedName);
#endif
					if (rolenames.Count == 0)
						return result.Append(KB.I("You do not have the required permission, and no security roles grant it")).ToString();
					if (rolenames.Count == 1)
						return result.Append(Thinkage.Libraries.Strings.Format(KB.K("You do not have the required permission. You must be a member of security role {0}"), rolenames[0])).ToString();

					result.Append(KB.K("You do not have the required permission. You must be a member of one of the following security roles"));
					result.AppendLine();
					result.Append(rolenames[0]);
					int online = 1; // initial already set
					bool linebreak;
					bool endgroup = false;
					bool wassingle = true;
					for (int j = 1; j < rolenames.Count; ++j) {
						if (rolenames[j] == "") {
							endgroup = true;
							continue;
						}
						bool issingle = j + 1 >= rolenames.Count || rolenames[j + 1] == "";
						linebreak = online++ > 5 || (endgroup && !(wassingle && issingle));
						result.Append(linebreak ? KB.I(",\n") : KB.I(", "));
						result.Append(rolenames[j]);
						if (linebreak)
							online = 0;
						wassingle = (wassingle || online == 1) && endgroup;
						endgroup = false;
					}
					return result.ToString();
				}
				catch (System.Exception ex) {
					// NOTE: If you get this message and {1} is an ArgumentOutOfRangeException it probably means that the required permission is not granted by any role.
					return Thinkage.Libraries.Strings.Format(KB.K("No permissions for ({0}); no security role information: {1}"), base.QualifiedName, Thinkage.Libraries.Exception.FullMessage(ex));
				}
			}
		}
	}
	#endregion
	#region MainBossPermissionsManager - A PermissionsManager that creates MainBossPermissionDisabler leaf nodes
	public class MainBossPermissionsManager : PermissionsManager, IRightGrantor {
#if DEBUG
		#region Right Set Support
		private Database.Security.RightSet pCurrentRightSet;
		public Database.Security.RightSet CurrentRightSet {
			get {
				if (pCurrentRightSet == null) {
					var newSet = new Database.Security.RightSet(dsMB.Schema, Database.SecurityCreation.RightSetLocation);
					CheckSecurityDefinitions(newSet);
					pCurrentRightSet = newSet;
				}
				return pCurrentRightSet;
			}
		}
		#region DEBUG CHECKS
		static bool beenHere = false;
		[System.Diagnostics.Conditional("DEBUG")]
		private void CheckSecurityDefinitions(Database.Security.RightSet security) {
			if (beenHere) return;
			beenHere = true;

			var SchemasWithoutUsedTableRights = new List<string>();
			foreach (DBI_Table t in dsMB.Schema.Tables) {
				if (t.IsDefaultTable || t.IsVariantBaseTable)
					continue;
				// Exceptions for special names appear here
				switch (t.Name) {
				case "__Variables":
					continue;
				default:
					break;
				}
				SchemasWithoutUsedTableRights.Add(t.Name);
			}

			System.Text.StringBuilder errors = new System.Text.StringBuilder();
			foreach (string role in security.RoleNames) {
				var rolePermission = security.RolePermissions(Thinkage.Libraries.DBILibrary.Security.TableRightType.Role, role);
				foreach (string costright in rolePermission.ViewCostPermissions) {
					if (costright.Equals("*"))
						continue;
					if (Root.Rights.ViewCost.FindDirectChild(costright) == null)
						errors.AppendLine(Thinkage.Libraries.Strings.IFormat("costright '{0}' referenced in role '{1}' is not defined in MB3Rights ViewCost group", costright, role));
				}
				foreach (string transitionright in rolePermission.TransitionPermissions) {
					if (transitionright.Equals("*"))
						continue;
					string[] parts = transitionright.Split('.');
					if (Root.Rights.Transition.FindDirectChild(parts[0]) == null)
						errors.AppendLine(Thinkage.Libraries.Strings.IFormat("Transition right '{0}' referenced in role '{1}' is not defined in MB3Rights Transition group ", transitionright, role));
				}
				foreach (string actionright in rolePermission.ActionPermissions) {
					if (actionright.Equals("*"))
						continue;
					if (Root.Rights.Action.FindDirectChild(actionright) == null)
						errors.AppendLine(Thinkage.Libraries.Strings.IFormat("Action right '{0}' referenced in role '{1}' is not defined in MB3Rights Action group ", actionright, role));
				}
				foreach (KeyValuePair<Thinkage.Libraries.DBILibrary.Security.TableRight, Thinkage.Libraries.DBILibrary.Security.TableRightName> kvp in rolePermission.TableRights) {
					if (kvp.Key.Class != Thinkage.Libraries.DBILibrary.Security.TableRightType.Table
						&& (kvp.Key.Rights & Thinkage.Libraries.DBILibrary.Security.TableRightName.Create) != 0)
						continue;
					SchemasWithoutUsedTableRights.Remove(kvp.Key.Name);
				}
			}
			if (SchemasWithoutUsedTableRights.Count > 0) {
				errors.AppendLine(KB.I("Following table schemas are defined but not referenced in any role as a table right"));
				foreach (string s in SchemasWithoutUsedTableRights)
					errors.AppendLine(s);
			}
			if (errors.Length > 0) {
				errors.Insert(0, KB.I("Errors detected in SecurityRights definition (this exception will not occur again)") + System.Environment.NewLine);
				Application.Instance.DisplayError(new Thinkage.Libraries.GeneralException(KB.T(errors.ToString())));
			}
		}
		#endregion
		#endregion
#endif
		#region Permission To Role Mapping
		/// <summary>
		/// Fetch the defined Role names from the current database to ensure proper translation; also will find user defined roles (future) attributed to a particular permission.
		/// </summary>
		/// <param name="permissionName"></param>
		/// <returns></returns>
		public List<string> MapPermissionToRoles([Thinkage.Libraries.Translation.Invariant] string permissionName) {
			if (RolesGrantingPermission != null) { // Will be empty if no one has called InitializeRolesGrantingPermission (or incorrect if they switched sessions underneath us)
				if (RolesGrantingPermission.TryGetValue(permissionName, out List<string> result))
					return result;
				// Try applying a '*' pattern for Tables to find permissions applied to all operations on a particular table.
				if (RolesGrantingPermission.TryGetValue(TablePermissionPattern.Replace(permissionName, KB.I("$1*")), out result))
					return result;
			}
			return new List<string>(); // empty list
		}
		private static readonly System.Text.RegularExpressions.Regex TablePermissionPattern = new System.Text.RegularExpressions.Regex(KB.I("\\A(Table\\.[\\p{L}_]\\w*\\.)[\\p{L}_]\\w*\\Z"));
		#endregion
		#region Constructor
		public MainBossPermissionsManager(MB3RootRights rights)
			: base(false, delegate(PermissionsManager mgr, PermissionsGroup g, string name) {
				return new MainBossPermissionDisabler((MainBossPermissionsManager)mgr, g, name);
			}) {
		}
		#endregion
		#region IRightGrantor Members
		public Permission GetPermission(Right r) {
			return FindPermission(r.QualifiedName);
		}
		public GeneralException ValidatePermission(string permissionPattern) {
			if (permissionPattern == null)
				return null;
			return Root.Rights.ValidatePermissionPattern(permissionPattern);
		}
		#endregion
		public PermissionsGroup GetGroup(RightsGroup g) {
			return FindPermissionGroup(g.QualifiedName);
		}
		/// <summary>
		/// This sets the RolesGrantingPermission dictionary so permission disabler tips can identify which roles are required for a particular permission.
		/// </summary>
		/// <param name="db"></param>
		public void InitializeRolesGrantingPermission(DBClient session) {
			var x = Thinkage.MainBoss.Database.Licensing.AllMainbossLicenses; // access a static to force translations into memory before we use them below.
			RolesGrantingPermission = new Dictionary<string, List<string>>();
			Libraries.DBAccess.DBVersionHandler vh = MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(session);
			if (vh.CurrentVersion >= new Version(1, 0, 10, 32))
				using (dsMB ds = new dsMB(session)) {
					session.ViewOnlyRows(ds, dsMB.Schema.T.Permission, new SqlExpression(dsMB.Path.T.Permission.F.PrincipalID.F.RoleID).IsNotNull()
							.And(new SqlExpression(dsMB.Path.T.Permission.F.PrincipalID.F.RoleID.F.Code).NEq(SqlExpression.Constant("All")))
							.And(new SqlExpression(dsMB.Path.T.Permission.F.PrincipalID.F.RoleID.F.Code).NEq(SqlExpression.Constant("AllView")))
							.Or(new SqlExpression(dsMB.Path.T.Permission.F.PrincipalID.F.CustomRoleID).IsNotNull()), null, new DBI_PathToRow[] {
							dsMB.Path.T.Permission.F.PrincipalID.PathToReferencedRow,
							dsMB.Path.T.Permission.F.PrincipalID.F.RoleID.PathToReferencedRow,
							dsMB.Path.T.Permission.F.PrincipalID.F.CustomRoleID.PathToReferencedRow
						});
					foreach (dsMB.PermissionRow pr in ds.T.Permission) {
						if (!RolesGrantingPermission.TryGetValue(pr.F.PermissionPathPattern, out List<string> roles))
							RolesGrantingPermission.Add(pr.F.PermissionPathPattern, roles = new List<string>());
						roles.Add(pr.PrincipalIDParentRow.F.RoleID.HasValue ? pr.PrincipalIDParentRow.RoleIDParentRow.F.RoleName.Translate() : pr.PrincipalIDParentRow.CustomRoleIDParentRow.F.Code);
					}
				}
			foreach (var list in RolesGrantingPermission.Values) // sort all the built lists for display purposes
				list.Sort();
		}
		private Dictionary<string, List<string>> RolesGrantingPermission;

	}
	#endregion
}
