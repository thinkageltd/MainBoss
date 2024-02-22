using System.Web.UI.WebControls;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;
namespace Thinkage.Libraries.Presentation.ASPNet {
	public class CommandButton : Button {
		public CommandButton(Key caption, ICommand command) {
			Text = caption.Translate();
			pCommand = command;
		}
		protected override void OnClick(System.EventArgs e) {
			base.OnClick(e);
			if (!pCommand.Enabled)
				throw new GeneralException(KB.K("This operation is not permitted")).WithContext(new MessageExceptionContext(pCommand.Tip));
			pCommand.Execute();	// TODO: This should be done through a common wrapper method as it is done in WinControls. Perhaps another App interface method.
		}
		private ICommand pCommand;
	}
}