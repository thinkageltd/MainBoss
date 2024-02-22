using Thinkage.Libraries;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Application {
	using System;
	using Thinkage.Libraries.XAF.Database.Layout;
	using Thinkage.Libraries.Presentation;
	using Thinkage.Libraries.XAF.Database.Service;
	using Thinkage.MainBoss.Controls;
	/// <summary>
	/// A collection of commonly shared Tbl definitions for MainBoss Applications
	/// </summary>
	public static class TICommon {
		#region RestoreExistingTblCreator
		public delegate void NotifyOnCompletion();
		public static Tbl RestoreExistingTblCreator(string dbserver, string dbname, AuthenticationCredentials credentials, NotifyOnCompletion notifyOnCompletion) {
			// TODO: Make this resistant to DB version changes and damaged DB contents.
			return new Tbl(dsMB.Schema.T.BackupFileName, TId.Backup,
				new Tbl.IAttr[] {
					new CustomSessionTbl(
						delegate(Thinkage.Libraries.DBAccess.DBClient existingDBAccess, DBI_Database newSchema, out bool callerHasCustody) {
							callerHasCustody = true;
							var db = new MB3Client(new MB3Client.ConnectionDefinition(dbserver, dbname, credentials));
							var manager = new MainBossPermissionsManager(Root.Rights);
							var versionHandler = MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(db);
							Guid currentUser = versionHandler.IdentifyUser(db);
							versionHandler.LoadPermissions( db, currentUser, true, delegate(string pattern, bool grant) {
								manager.SetPermission(pattern, grant);
							});
							manager.InitializeRolesGrantingPermission(db);
							TableOperationRightsGroup rightsGroup = (TableOperationRightsGroup)Root.Rights.Table.FindDirectChild(dsMB.Schema.T.License.MainTableName);
							var permissionRequired = (IDisablerProperties)manager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create));
							if (!permissionRequired.Enabled) {
								throw new GeneralException(permissionRequired.Tip);
							}
							return db;
						}),
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.BackupFileName.F.FileName),
						BTbl.ListColumn(dsMB.Path.T.BackupFileName.F.LastBackupDate),
						BTbl.AdditionalVerb(KB.K("Restore"),
							delegate(BrowseLogic browserLogic) {
								Libraries.DataFlow.Source fileNameSource = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.BackupFileName.F.FileName, -1);
								return new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Restore the selected organization from one of its backups"),
									delegate() {
										MB3Client.ConnectionDefinition cd = ((MB3Client)browserLogic.DB).ConnectionInfo;
										var fileName = (string)fileNameSource.GetValue();
										// We schedule the actual restore operation as a callback, then close this form.
										// That way, the MB3Client we have will be closed by the time the actual restore is occurring.
										// The delegate does not refer to any members of the form or logic object and so will work fine after the form closes.
										Thinkage.Libraries.Application.Instance.GetInterface<IIdleCallback>().ScheduleIdleCallback(fileName,
											delegate() {
												IProgressDisplay ipd = null;
												System.Exception completionInformation = null;
												System.Exception errorInformation = null;
												try {
													ipd = browserLogic.CommonUI.UIFactory.CreateProgressDisplay(KB.K("Restoring database"), -1);
													// This is in theory restoring from a backup we made, and we always overwrite (not append) so there should only be one
													// backup set in the file. To ensure this we pass null as the backup set number.
													completionInformation = MB3Client.RestoreDatabase(cd, fileName, null, ipd);
												}
												catch( System.Exception x){
													errorInformation = x;
												}
												finally {
													if (ipd != null)
														ipd.Complete();
												}
												if( errorInformation != null)
													Libraries.Application.Instance.DisplayError(errorInformation);
												else if (completionInformation != null)
													Libraries.Application.Instance.DisplayInfo(completionInformation);
												notifyOnCompletion();
											});
										browserLogic.BrowseUI.Form.CloseForm(UIDialogResult.OK);
									}), browserLogic.NeedSingleSelectionDisabler);
							})
					)
				},
				new TblLayoutNodeArray()
			);
		}
		#endregion
	}
}