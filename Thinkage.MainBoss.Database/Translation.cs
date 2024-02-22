using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// Keybuilder for the DatabaseLayout. Provides some internal state change operations in the context of WinControls
	/// </summary>
	internal class KB : Thinkage.Libraries.Translation.KB {
		static KB() {
			DeclareAssemblyProvidesTranslationsUsingResource(K(null), System.Reflection.Assembly.GetExecutingAssembly());
		}
		static readonly KB Instance = new KB();
		public static SimpleKey K([Context(Level = 1)] string s) {
			return Instance.BuildKey(s);
		}
	}
	/// <summary>
	/// Public to allow for creation/upgrading of ServiceWorker UserTranslationMessage keys
	/// </summary>
	public class UK : GeneralKeyBuilder {
		const string MainBossServiceContext = "MainBossService";

		static readonly UK Instance = new UK();
		protected UK() {
		}
		protected override ContextReference GetContext() {
			return ContextReference.New(MainBossServiceContext);
		}
		public static Key K([Context(MainBossServiceContext)]string s) {
			return Instance.BuildKey(s);
		}
	}
	public class RequestClosePreferenceK : GeneralKeyBuilder {
		public const string RequestClosePreference = "RequestClosePreference";

		static readonly RequestClosePreferenceK Instance = new RequestClosePreferenceK();
		protected RequestClosePreferenceK() {
		}
		protected override ContextReference GetContext() {
			return ContextReference.New(RequestClosePreference);
		}
		public static SimpleKey K([Context(RequestClosePreference)]string s) {
			return Instance.BuildKey(s);
		}
	}

	/// <summary>
	/// State Codes and Descs are segregated from regular controls for translation context assistance
	/// Note that for historical reasons these use the context (StateCode) rather than just (State)
	/// </summary>
	public class StateContext : Thinkage.Libraries.Translation.KB {
		private static readonly StateContext Instance = new StateContext();
		private static SimpleKey K([Context("StateCode", Level = 1)] string s) {
			return Instance.BuildKey(s);
		}
		internal static SimpleKey DescK([Context("StateCode", Level = 1)] string s) {
			return Instance.BuildKey(s);
		}
		public static readonly SimpleKey NewCode = K("New");
		public static readonly SimpleKey DraftCode = K("Draft");
		public static readonly SimpleKey InProgressCode = K("In Progress");
		public static readonly SimpleKey OpenCode = K("Open");
		public static readonly SimpleKey IssuedCode = K("Issued");
		public static readonly SimpleKey ClosedCode = K("Closed");
		public static readonly SimpleKey VoidedCode = K("Voided");
		// The following are pseudo-states that do not appear in the State tables but are subclassifictions of "real" states used
		// in certain places.
		// Late means it should be open/issued/in progress but isn't
		public static readonly SimpleKey LateCode = K("Start Late");
		public static readonly SimpleKey EarlyCode = K("Start Early");
		// Overdue means it should be closed but is still open/issued/in progress (differs from End Late in that the work order is not yet closed)
		public static readonly SimpleKey OverdueCode = K("Overdue");
		// Ended Late means it closed but after the estimated work end date
		public static readonly SimpleKey EndLateCode = K("End Late");
		// Ended Early means it closed but before the estimated work end date
		public static readonly SimpleKey EndEarlyCode = K("End Early");
	}
	/// <summary>
	/// A direct ITranslator that looks up translations directly in the UserMessageTranslation table
	/// </summary>
	// marked sealed to eliminate CA1063 code analysis warnings about non-standard model of implementing Dispose
	public sealed class UserMessageTranslator : ITranslator, IDisposable {
		private dsMB umtDs;
		private readonly SortingPositioner Positioner;
		private readonly PopulatingCursorManager Cursor;
		// DataSources
		private readonly Thinkage.Libraries.DataFlow.Source LCID;
		private readonly Thinkage.Libraries.DataFlow.Source Translation;
		private readonly Thinkage.Libraries.DataFlow.Source Context;
		private readonly Thinkage.Libraries.DataFlow.Source Key;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		// bufferManager Dispose responsibility of CursorManager
		public UserMessageTranslator(Thinkage.Libraries.DBAccess.DBClient db) {
			Libraries.DBAccess.DBVersionHandler vh = MBUpgrader.UpgradeInformation.CreateCurrentVersionHandler(db);
			if (vh.CurrentVersion >= new Version(1, 0, 10, 6)) {
				umtDs = new dsMB(db);
				DynamicBufferManager bufferManager = new DynamicBufferManager(db, dsMB.Schema, false);
				Cursor = new PopulatingCursorManager(bufferManager, dsMB.Schema.T.UserMessageTranslation, true, null);
				LCID = Cursor.GetPathSource(dsMB.Path.T.UserMessageTranslation.F.LanguageLCID);
				Translation = Cursor.GetPathSource(dsMB.Path.T.UserMessageTranslation.F.Translation);
				Key = Cursor.GetPathSource(dsMB.Path.T.UserMessageTranslation.F.UserMessageKeyID.F.Key);
				Context = Cursor.GetPathSource(dsMB.Path.T.UserMessageTranslation.F.UserMessageKeyID.F.Context);

				Positioner = new SortingPositioner(Cursor, new KeyProviderFromSources(Context, Key, LCID));
				Cursor.SetAllKeepUpdated(true);
			}
		}

		#region ITranslator Members
		private bool FindTranslation(string context, string localKey, int lcid, out string translated) {
			if (Positioner != null) {
				Position pos = Positioner.PositionOfFirstGreaterOrEqual(new object[] { context, localKey, lcid });
				if (!pos.IsEnd) {
					Positioner.CurrentPosition = pos;
					if (Key.TypeInfo.GenericEquals(Key.GetValue(), localKey)
						&& Context.TypeInfo.GenericEquals(Context.GetValue(), context)
						&& LCID.TypeInfo.GenericEquals(LCID.GetValue(), lcid)) {
						translated = (string)Translation.TypeInfo.GenericAsNativeType(Translation.GetValue(), typeof(string));
						return true;
					}
				}
			}
			translated = null;
			return false;
		}
		public override string Translate(string context, string localKey, CultureInfo culture, string[] implicitTags, ref int penalty, Set<string> qualifiers) {
			do
				if (FindTranslation(context, localKey, culture.LCID, out string translated)) {
					if (implicitTags != null) {
						var tset = new Set<string>(implicitTags);
						penalty = 1000 * qualifiers.Count + tset.Count;
						tset.IntersectWith(qualifiers);
						penalty -= 1001 * tset.Count;
					}
					else
						penalty = 1000 * qualifiers.Count;
					return translated;
				}
			while ((culture = GetFallbackCultureInfo(culture)) != null);
			// none found
			penalty = int.MaxValue;
			return null;
		}
		public override Key.ITranslationEnumerator AllTranslations(string context, string localKey, CultureInfo ci, Set<string> qualifiers) {
			int penalty = int.MaxValue;
			string translated = Translate(context, localKey, ci, null, ref penalty, qualifiers);
			if (string.IsNullOrEmpty(translated))
				return new Key.EmptyTranslationEnumerable();
			else
				return new Key.SingleTranslationEnumerable(new Key.Translation(translated, penalty));
		}
		#endregion
		#region IDisposable Members
		public void Dispose() {
			if (umtDs != null) {
				umtDs.Dispose();
				umtDs = null;
			}
			if (Cursor != null)
				Cursor.Dispose();
		}
		#endregion
	}

	public class StaticUserMessageTranslator : ITranslator {
		private SimpleTranslator SimpleTranslator = null;
		public StaticUserMessageTranslator(MB3Client.ConnectionDefinition dbConnect) {
			RefreshTranslations(dbConnect);
			if (SimpleTranslator == null) // accessing the database failed.
				SimpleTranslator = new SimpleTranslator(); // contains nothing but the calls will succeed
		}
		public void RefreshTranslations(MB3Client.ConnectionDefinition dbConnect) {
			if (dbConnect == null)
				return;
			try {
				var db = new MB3Client(dbConnect);
				var t = new SimpleTranslator();
				using (dsMB ds = new dsMB(db)) {
					ds.EnsureDataTableExists(dsMB.Schema.T.UserMessageTranslation, dsMB.Schema.T.UserMessageKey);
					db.ViewAdditionalRows(ds, dsMB.Schema.T.UserMessageTranslation, null, null, new DBI_PathToRow[] { dsMB.Path.T.UserMessageTranslation.F.UserMessageKeyID.PathToReferencedRow });
					foreach (dsMB.UserMessageTranslationRow r in ds.GetDataTable(dsMB.Schema.T.UserMessageTranslation).Rows) {
						var key = new SimpleKey(ContextReference.New(r.UserMessageKeyIDParentRow.F.Context), r.UserMessageKeyIDParentRow.F.Key);
						t.Add(key, new CultureInfo(r.F.LanguageLCID), r.F.Translation);
					}
				}
				db.CloseDatabase();
				SimpleTranslator = t;
			}
			catch (System.Exception) { }
		}
		#region ITranslator Members
		public override string Translate(string context, string localKey, CultureInfo culture, string[] implicitTags, ref int penalty, Set<string> qualifiers) {
			return SimpleTranslator.Translate(context, localKey, culture, implicitTags, ref penalty, qualifiers);
		}
		public override Key.ITranslationEnumerator AllTranslations(string context, string localKey, CultureInfo ci, Set<string> qualifiers) {
			return SimpleTranslator.AllTranslations(context, localKey, ci, qualifiers);
		}
		#endregion
	}
}
