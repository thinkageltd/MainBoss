using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// Browse control for picking Expense Categories filtered based on association with a given Expense Model
	/// </summary>
	public class FilteredWorkOrderExpenseCategoryBrowseLogic : BrowseLogic
	{
		public static readonly object ModelFilterId = 42;

		#region Construction
		public FilteredWorkOrderExpenseCategoryBrowseLogic(IBrowseUI control, DBClient db, bool takeDBCustody, Tbl tbl, Settings.Container settingsContainer, BrowseLogic.BrowseOptions structure)
			: base(control, db, takeDBCustody, tbl, settingsContainer, structure)
		{
		}

		public override void CreateTblCodedFilters() {
			base.CreateTblCodedFilters();
			ModelFilter f = new ModelFilter();
			AddGlobalFilter(f);
			TaggedFilters[ModelFilterId] = f.ExpenseModelIDParameter;
		}
		#endregion

		#region Custom Filter class
		private class ModelFilter : BrowseLogic.Filter {
			public ModelFilter()
				: this(new FilterParameter(dsMB.Schema.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID.EffectiveType.UnionCompatible(Libraries.TypeInfo.NullTypeInfo.Universe), null)) {
			}
			private ModelFilter(FilterParameter parameter) {
				ExpenseModelIDParameter = parameter;
				ExpenseModelIDParameter.ParameterChanged += NotifyServerFilterChanged;
			}
			public readonly FilterParameter ExpenseModelIDParameter;
			protected override Thinkage.Libraries.DBILibrary.SqlExpression InnerServerFilter {
				get {
					if (ExpenseModelIDParameter.GetValue() == null)
						return SqlExpression.Constant(true);

					return new SqlExpression(dsMB.Path.T.WorkOrderExpenseCategory.F.Id)
						.In(new SelectSpecification(dsMB.Schema.T.WorkOrderExpenseModelEntry,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseCategoryID) },
							new SqlExpression(dsMB.Path.T.WorkOrderExpenseModelEntry.F.WorkOrderExpenseModelID).Eq(SqlExpression.Constant(ExpenseModelIDParameter.GetValue())),
							null));
				}
			}
		}
		#endregion
	}
}
