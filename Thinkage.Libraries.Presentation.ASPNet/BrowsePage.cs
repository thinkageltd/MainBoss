using System;
using System.Web.UI.HtmlControls;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBAccess;

// TODO: This generates __VIEWSTATE and some other state junk that I want to avoid having. Investigate why it is being created.
// The length of the __VIEWSTATE seems roughly proportional to the number of records in the browse data; it would appear that adding all those
// <tr> elements for the list data is causing them to appear.

namespace Thinkage.Libraries.Presentation.ASPNet {
	public abstract class BrowsePage : TblPage {
		protected BrowsePage() {
		}

		// There are several ways to run the contextual commands. The command button itself could supply all the inits encoded in its URL, but we would
		// have to verify these anyway to see if the user has not synthesized a URL that would violate the business models, so we might as well just have
		// all the browser commands be callbacks that just supply the ID of the selected record.
		// As a result the form contains only one control with a value, namely the ID of the selected record.
		// You also get, of course, the "value" for the button that caused the GET/POST operation.
		// The GET/POST operation also must supply all the information originally used to get the browser. [TODO: Can we reliably get this from the Referer if we
		//		always use GET (query) syntax to initially call the browser?]
		// The postback reply will either be the same browser
		// with an error message included as to why the command did not work, or a client-side forwarding request to open the new form called by the command
		// in a new browser window [TODO: Can this be done? i.e. can the server decide if the result of the reuqest is displayed in the same or a new window?]
		// I suppose instead that we could always give a new window which contains either the error message or the requested form.
		//
		// There are several ways to run the "list".
		// One way is to use a standard html list control (<select> element with style to render it in list form). This has a couple of disadvantages, though:
		// 1 - It cannot represent multiple-column data unless you force a fixed-pitch font and use spacing to align text, and even then you get no column headers
		// 2 - It cannot represent "no selection"
		//
		// Another way is to use a table to represent database rows. A <thead> of <th> elements contains the headers. The <tr> element that represents
		// the current selection is given a special style by client-side code, which also sets the ID into a hidden control. The <tr> element also contains
		// its ID in a hidden column, or perhaps as the ID of the <td> element itself, depending on how flexible DOM access is. The entire <tr> would be in
		// an <a> (or the individual cells if necessary) whose href is a script call to "selectRow(<row id>)" or some such thing. The initial selection would
		// be set by a direct script call to selectRow(hidden control's value) in the appropriate page-load event. Note that it happens that a GUID is a valid
		// node 'id' attribute value as long as you ensure it starts with an alphabetic value.
		// The disadvantages of this are:
		// 1 - It relies on client-side scripting
		// 2 - If the entire row can't be an <A> element and instead each <td> contains <a> the html becomes bulky and this may make it difficult to select a row whose
		//		contents are all null.
		protected override void OnInit(EventArgs e) {
			base.OnInit(e);
			// The following limits but does not entirely eliminate the __VIEWSTATE data in the form. It at least keeps it to more or less constant length.
			EnableViewState = false;
			// Verify that we can use the TBl. It must have a BTbl. Note that the BrowseLogic has an assertion to this effect but
			// because users can synthesize bogus URL's we need a solid check here.
			if (BTbl.Find(TInfo) == null)
				// TODO: Add context information, maybe there is some sort of way for TblPage to decorate unhandled exceptions.
				throw new GeneralException(KB.K("Form layout information does not support browsing"));

			SetTitles(string.Format("View {0} records{1}", TInfo.Identification, Request.QueryString["Q"]));

			// TODO: If the browser has no records its value will be null even though the Id column type forbids this.
			ListControl = new BrowserControl(TblApplication.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session, false, TInfo, BrowseLogic.BrowseOptions.NoSecondaries, BrowserControl.BrowserModes.Browse, TInfo.Schema == null ? Libraries.TypeInfo.NullTypeInfo.Universe : TInfo.Schema.InternalIdColumn.EffectiveType.UnionCompatible(Libraries.TypeInfo.NullTypeInfo.Universe), "a");

			// Add the PathValue filters specified in the query parameters.
			string[] values;
			values = Request.QueryString.GetValues("PF");
			if (values != null)
				foreach (string s in values)
					AddPathValueFilter(s, false);
			values = Request.QueryString.GetValues("PT");
			if (values != null)
				foreach (string s in values)
					AddPathValueFilter(s, true);

			// We wrap this SelectionControl and all the command buttons into a <form> element. Unfotunately it is the <form>
			// element's target attribute that determines if the result of the submit goes in the same window or a new one. As a result
			// it is difficult to make some commands (Refresh, Delete) return results in the same window, and others (View, New) return
			// results in a new window. One method is to give either set of buttons an onclick event that overrides the normal form
			// submission, and explicitly submits the form with the desired target. Another method would be to have two forms
			// with a SelectionControl in each, both of which selectRow would have to set.
			// Note also that we don't need a Refresh command, the browser refresh should suffice with a little tweaking of the client onload event
			// for the case where the current selection has vanished from the list.
			// The Delete command can be a special case of the Edit form, which shows you the record you are about to delete, or the View form
			// could always include a Delete or UnDelete button.

			// All the buttons in the form call the same URL; we just get different Click events firing, some of which will sidetrack us
			// to a completely different form (using client-side redirection: Response.Redirect(newHRef)). Ideally we should always redirect
			// to a new form, even when an error occurs; otherwise the user ends up with two screens of essentially the same browser.
			// TODO: A page should "know" if it is being called in a new form or not. Any page called in the same form should do so with GET only,
			// and no POSTDATA, so that the browser BACK button behaves. Conversely, any pages called with POST should be in a new browser window,
			// and should always respond by redirecting again so there is no POST in the browser history.
			// If the callback form uses GET it appears to cause the params in the "action" to be *replaced* by the form values, rather than added to,
			// at least when using Firefox. From reading the HTTP documentation this stikes me as incorrect; they state that the form data (in query-string format)
			// is *appended* to the URL in the form's 'action' attribute with a '?' in between. Having typed this I now realize that this would yield a query like
			// BrowseTable.aspx?T=123?Control1=its_data&Control2=its_data
			// i.e. because the initial delimiter for the query is a '?' and separators for subsequent ones are '&' the 'T' parameter value would be
			// interpreted to be '123?Control1=its_data' and Control1's value would be lost.
			// However, inspection of the query reveals that firefox has indeed stripped off and replaced the original query parameters.
			CallbackForm = new HtmlForm();
			Body.Controls.Add(CallbackForm);
			CallbackForm.Action = Request.RawUrl;
			CallbackForm.Target = "_blank";
			CallbackForm.Controls.Add(ListControl);
		}
		private BrowserControl ListControl;
		private HtmlForm CallbackForm = null;
		private void AddPathValueFilter(string encoded, bool resultIfValueNull) {
			// The string should be of the form Table.F.Field.F.Field...=constant.
			// First get the table name.
			int ix = encoded.IndexOf('.');
			int start;
			if (ix < 1)
				return;
			DBI_Table t = TInfo.Schema.Database.Tables[encoded.Substring(0, ix)];
			if (t == null)
				return;
			if (encoded.Substring(ix, 3) != ".F.")
				return;
			start = ix + 3;
			DBI_Column c;
			DBI_Path path = null;
			for (; ; ) {
				ix = encoded.IndexOfAny(new char[] { '.', '=' }, start);
				if (ix <= start)
					return;
				c = t.Columns[encoded.Substring(start, ix - start)];
				if (c == null)
					return;
				path = new DBI_Path(path == null ? new DBI_PathToRow(t) : path.PathToReferencedRow, c);
				// At this point we should either have an '=' or ".
				if (encoded[ix] == '=')
					break;
				// The only other choice is ".F."
				if (encoded.Substring(ix, 3) != ".F.")
					return;
				if (c.ConstrainedBy == null)
					return;
				start = ix + 3;
				t = c.ConstrainedBy.Table;
			}
			TypeInfo.TypeInfo tinfo = c.EffectiveType;
			if (tinfo is LinkedTypeInfo)
				tinfo = ((LinkedTypeInfo)tinfo).BaseType;
			object constantValue;
			encoded = encoded.Substring(ix + 1);
			if (encoded.Length == 0)
				constantValue = null;
			else if (tinfo is IdTypeInfo)
				constantValue = new Guid(encoded);
			else
				constantValue = tinfo.GetTypeEditTextHandler(Thinkage.Libraries.Application.InstanceCultureInfo).ParseEditText(encoded);

			BrowseLogic.ParameterizedFilter f = new BrowseLogic.PathValueFilter(path, resultIfValueNull);
			f.SetValue(constantValue);
			ListControl.BrowserLogic.AddGlobalFilter(f);
		}
	}
}