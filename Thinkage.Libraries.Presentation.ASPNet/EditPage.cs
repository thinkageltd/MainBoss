using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI.HtmlControls;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBAccess;

// TODO: Use <caption> (first element within <table>) to label nested groups and the main field group
// TODO: Use <thead> instead of <tr> for the header row of a multi-column group
// TODO: Use explicit <tbody> around <tr> collection
namespace Thinkage.Libraries.Presentation.ASPNet {
	public abstract class EditPage : TblPage {
		// TODO: A method to add a command button with a new URL  and options for a new Tbl, same Tbl, and passing the current ID or not.

		protected string ModeName(EdtMode mode) {
			switch (mode) {
			case EdtMode.Clone:
				return KB.T("record cloning").Translate();
			case EdtMode.Edit:
				return KB.T("record editing").Translate();
			case EdtMode.View:
				return KB.T("record viewing").Translate();
			case EdtMode.ViewDeleted:
				return KB.T("deleted record viewing").Translate();
			case EdtMode.ViewDefault:
				return KB.T("default values viewing").Translate();
			case EdtMode.EditDefault:
				// TODO: Need a 'default record viewing' mode??
				return KB.T("default values editing").Translate();
			case EdtMode.New:
				return KB.T("new record creation").Translate();
			case EdtMode.UnDelete:
				return KB.T("record recovery").Translate();
			}
			return KB.I("");
		}
		#region Page processing sequence
		#region - OnInit
		protected override void OnInit(EventArgs e) {
			base.OnInit(e);
			EnableViewState = false;
			// Get the Mode from the query parameters.
			EdtMode Mode = GetModeFromQuery();

			// Verify that we can use the TBl. It must have an ETbl that allows EdtModeDotView
			ETbl ETblNode = ETbl.Find(TInfo);
			if (ETblNode == null || !ETblNode.AllowedInitialMode(Mode))
				// TODO: Add context information, maybe there is some sort of way for TblPage to decorate unhandled exceptions.
				throw new GeneralException(KB.K("Form layout information does not support {0}"), ModeName(Mode));

			// Get the ID from the query parameters if the mode requires an ID.
			object[] recordIds = null;
			switch (Mode) {
			case EdtMode.Edit:
			case EdtMode.View:
			case EdtMode.Delete:
			case EdtMode.UnDelete:
			case EdtMode.ViewDeleted:
			case EdtMode.Clone:
				string idText = Request.QueryString["ID"];
				if (string.IsNullOrEmpty(idText))
					throw new GeneralException(KB.K("Query parameter 'ID' is required for {0} but is missing or empty"), ModeName(Mode));
				TypeInfo.TypeInfo idType = TInfo.Schema.InternalIdColumn.EffectiveType;
				TypeEditTextHandler idParser = idType.GetTypeEditTextHandler(System.Globalization.CultureInfo.InvariantCulture);
				if (idParser != null)
					recordIds = new object[] { idParser.ParseEditText(idText) };
				else if (idType is TypeInfo.IdTypeInfo)
					recordIds = new object[] { new Guid(idText) };
				else
					throw new GeneralException(KB.K("No parser available for type {0} to parse the ID value"), idType.ToTypeSpecification());
				break;
			}

			// Read the Inits supplied in the query, if any. This has to happen after creating the dataSet so we can extract its Schema.
			string[] textInits = Request.QueryString.GetValues("I");
			// TODO: The following will fail if TInfo.Schema is null, but then there should be no path-based Inits
			DBI_Database dbSchema = TInfo.Schema.Database;
			List<TblActionNode> urlInits = new List<TblActionNode>();
			if (textInits != null)
				foreach (string s in textInits)
					urlInits.Add(GetInitFromText(s, dbSchema));

			// TODO: Read the following from the URL
			bool[] subsequentModeRestrictions = new bool[(int)EdtMode.Max];
			for (int i = (int)EdtMode.Max; --i >= 0; )
				subsequentModeRestrictions[i] = true;

			// Create an HTML form that contains the entire body and calls us back with the same URL.
			HtmlForm form = new HtmlForm();
			Body.Controls.Add(form);
			form.Action = Request.RawUrl;
			EditorControlObject = new EditorControl(TblApplication.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().Session, TInfo, Mode, recordIds, subsequentModeRestrictions, urlInits);
			form.Controls.Add(EditorControlObject);
		}
		private EdtMode GetModeFromQuery() {
			// Get the Mode from the query parameters.
			string modeText = Request.QueryString["M"];
			for (int i = 0; i < ModeCodes.Length; i++)
				if (ModeCodes[i] == modeText)
					return ModeValues[i];
			throw new GeneralException(KB.K("Query parameter 'M' is missing or invalid"));
		}
		#endregion
		// No Page_Load handling, at that point we are in a limbo state where the controls have their new values but have not notified anyone.
		// After Page_Load and before PreRender, control change events occur (and our binding handles changes) and command button if any are
		// executed... some of these redirect to a new page. If not redirected we fall into our PreRender and Render.
		#region Destruction
		public override void Dispose() {
			// TODO: It is not clear how disposal works in the Web environment. One would expect that disposing of a container would automatically dispose of the contents as well,
			// but I haven't found the code path that does this. So for now we dispose of the EditorControlObject ourselves.
			if (EditorControlObject != null) {
				EditorControlObject.Dispose();
				EditorControlObject = null;
			}
			base.Dispose();
		}
		#endregion
		#endregion
		private EditorControl EditorControlObject;
		#region Static methods to facilitate calling an EditPage derivation
		private enum TypeMark { Null, Byte, SByte, NegSByte, UShort, Short, NegShort, UInt, Int, NegInt, ULong, Long, NegLong, TrueBool, FalseBool, String, ByteArray, Guid }
		private static TypeInfo.StringTypeInfo DummyStringType = new Thinkage.Libraries.TypeInfo.StringTypeInfo(0, 1, 0, false, false, false);
		#region Putting data to a stream
		private static void AddValueToStream(List<Byte> output, object value) {
			// This adds a c# value (of a selected subset of types) to the stream in a manner that it can be read back context-free.
			if (value == null)
				AddULongToStream(output, (ulong)TypeMark.Null);
			else if (value is Byte) {
				AddULongToStream(output, (ulong)TypeMark.Byte);
				AddULongToStream(output, (ulong)(Byte)value);
			}
			else if (value is SByte) {
				SByte v = (SByte)value;
				if (v >= 0) {
					AddULongToStream(output, (ulong)TypeMark.SByte);
					AddULongToStream(output, (ulong)v);
				}
				else {
					AddULongToStream(output, (ulong)TypeMark.NegSByte);
					AddULongToStream(output, (ulong)unchecked(-v));
				}
			}
			else if (value is UInt16) {
				AddULongToStream(output, (ulong)TypeMark.UShort);
				AddULongToStream(output, (ulong)(UInt16)value);
			}
			else if (value is Int16) {
				Int16 v = (Int16)value;
				if (v >= 0) {
					AddULongToStream(output, (ulong)TypeMark.Short);
					AddULongToStream(output, (ulong)v);
				}
				else {
					AddULongToStream(output, (ulong)TypeMark.NegShort);
					AddULongToStream(output, (ulong)unchecked(-v));
				}
			}
			else if (value is UInt32) {
				AddULongToStream(output, (ulong)TypeMark.UInt);
				AddULongToStream(output, (ulong)(UInt32)value);
			}
			else if (value is Int32) {
				Int32 v = (Int32)value;
				if (v >= 0) {
					AddULongToStream(output, (ulong)TypeMark.Int);
					AddULongToStream(output, (ulong)v);
				}
				else {
					AddULongToStream(output, (ulong)TypeMark.NegInt);
					AddULongToStream(output, (ulong)unchecked(-v));
				}
			}
			else if (value is UInt64) {
				AddULongToStream(output, (ulong)TypeMark.ULong);
				AddULongToStream(output, (ulong)(UInt64)value);
			}
			else if (value is Int64) {
				Int64 v = (Int64)value;
				if (v >= 0) {
					AddULongToStream(output, (ulong)TypeMark.Long);
					AddULongToStream(output, (ulong)v);
				}
				else {
					AddULongToStream(output, (ulong)TypeMark.NegLong);
					AddULongToStream(output, (ulong)unchecked(-v));
				}
			}
			else if (value is Boolean)
				AddULongToStream(output, (ulong)((Boolean)value ? TypeMark.TrueBool : TypeMark.FalseBool));
			else if (value is string) {
				char[] charArray = ((string)value).ToCharArray();
				System.Text.Encoder e = System.Text.Encoding.UTF8.GetEncoder();
				int byteCount = e.GetByteCount(charArray, 0, charArray.Length, true);
				Byte[] bytes = new Byte[byteCount];
				e.GetBytes(charArray, 0, charArray.Length, bytes, 0, true);
				AddULongToStream(output, (ulong)TypeMark.String);
				AddByteArrayToStream(output, bytes);
			}
			else if (value is Byte[]) {
				AddULongToStream(output, (ulong)TypeMark.ByteArray);
				AddByteArrayToStream(output, (Byte[])value);
			}
			else if (value is Guid) {
				AddULongToStream(output, (ulong)TypeMark.Guid);
				output.AddRange(((Guid)value).ToByteArray());
			}
		}
		private static void AddByteArrayToStream(List<Byte> output, Byte[] input) {
			AddULongToStream(output, (ulong)input.Length);
			output.AddRange(input);
		}
		private static void AddULongToStream(List<Byte> output, ulong value) {
			for (; ; ) {
				Byte bits = unchecked((Byte)(value & (Byte.MaxValue >> 1)));
				value >>= 7;	// Ugh!
				if (value != 0)
					output.Add((Byte)(bits | ((Byte.MaxValue >> 1) + 1)));
				else {
					output.Add(bits);
					return;
				}
			}
		}
		#endregion
		#region Reading data from a stream
		private static object GetValueFromStream(Byte[] input, ref int index) {
			TypeMark m = (TypeMark)GetULongFromStream(input, ref index);
			Byte[] bytes;
			Char[] chars;
			switch (m) {
			case TypeMark.Null:
				return null;
			case TypeMark.Byte:
				return (Byte)GetULongFromStream(input, ref index);
			case TypeMark.SByte:
				return (SByte)GetULongFromStream(input, ref index);
			case TypeMark.NegSByte:
				return (SByte)unchecked(-(long)GetULongFromStream(input, ref index));
			case TypeMark.UShort:
				return (UInt16)GetULongFromStream(input, ref index);
			case TypeMark.Short:
				return (Int16)GetULongFromStream(input, ref index);
			case TypeMark.NegShort:
				return (Int16)unchecked(-(long)GetULongFromStream(input, ref index));
			case TypeMark.UInt:
				return (UInt32)GetULongFromStream(input, ref index);
			case TypeMark.Int:
				return (Int32)GetULongFromStream(input, ref index);
			case TypeMark.NegInt:
				return (Int32)unchecked(-(long)GetULongFromStream(input, ref index));
			case TypeMark.ULong:
				return (UInt64)GetULongFromStream(input, ref index);
			case TypeMark.Long:
				return (Int64)GetULongFromStream(input, ref index);
			case TypeMark.NegLong:
				return (Int64)unchecked(-(long)GetULongFromStream(input, ref index));
			case TypeMark.TrueBool:
				return true;
			case TypeMark.FalseBool:
				return false;
			case TypeMark.String:
				bytes = GetByteArrayFromStream(input, ref index);
				System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
				chars = new char[d.GetCharCount(bytes, 0, bytes.Length, true)];
				d.GetChars(bytes, 0, bytes.Length, chars, 0);
				return new string(chars);
			case TypeMark.ByteArray:
				return GetByteArrayFromStream(input, ref index);
			case TypeMark.Guid:
				bytes = new Byte[16];
				Array.Copy(input, index, bytes, 0, 16);
				index += 16;
				return new Guid(bytes);
			default:
				throw new NotImplementedException();
			}
		}
		private static Byte[] GetByteArrayFromStream(Byte[] input, ref int index) {
			ulong lenval = GetULongFromStream(input, ref index);
			Byte[] result = new Byte[lenval];
			Array.Copy(input, index, result, 0, (long)lenval);
			index += (int)lenval;
			return result;
		}
		private static ulong GetULongFromStream(Byte[] input, ref int index) {
			ulong result = 0;
			int shift = 0;
			for (; ; ) {
				ulong bits = input[index++];
				result |= (bits & (Byte.MaxValue >> 1)) << shift;
				shift += 7;
				if ((bits & ~(ulong)(Byte.MaxValue >> 1)) == 0)
					return result;
			}
		}
		public static Init GetInitFromText(string text, Thinkage.Libraries.DBILibrary.DBI_Database schema) {
			Byte[] stream = Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/'));
			int sx;
			List<EdtMode> onLoadModes = new List<EdtMode>();
			List<EdtMode> continuousModes = new List<EdtMode>();
			Thinkage.Libraries.DBILibrary.DBI_Path path = null;
			int rs;
			ulong code;

			// Decode the activity (2 bytes)
			UInt16 activity = unchecked((UInt16)(stream[0] | (stream[1] << 8)));
			sx = 2;
			for (int m = (int)EdtMode.Max; --m >= 0; ) {
				switch ((TblActionNode.Activity)((activity >> (2 * m)) & 3)) {
				case TblActionNode.Activity.Continuous:
					continuousModes.Add((EdtMode)m);
					break;
				case TblActionNode.Activity.OnLoad:
					onLoadModes.Add((EdtMode)m);
					break;
				}
			}

			// Decode the target
			EditInitTarget target;
			code = GetULongFromStream(stream, ref sx);
			switch (code) {
			case 1:
			case 2:
				rs = (int)GetULongFromStream(stream, ref sx);
				string pathText = (string)GetValueFromStream(stream, ref sx);
				int ix = pathText.IndexOf('.');
				if (ix < 0)
					throw new NotImplementedException();
				int start;
				Thinkage.Libraries.DBILibrary.DBI_Table t = schema.Tables[pathText.Substring(0, ix)];
				Thinkage.Libraries.DBILibrary.DBI_Column c;
				if (t == null)
					throw new NotImplementedException();
				if (pathText.Substring(ix, 3) != ".F.")
					throw new NotImplementedException();
				start = ix + 3;
				for (; ; ) {
					ix = pathText.IndexOf('.', start);
					if (ix <= start)
						ix = pathText.Length;
					c = t.Columns[pathText.Substring(start, ix - start)];
					if (c == null)
						throw new NotImplementedException();
					path = new Thinkage.Libraries.DBILibrary.DBI_Path(path.PathToReferencedRow, c);
					// At this point we should either have end of string or "."
					if (ix == pathText.Length)
						break;
					// The only other choice is ".F."
					if (pathText.Substring(ix, 3) != ".F.")
						throw new NotImplementedException();
					if (c.ConstrainedBy == null)
						throw new NotImplementedException();
					start = ix + 3;
					t = c.ConstrainedBy.Table;
				}
				target = code == 1 ? (EditInitTarget)new PathOrFilterTarget(path, rs) : (EditInitTarget)new PathTarget(path, rs);
				break;
			default:
				throw new NotImplementedException();
			}
			// Decode the source
			EditorInitValue source = new ConstantValue(GetValueFromStream(stream, ref sx));
			return Presentation.Init.New(target, source, null, TblActionNode.Activity.Disabled, Presentation.Init.SelectiveActivity(TblActionNode.Activity.OnLoad, onLoadModes.ToArray()), Presentation.Init.SelectiveActivity(TblActionNode.Activity.Continuous, continuousModes.ToArray()));
		}
		#endregion
		/// <summary>
		/// Return the name of the .aspx page to be called to edit the given Tbl. This is built by taking the last identifier
		/// from the EditControl derivation using by the local app and replacing the word "Control" with "Record"
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static string CustomPageName(Tbl t) {
			// TODO: We need our own way of specifying and querying custom editor class information.
			return KB.I("EditorControl");
		}
		private static StringBuilder BuildBasicHRef(Tbl t, List<TblActionNode> initList) {
			StringBuilder sb = new StringBuilder(CustomPageName(t));
			sb.Append(".aspx?Tbl=");
			sb.Append(TblApplication.Instance.GetTblId(t));
			// Add the Inits to the query.
			if (initList != null)
				foreach (TblActionNode n in initList) {
					Init i = n as Init;
					if (i == null)
						throw new NotImplementedException("Editor being called with non-Init TblActionNode");
					// Handle ValueMapper
					if (i.ValueMapper != null)
						throw new NotImplementedException("Editor being called with Init having a ValueMapper delegate");
					// Handle Predicate
					if (i.Predicate != null)
						throw new NotImplementedException("Editor being called with Init having a Predicate delegate");
					// Handle EnablerValue
					if (i.EnablerValue != null)
						throw new NotImplementedException("Editor being called with Init having an EnablerValue");
					// Handle Activity
					UInt16 activity = 0;
					// The Activity is not directly available so we query it for each of the EdtModes and build a bit mask of values.
					for (int m = (int)EdtMode.Max; --m >= 0; ) {
						TblActionNode.Activity a = i.GetActivity((EdtMode)m);
						activity |= (UInt16)((UInt32)a << (2 * m));
					}
					List<Byte> encodedInit = new List<byte>();
					encodedInit.Add((Byte)(activity & Byte.MaxValue));
					encodedInit.Add((Byte)(activity >> 8));

					// Handle InitTarget
					ControlTarget ct = i.InitTarget as ControlTarget;
					InSubBrowserTarget isbt = i.InitTarget as InSubBrowserTarget;
					ControlReadonlyTarget crt = i.InitTarget as ControlReadonlyTarget;
					PathOrFilterTarget pft = i.InitTarget as PathOrFilterTarget;
					PathTarget pt = i.InitTarget as PathTarget;
					// TODO: Handle all the Id-based inits by giving the app class a registry for Id values similar to the one for Tbl's.
					// ControlTarget
					if (ct != null) {
						throw new NotImplementedException("Editor being called with Init ControlTarget that uses an Id");
					}
					// InSubBrowserTarget
					else if (isbt != null) {
						throw new NotImplementedException("Editor being called with Init InSubBrowserTarget that uses an Id");
					}
					// ControlReadonlyTarget
					else if (crt != null) {
						throw new NotImplementedException("Editor being called with Init ControlReadonlyTarget that uses an Id");
					}
					// PathOrFilterTarget
					else if (pft != null) {
						AddULongToStream(encodedInit, 1);
						AddULongToStream(encodedInit, (ulong)pft.RecordSet);
						AddValueToStream(encodedInit, pft.PathInEditBuffer.ToString());
					}
					// PathTarget
					else if (pt != null) {
						AddULongToStream(encodedInit, 2);
						AddULongToStream(encodedInit, (ulong)pt.RecordSet);
						AddValueToStream(encodedInit, pt.PathInEditBuffer.ToString());
					}

					// Handle InitValue
					// BrowseControl always resolves the values it has to ConstantValue so this is the only case we must handle.
					ConstantValue cv = i.InitValue as ConstantValue;
					if (cv == null)
						throw new NotImplementedException("Editor being called with Init source that is not a constant");
					AddValueToStream(encodedInit, cv.Value);
					// ControlUncheckedValue
					// ControlValue
					// InSubBrowserValue
					// PathValue
					// SequenceCountValue
					// UserIDValue
					// VariableValue
					// EditorCalculatedInitValue

					// Encode the Init as I=(base64 data)
					sb.Append("&I=");
					// Base64 can contain + and / which will turn into blanks or cause a syntactically invalid URL if used verbatim, or
					// will expand to %xx if url-encoded. Instead we replace them with - and _ and use the result verbatim. It may end with one or two
					// equal-signs, but these do not interfere with parsing the query.
					sb.Append(Convert.ToBase64String(encodedInit.ToArray()).Replace('+', '-').Replace('/', '_'));
				}
			return sb;
		}
		private static void AddIdParam(StringBuilder sb, Tbl t, object editId) {
			sb.Append("&ID=");
			sb.Append(HttpUtility.UrlEncode(t.Schema.InternalIdColumn.EffectiveType.GetTypeFormatter(Thinkage.Libraries.Application.InstanceCultureInfo).Format(editId)));
		}
		private static void AddModeParam(StringBuilder sb, EdtMode mode) {
			for (int i = 0; i < ModeValues.Length; i++)
				if (ModeValues[i] == mode) {
					sb.Append("&M=");
					sb.Append(ModeCodes[i]);
					return;
				}
			throw new ArgumentOutOfRangeException("mode");
		}
		public static string BuildEditHRef(Tbl t, EdtMode mode, object editId, List<TblActionNode> initList) {
			// for Edit, Clone, Delete, UnDelete, View, ViewDeleted modes
			StringBuilder sb = BuildBasicHRef(t, initList);
			AddIdParam(sb, t, editId);
			AddModeParam(sb, mode);
			return sb.ToString();
		}
		public static string BuildEditHRef(Tbl t, EdtMode mode, List<TblActionNode> initList) {
			// for EditDefaults, New, ViewDefaults modes
			StringBuilder sb = BuildBasicHRef(t, initList);
			AddModeParam(sb, mode);
			return sb.ToString();
		}
		private static EdtMode[] ModeValues = new EdtMode[] {
			EdtMode.Edit,
			EdtMode.EditDefault,
			EdtMode.New,
			EdtMode.Clone,
			EdtMode.Delete,
			EdtMode.UnDelete,
			EdtMode.View,
			EdtMode.ViewDefault,
			EdtMode.ViewDeleted
		};
		private static string[] ModeCodes = new string[] {
			"E",
			"D",
			"N",
			"C",
			"X",
			"U",
			"V",
			"a",
			"b"
		};
		#endregion
	}
}
