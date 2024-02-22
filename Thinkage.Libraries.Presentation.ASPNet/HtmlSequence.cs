using System.Web.UI.HtmlControls;
namespace Thinkage.Libraries.Presentation.ASPNet {
	public class HtmlSequence : HtmlContainerControl {
		public HtmlSequence()
			: base(null) {
		}
		protected override void Render(System.Web.UI.HtmlTextWriter writer) {
			RenderChildren(writer);
		}
	}
}