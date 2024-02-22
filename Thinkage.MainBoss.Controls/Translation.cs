using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Keybuilder for the Thinkage.MainBoss.Controls context
	/// </summary>
	internal class KB : Thinkage.Libraries.Translation.KB {
		static KB() {
			DeclareAssemblyProvidesTranslationsUsingResource(K(null), System.Reflection.Assembly.GetExecutingAssembly());
		}
		static readonly KB Instance = new KB();
		public static SimpleKey K([Context(Level = 1)] string s) {
#if FINDMISSINGTIPS
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(s));
#endif
			return Instance.BuildKey(s);
		}
		/// <summary>
		/// Produce a composed key from the given TableObjct name for a collective form of the name (typically just plural).
		/// This would be used to create a label for places like TabNodes or group boxes containing browsettes to identify what the browsette shows.
		/// It is essentially a short form of the phrase "Browse {0}" without the word 'Browse'
		/// </summary>
		/// <param name="ident">The Tbl identification</param>
		/// <returns></returns>
		public static Key TOc(Thinkage.Libraries.Presentation.Tbl.TblIdentification ident) {
			return ident.Compose("Collection of {0}");
		}
		/// <summary>
		/// Produce a composed key from the given TableObject name to refer to an individual record in the table (typically just singular).
		/// This would be used to create a label for a group box or multicolumn-layout row to identify what the group or row is showing.
		/// </summary>
		/// <param name="ident">The Tbl identification</param>
		/// <returns></returns>
		public static Key TOi(Thinkage.Libraries.Presentation.Tbl.TblIdentification ident) {
			return ident.Compose("Singular {0}");
		}
		/// <summary>
		/// Produce a composed key from the given TableObject name for use as a menu item or control panel item that will display a browser (viewing) of table records
		/// </summary>
		/// <param name="ident">The Tbl identification</param>
		/// <returns></returns>
		public static Key TOControlPanel(Thinkage.Libraries.Presentation.Tbl.TblIdentification ident) {
			return ident.Compose("ControlPanel {0}");
		}
		public static Key TOBrowsetteTip(Thinkage.Libraries.Presentation.Tbl.TblIdentification master, Thinkage.Libraries.Presentation.Tbl.TblIdentification target) {
			return master.Compose("Display the {1} for this {0}", target);
		}
		public static Key TOBrowsetteLabel(Thinkage.Libraries.Presentation.Tbl.TblIdentification master, Thinkage.Libraries.Presentation.Tbl.TblIdentification target) {
			return master.Compose("{1} in context of {0}", target);
		}
	}
	/// <summary>
	/// ControlPanel verbs are segregated from regular controls for translation context assistance
	/// </summary>
	public class ControlPanelContext : Thinkage.Libraries.Translation.KB {
		static readonly ControlPanelContext Instance = new ControlPanelContext();
		public static SimpleKey K([Context("ControlPanel", Level = 1)] string s) {
			return Instance.BuildKey(s);
		}
	}
}