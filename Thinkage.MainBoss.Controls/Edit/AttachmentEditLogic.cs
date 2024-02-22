using System.Collections.Generic;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	public class AttachmentEditLogic : EditLogic {
		public AttachmentEditLogic(IEditUI editor, DBClient db, Tbl tbl, Settings.Container settingsContainer, EdtMode mode, object[][] rowIDs, bool[] subsequentModeRestrictions, List<TblActionNode>[] initLists)
			: base(editor, db, tbl, settingsContainer, mode, rowIDs, subsequentModeRestrictions, initLists) {
		}

		protected override void SetupCommands() {
			base.SetupCommands();
			MutuallyExclusiveCommandSetDeclaration cgd = new MutuallyExclusiveCommandSetDeclaration {
				new CommandDeclaration(KB.K("Open Attachment"), new OpenAttachmentCommand(RecordManager.GetPathNotifyingValue(TInfo.Schema.IsDefaultTable ? dsMB.Path.T.Attachment.F.Path.DefaultPath : dsMB.Path.T.Attachment.F.Path, 0)))
			};
			CommandGroupDeclarationsInOrder.Insert(CommandGroupDeclarationsInOrder.Count - 1, cgd);
		}
	}
	#region OpenAttachmentCommand
	/// <summary>
	/// A command that will open a pathname/url using the system default program associated with the url
	/// </summary>
	public class OpenAttachmentCommand : Thinkage.Libraries.XAF.UI.CommandRequiringValue {
		public OpenAttachmentCommand(NotifyingSource source)
			: base(StringTypeInfo.NonNullUniverse, source) {
		}
		#region ICommand Members

		public override void Execute() {
			System.Diagnostics.Process.Start((string)Value);
		}
		public override bool RunElevated {
			get {
				return false;
			}
		}

		#endregion
		protected override Thinkage.Libraries.Translation.Key EnabledTip {
			get {
				return KB.K("Open the attachment using the system's default file association");
			}
		}
	}
	#endregion
}
