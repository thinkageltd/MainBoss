//#define NEW_CODE_IS_FINISHED
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.DBAccess;
using System.Collections.Generic;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.DataFlow;

namespace Thinkage.MainBoss.Controls {
	public class StateHistoryBrowseLogic : BrowseLogic {
		private class CustomTblBuilder {
			public CustomTblBuilder(DBClient db, Tbl tbl) {
				Tbl = tbl;
#if NEW_CODE_IS_FINISHED
				MB3BTbl.StateHistoryLogicArg wha = (MB3BTbl.StateHistoryLogicArg)BTbl.Find(Tbl).BrowserLogicClassArg;
				ActionManager = new HistoryActionManager(db, wha.HistoryTable, wha.CustomFlagField, wha.TblToUseWhenCustomFlagGoesFalse,
					delegate(DBI_Path path) {
						return new NotifyingSourceWrapper(new NullSource(NullTypeInfo.Universe));
					},
					null);
#endif
			}
			private readonly Tbl Tbl;
#if NEW_CODE_IS_FINISHED
			private readonly HistoryActionManager ActionManager;
#endif
			public Tbl GetTbl() {
#if NEW_CODE_IS_FINISHED
				// TODO: Have a permanent cache of the created tbls (including the db in the key), and have HistoryActionManager be long-lived as well, sending us a notification
				// when its transitions change so we can clear the cache (and re-build it again on demand).

				// The called edit tbls are grouped by what additional paths are referenced by the conditions, and the edit tbl itself contains appropriate Check's
				// again on the current/new state and on additional conditions.
				// The reason for not using a single edit tbl is that, say for a transition from WO Draft to WO Open (neither of which care about temp storage being empty)
				// the WO.TemporaryStorageEmpty flag will still be referenced by a (unused) Check operation for Open->Closed, so if one person is trying to open a draft WO
				// while someone else is emptying (on unemptying) temp storage the first user will get a bogus concurrency error.
				// The called editor is based on TIGeneralMB3.FindDelayedEditTbl(Tbl.Schema), with us adding a Check operation that validates the state transition.
				var extraNewVerbs = new List<CompositeView>(Tbl.TblViews);
				CompositeView.ICtorArg commandGroupArg = CompositeView.NewCommandGroup(KB.K("State History"));
				foreach (HistoryActionManager.Transition t in ActionManager.Transitions) {
					//     build/pull from cache the edit tbl
					// TODO: Tbl should contain Check operations on valid from/to states (all with a single table-driven check operation which takes the old-state and new-state paths as Check arguments)
					// If the transition needs other information the paths should be additional arguments to the Check operation, and the argument list of the check operation should be part of the key
					// (along with the original tbl) used to cache massaged edit tbls.

					Tbl editTbl = t.EditTblCreator.Tbl;
					// TODO: Add conditions to the New command. Note that normally compostive view New-command conditions apply only to contextual new commands.
					// Here we want a non-contextual condition to use on non-contextual new commands. ***This is a toughie; similar toughness occurs in the Dead End Disabler***
					// The problem is that we want to follow a path from the ID in the master-record filter not using any records in the browse buffer. On export this would transform into a regular
					// path condition in the outer scope (assuming export consisted of building a new command from scratch in the outer scope).
					// foreach (MB3Client.StateHistoryTable.StateFlagRestriction r in t.Restrictions)
					//		Apply r.Condition as an enabling condition on the New verb, with r.ViolationMessage as the disabled tip.
					// Apply t.OldState.ID == current master-record state as an enabling condition on the New verb, with KB.K("This operation is not allowed in the current state") as the disabled tip

					// if (t.ControllingActionRightName != null)
					//		somehow require the right t.ControllingActionRightName on the New command, maybe by putting it on the edit tbl. This would be yet another cache key for these.

					//     Create an AdditionalNewVerb which uses this tbl and inits the destination state
					//
					// TODO: If t.TransitionWithoutUI tell the composite view likewise (need to add NoUIOnContextFreeNew(bool))
					CompositeView additionalNewVerbView = CompositeView.ExtraNewVerb(new DelayedCreateTbl(editTbl),
						CompositeView.ExportNewVerb(true),
						CompositeView.JoinedNewCommand(t.Name),
						commandGroupArg,
						CompositeView.IdentificationOverride(t.Name.Translate(CultureInfo.InvariantCulture)),
						CompositeView.ContextFreeInit(new ConstantValue(t.NewState.ID), ActionManager.HistoryTable.HistToStatePath)
					);
					//     Add it to a list of additional new verbs
					extraNewVerbs.Add(additionalNewVerbView);
				}
				// create and return a new Composite Tbl with all these additional views.
				return new CompositeTbl(Tbl.Schema, Tbl.Identification, Tbl.Attributes, Tbl.RecordType, extraNewVerbs.ToArray());
#else
				return Tbl;
#endif
			}
		}
		public StateHistoryBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: this(control, db, takeDBCustody, new CustomTblBuilder(db, tbl), settingsContainer, structure) {
		}
		private StateHistoryBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, CustomTblBuilder tblBuilder, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tblBuilder.GetTbl(), settingsContainer, structure) {
		}
	}
}