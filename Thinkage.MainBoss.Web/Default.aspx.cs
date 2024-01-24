using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Thinkage.Libraries;
using Thinkage.MainBoss.Controls;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.Presentation.ASPNet;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;

namespace Thinkage.MainBoss.Web {
	public partial class _Default : System.Web.UI.Page {
		protected void Page_Load(object sender, EventArgs e) {
			//
			// On first entry we want to:
			// create an Application derivation
			// open the database
			// identify the user
			// load the user's permissions
			// check licensing and enable feature groups
			// Essentially we want to do all of that on *any* page when we find there is no Application object, or its DBClient is defective, or the user or
			// db identification information has changed.
			//
			// This means that each page must check its own feature group; it is not sufficient to merely hide links to suppressed features since a user can
			// manually construct a URL to get there anyway.
			//
			// We want the database identification information to come from IIS config parameters, ideally. Or maybe we can get custom data from web.config.
			//
			// We want some infrastructure for handling exceptions and error messages during page handling. In general if a page is handling a postback event
			// and gets an error, the user should get back the same page with the error message appearing somewhere near the control that caused the postback.
			//
			// In order to get the postback handling (which gives us reloading of control values from the posted form) the Form must have an ID and/or runat=server (not sure which).
			//
			// The Application object and its add-in interface objects (one of which owns the DBClient) all need to be saved in client-side state as live CLR objects
			// (not as some sort of streamed data to allow re-creation).
			//
			// This is (in theory) the identity of the user making the web request. On initial entry to the app (when we open the DB) we should authenticate this
			// against the Users table, and prime the Permissions. Do we need to worry about some other user usurping the Session?

			// TODO: TblPage should probably put this at the bottom of every page.
			IApplicationWithSingleDatabaseConnection dbAppObj = MBWApplication.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>();
			ConnectioInfo.InnerText = Strings.Format(KB.K("Organization '{0}', Connection: {1}, Windows user '{2}', App object id {3}"),
				dbAppObj.OrganizationName,
				dbAppObj.Session.ConnectionInfo.DisplayName,
				Thinkage.Libraries.Application.Instance.UserName,
				MBWApplication.Instance.Id);
			HtmlGenericControl itemList = BuildItemList(new ControlPanelLayout().MainContents);
			if (itemList != null)
				OutputContainer.Controls.Add(itemList);
		}
		private HtmlGenericControl BuildItemList(MenuDef[] mdef) {
			HtmlGenericControl result = null;
			// TODO: Style these so they have no bullets and are not so spaced-out vertically
			// TODO: Tbls with no columns (only one row, such as db status, @Request settings, all defaults etc) should go directly to View mode on the record; we have
			// no easy way to detect this, though. Perhaps the Browser should transfer on its own when it finds such cases.
			if (mdef != null)
				foreach (MenuDef item in mdef) {
					string itemHRef = null;
					HtmlGenericControl subcontents = BuildItemList(item.subMenu);
					BrowseMenuDef brItem = item as BrowseMenuDef;
					if (brItem != null)
						itemHRef = Strings.IFormat("BrowseTable.aspx?Tbl={0}", MBWApplication.Instance.GetTblId(brItem.TblCreator.Tbl));

					if (itemHRef == null && subcontents == null)
						continue;
					HtmlGenericControl itemContents = new HtmlGenericControl("li");
					HtmlGenericControl itemEntry = new HtmlGenericControl("p");
					itemEntry.InnerText = item.name.Translate();
					if (itemHRef != null) {
						// wrap an anchor around itemEntry
						HtmlAnchor link = new HtmlAnchor();
						link.Controls.Add(itemEntry);
						link.HRef = HttpUtility.HtmlEncode(itemHRef);
						link.Target = "_blank";
						itemContents.Controls.Add(link);
					}
					else
						itemContents.Controls.Add(itemEntry);
					if (subcontents != null)
						itemContents.Controls.Add(subcontents);
					if (result == null)
						result = new HtmlGenericControl("ul");
					result.Controls.Add(itemContents);
				}
			return result;
		}
	}
}
