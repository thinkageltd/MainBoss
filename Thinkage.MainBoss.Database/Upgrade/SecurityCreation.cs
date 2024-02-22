using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.Database.Layout.Security;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Database {
	public static class SecurityCreation {
		public static readonly string RightSetLocation = KB.I("manifest://localhost/Thinkage.MainBoss.Database,Thinkage/MainBoss/Database/Schema/Security/rightset.xml");

		/// <summary>
		/// Populate the necessary security data tables according to a given XML definition of rights and roles.
		/// </summary>
		/// <param name="updateDs"></param>
		/// <param name="databaseCreatorId">Id of User table record to which creation roles apply (as defined in rightsetUri)</param>
		/// <param name="rightsetUri"></param>
		public static void CreateSecurityDataSet(dsMB updateDs, Guid? databaseCreatorId, [Invariant] string rightsetUri) {
			DBClient db = updateDs.DB;
			Security.RightSet securitySet = new Security.RightSet(dsMB.Schema, rightsetUri);

			updateDs.EnsureDataTableExists(dsMB.Schema.T.User, dsMB.Schema.T.Contact, dsMB.Schema.T.Role, dsMB.Schema.T.Principal, dsMB.Schema.T.UserRole);

			// Fetch any permissions applied to builtin roles
			db.ViewOnlyRows(updateDs, dsMB.Schema.T.Permission, new SqlExpression(dsMB.Path.T.Permission.F.PrincipalID.F.RoleID).IsNotNull(), null, null);
			// Fetch the Principal and Role records for all builtin Roles; we want additional roles as we may have added a databaseCreator user record during database creation.
			db.ViewAdditionalRows(updateDs, dsMB.Schema.T.Principal, new SqlExpression(dsMB.Path.T.Principal.F.RoleID).IsNotNull(), null, new DBI_PathToRow[] { dsMB.Path.T.Principal.F.RoleID.PathToReferencedRow });
			// Fetch the UserRole records associated with builtin roles
			db.ViewAdditionalRows(updateDs, dsMB.Schema.T.UserRole, new SqlExpression(dsMB.Path.T.UserRole.F.PrincipalID.F.RoleID).IsNotNull(), null, new DBI_PathToRow[] { dsMB.Path.T.UserRole.F.UserID.PathToReferencedRow, dsMB.Path.T.UserRole.F.PrincipalID.F.RoleID.PathToReferencedRow });

			// We update existing role information with the current definition. We keep track of preexisting builtin roles and if they are not present in the current set, we 
			// delete UserRoles associated with the now deleted Role, and delete the role after updating existing information

			// Delete all the permissions
			for (int i = updateDs.T.Permission.Rows.Count; --i >= 0;)
				updateDs.T.Permission.Rows[i].Delete();
			// Add all the known principal rows to this list initially;
			// we remove rows as we process them from the new set; any left over will be deleted
			var principalsToDelete = new List<dsMB.PrincipalRow>();
			for (int i = updateDs.T.Principal.Rows.Count; --i >= 0;) {
				dsMB.PrincipalRow row = (dsMB.PrincipalRow)updateDs.T.Principal.Rows[i];
				if (row.F.RoleID.HasValue) // delete only Builtin Roles (only builtin roles are in the dataset)
					principalsToDelete.Add(row);
			}

			// TODO: Something in the securitySet must distinguish true roles from 'permission sets' used as intermediate
			// values in the process of defining the role rights.
			foreach (Security.RightSet.RoleAndPermission p in securitySet.RolesAndPermissions) {
				if (p.IsRole == false)
					// we don't put Rights in the Database at present
					continue;
				// Change the Id fields to KnownIds
				Guid knownRoleId = KnownIds.RoleAndPrincipalIDFromRoleRight(p.Role.Id, out Guid knownPrincipalId);
				dsMB.RoleRow row;
				// locate an existing known role/principal
				dsMB.PrincipalRow prow = (dsMB.PrincipalRow)updateDs.T.Principal.Rows.Find(knownPrincipalId);
				if (prow == null) {
					// make a new role and set the linkage Ids to the known ids
					row = (dsMB.RoleRow)db.AddNewRowAndBases(updateDs, dsMB.Schema.T.Role);
					prow = row.PrincipalIDParentRow;
					row.SetReadOnlyColumn(dsMB.Schema.T.Role.F.Id, knownRoleId);
					prow.SetReadOnlyColumn(dsMB.Schema.T.Principal.F.Id, knownPrincipalId);
					prow.SetReadOnlyColumn(dsMB.Schema.T.Principal.F.RoleID, knownRoleId);
					row.SetReadOnlyColumn(dsMB.Schema.T.Role.F.PrincipalID, knownPrincipalId);
				}
				else {
					// we are updating a known role; remove from deletion list
					principalsToDelete.Remove(prow);
					row = prow.RoleIDParentRow;
				}
				string RoleNameKey = Strings.IFormat("{0}_Name", p.Role.Name);
				string RoleCommentKey = Strings.IFormat("{0}_Comment", p.Role.Name);
				string RoleDescKey = Strings.IFormat("{0}_Desc", p.Role.Name);
				row.F.Code = p.Role.Name;
				row.F.RoleName = KB.K(RoleNameKey);
				if (p.Role.Comment != null)
					row.F.RoleComment = KB.K(RoleCommentKey);
				if (p.Role.Description != null)
					row.F.RoleDesc = KB.K(RoleDescKey);

				foreach (string r in new Security.RightSet.RolePermissionStrings(p.Permission))
					AddPermission(db, updateDs, row.F.PrincipalID, r);
			}
			// now delete unused principal rows and any UserRoles linked to them
			// Need the roleIds associated with the remaining principalsToDelete
			var deletedRoleIds = new List<Guid>(principalsToDelete.ConvertAll<Guid>((r) => {
				return r.F.Id;
			}));
			var deletedRoleWarning = new System.Text.StringBuilder();
			// We will be deleting the Role record by deleting its base Principal record. This is the usual way of deleting a derived record.
			// However, the dependency checking in Update doesn't detect the CASCADE delete referential linkage in its determination
			// of what order tables are updated, and so it does not detect that the UserRole record must be deleted before the Role record it
			// references. As a result a foreign constraint violation could occur if DBClient lucks out to choose the wrong deletion order.
			// Also, multiple UserRole records may refer to the same Role record so we can't delete the Role record while we are deleting the UserRole Records.
			// To avoid this we explicitly delete the UserRole records. Then it is safe to simply delete the Principal /Role record and rely on the CASCADE delete.
			for (int i = updateDs.T.UserRole.Rows.Count; --i >= 0;) {
				dsMB.UserRoleRow row = (dsMB.UserRoleRow)updateDs.T.UserRole.Rows[i];
				dsMB.PrincipalRow prow = row.PrincipalIDParentRow;

				if (deletedRoleIds.Contains(prow.F.Id)) {
					if (!row.UserIDParentRow.F.Hidden.HasValue)
						deletedRoleWarning.AppendLine(Strings.Format(KB.K("User with Authentication Credential '{0}' belonged to role named '{1}' which no longer exists; the association has been deleted."), row.UserIDParentRow.F.AuthenticationCredential, prow.RoleIDParentRow.F.RoleName.Translate()));
					// delete the UserRole now
					row.Delete();
				}
			}
			// now delete the actual principal/role records
			foreach (dsMB.PrincipalRow d in principalsToDelete)
				d.Delete();

			if (deletedRoleWarning.Length > 0)
				Thinkage.Libraries.Application.Instance.DisplayInfo(deletedRoleWarning.ToString());

			// If necessary assign XML defined roles for the database creator user id.
			if (databaseCreatorId.HasValue)
				GrantAccess(updateDs, databaseCreatorId.Value, securitySet, Security.RightSet.AdminUser);
		}
		public static void GrantCreatorAccess(dsMB updateDs, Guid databaseCreatorId, [Invariant] string rightsetUri) {
			GrantAccess(updateDs, databaseCreatorId, new Security.RightSet(dsMB.Schema, rightsetUri), Security.RightSet.AdminUser);
		}
		public static void GrantITAdminAccess(dsMB updateDs, Guid databaseCreatorId, [Invariant] string rightsetUri) {
			GrantAccess(updateDs, databaseCreatorId, new Security.RightSet(dsMB.Schema, rightsetUri), Security.RightSet.ITAdminUser);
		}
		private static void GrantAccess(dsMB updateDs, Guid databaseCreatorId, Security.RightSet securitySet, Security.RightSet.SecurityRoleIDs[] roleSet) {
			// We do not create duplicates of rows already in the dataset, so the caller can issue a query ahead of time to get these if they
			// want to avoid duplicates in the database.
			foreach (Security.RightSet.RoleAndPermission p in securitySet.RolesAndPermissionsFor(roleSet)) {
				dsMB.UserRoleRow row;

				_ = KnownIds.RoleAndPrincipalIDFromRoleRight(p.Role.Id, out Guid roleKnownPrincipalId);
				dsMB.UserRoleRow[] hits = updateDs.T.UserRole.Rows.Select(
						new SqlExpression(dsMB.Path.T.UserRole.F.UserID).Eq(SqlExpression.Constant(databaseCreatorId))
						.And(new SqlExpression(dsMB.Path.T.UserRole.F.PrincipalID).Eq(SqlExpression.Constant(roleKnownPrincipalId))));
				if (hits.Length > 0)
					continue;
				row = (dsMB.UserRoleRow)updateDs.DB.AddNewRowAndBases(updateDs, dsMB.Schema.T.UserRole);
				row.F.UserID = databaseCreatorId;
				row.F.PrincipalID = roleKnownPrincipalId;
			}
		}
		private static void AddPermission(DBClient db, dsMB updateDs, Guid principalId, string permission) {
			dsMB.PermissionRow prow = (dsMB.PermissionRow)db.AddNewRowAndBases(updateDs, dsMB.Schema.T.Permission);
			prow.F.PermissionPathPattern = permission;
			prow.F.PrincipalID = principalId;
		}
	}
}