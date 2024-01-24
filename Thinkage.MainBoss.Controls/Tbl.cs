using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	#region StateHistoryUITable combining MB3Client.StateHistoryTable with UI information (state history edit tbls)
	public class StateHistoryUITable {
		public StateHistoryUITable(MB3Client.StateHistoryTable historyTable, DBI_Column customFlagField, DelayedCreateTbl editTblToUseByDefault, DelayedCreateTbl editTblToUseWhenCustomFlagGoesFalse) {
			HistoryTable = historyTable;
			CustomFlagField = customFlagField;
			EditTblToUseByDefault = editTblToUseByDefault;
			EditTblToUseWhenCustomFlagGoesFalse = editTblToUseWhenCustomFlagGoesFalse;
		}
		public readonly MB3Client.StateHistoryTable HistoryTable;
		public readonly DBI_Column CustomFlagField;
		public readonly DelayedCreateTbl EditTblToUseByDefault;
		public readonly DelayedCreateTbl EditTblToUseWhenCustomFlagGoesFalse;
	}
	#endregion
	#region MB3BTbl - Custom BTbl derivation for MB3
	// We mark this class abstract so no one tries to create one.
	public abstract class MB3BTbl : BTbl {
		#region Construction
		#region - Constructor optional arguments
		/// <summary>
		/// Specify that a WithHistoryColumnBrowseControl should be created using the specified StateHistoryUITable
		/// </summary>
		/// <returns></returns>
		public static ICtorArg HasStateHistory(DelayedConstruction<StateHistoryUITable> historyTable) {
			return new WithHistoryLogicArg(historyTable);
		}
		/// <summary>
		/// Specify that a StateHistoryLogicArg should be created using the specified StateHistoryUITable
		/// </summary>
		/// <returns></returns>
		public static ICtorArg IsStateHistoryTbl(DelayedConstruction<StateHistoryUITable> historyTable) {
			return new StateHistoryLogicArg(historyTable);
		}
		#endregion
		#region - Constructor optional argument implementation classes
		public class WithHistoryLogicArg : CustomLogicClassArg {
			public WithHistoryLogicArg(DelayedConstruction<StateHistoryUITable> historyTable)
				: base(typeof(WithHistoryColumnBrowseLogic)) {
				HistoryTable = historyTable;
			}
			public readonly DelayedConstruction<StateHistoryUITable> HistoryTable;
		}
		public class StateHistoryLogicArg : CustomLogicClassArg {
			public StateHistoryLogicArg(DelayedConstruction<StateHistoryUITable> historyTable)
				: base(typeof(StateHistoryBrowseLogic)) {
				HistoryTable = historyTable;
			}
			public readonly DelayedConstruction<StateHistoryUITable> HistoryTable;
		}
		public abstract class WithServiceLogicArg : CustomLogicClassArg {
			public WithServiceLogicArg(System.Type browserLogicClass, System.Type serviceApplicationClass)
				: base(browserLogicClass) {
				ServiceDefinitionClass = serviceApplicationClass;
				System.Diagnostics.Debug.Assert(ServiceDefinitionClass.GetConstructor(ctorArgTypes) != null);
			}
			public readonly System.Type ServiceDefinitionClass;
			public static readonly System.Type[] ctorArgTypes = new System.Type[] { };
		}
		public class WithManageServiceLogicArg : WithServiceLogicArg {
			public WithManageServiceLogicArg(System.Type serviceApplicationClass, params ManageServiceBrowseLogic.ServiceActionCommand[] serviceCommands)
				: base(typeof(ManageServiceBrowseLogic), serviceApplicationClass) {
				ServiceCommands = serviceCommands;
			}
			public readonly ManageServiceBrowseLogic.ServiceActionCommand[] ServiceCommands;
		}
		#endregion
		#endregion
	}
	#endregion
	#region MB3ETbl - Custom ETbl derivation for MB3
	// We mark this class abstract so no one tries to create one.
	public abstract class MB3ETbl : ETbl {
		#region - Constructor optional arguments
		/// <summary>
		/// Specify that this Tbl is for editing State History records.
		/// </summary>
		/// <param name="stateHistory"></param>
		/// <returns></returns>
		public static ICtorArg IsStateHistoryTbl(DelayedConstruction<StateHistoryUITable> stateHistory) {
			return new StateHistoryTbl(stateHistory);
		}
		/// <summary>
		/// Specify that the Tbl is for editing records with a sequence counter and a state history
		/// </summary>
		/// <returns></returns>
		public static ICtorArg HasStateHistoryAndSequenceCounter(DBI_Path sequenceTargetPath, DBI_Table spoilTable, DBI_Variable counterVariable, DBI_Variable formatVariable, DelayedConstruction<StateHistoryUITable> stateHistory) {
			return new WithStateHistoryAndSequenceCounterLogicArg(typeof(WithSequenceCounterEditLogic), sequenceTargetPath, spoilTable, counterVariable, formatVariable, stateHistory);
		}
		public static ICtorArg HasStateHistoryAndSequenceCounter(System.Type logicType, DBI_Path sequenceTargetPath, DBI_Table spoilTable, DBI_Variable counterVariable, DBI_Variable formatVariable, DelayedConstruction<StateHistoryUITable> stateHistory) {
			System.Diagnostics.Debug.Assert(logicType.IsSubclassOf(typeof(WithSequenceCounterEditLogic)), "MB3ETbl.WithSequenceCounterLogic: supplied EditLogic class is not a derivation of WithSequenceCounterEditLogic");
			return new WithStateHistoryAndSequenceCounterLogicArg(logicType, sequenceTargetPath, spoilTable, counterVariable, formatVariable, stateHistory);
		}
		#endregion
		#region - Constructor optional argument implementation classes
		public class StateHistoryTbl : ETbl.ICtorArg {
			#region Housekeeping for ICtorArg
			internal class Organization : ICtorArgOrganization {
				public bool IsSingleValued {
					get {
						return true;
					}
				}
			}
			internal static readonly Organization OrganizationObject = new Organization();
			public ICtorArgOrganization CtorArgOrganization {
				get {
					return OrganizationObject;
				}
			}
			#endregion
			public readonly DelayedConstruction<StateHistoryUITable> StateHistory;
			public StateHistoryTbl(DelayedConstruction<StateHistoryUITable> stateHistory) : base() {
				StateHistory = stateHistory;
			}
		}
		public class WithStateHistoryAndSequenceCounterLogicArg : CustomLogicClassArg {
			public WithStateHistoryAndSequenceCounterLogicArg(System.Type logicType, DBI_Path sequenceTargetPath, DBI_Table spoilTable, DBI_Variable counterVariable, DBI_Variable formatVariable, DelayedConstruction<StateHistoryUITable> stateHistory)
				: base(logicType) {
				SequenceTargetPath = sequenceTargetPath;
				SpoilTable = spoilTable;
				CounterVariable = counterVariable;
				FormatVariable = formatVariable;
				StateHistory = stateHistory;
			}
			public readonly DBI_Path SequenceTargetPath;
			public readonly DBI_Table SpoilTable;
			public readonly DBI_Variable CounterVariable;
			public readonly DBI_Variable FormatVariable;
			public readonly DelayedConstruction<StateHistoryUITable> StateHistory;
		}
		#endregion
	}
	#endregion
	#region - AddSchemaCostTblLayoutNodeAttributesTbl
	public class AddSchemaCostTblLayoutNodeAttributesTbl : AddCostTblLayoutNodeAttributesTbl {
		public override TblLayoutNode.ICtorArg[] Attributes { get { return pAttributes; } }
		private TblLayoutNode.ICtorArg[] pAttributes;

		public AddSchemaCostTblLayoutNodeAttributesTbl()
			: base() {
		}
		public override void AssignToTbl(Tbl tbl) {
			base.AssignToTbl(tbl);
			pAttributes = AddSchemaCostTblLayoutNodeAttributesTbl.PermissionAttributesFromSchema(tbl.Schema).ToArray();
		}

		public static IEnumerable<TblLayoutNode.ICtorArg> PermissionAttributesFromSchema(Thinkage.Libraries.DBILibrary.DBI_Table schema) {
			if (schema == null || schema.CostRights.Count == 0) {
				return new TblLayoutNode.ICtorArg[] { };
			}
			List<PermissionToView> attributes = new List<PermissionToView>();
			foreach (string p in schema.CostRights)
				attributes.Add(new PermissionToView((Right)Root.Rights.ViewCost.FindDirectChild(p)));
			return attributes;
		}
	}
	#endregion
	#region Pre-built Tbl Attributes
	public static class CommonTblAttrs {
		public static Tbl.IAttr ViewCostsDefinedBySchema = new AddSchemaCostTblLayoutNodeAttributesTbl();
	}
	#endregion
}
