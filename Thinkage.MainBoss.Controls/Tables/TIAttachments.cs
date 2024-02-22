using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	public class TIAttachments : TIGeneralMB3 {
		private static readonly FeatureGroup AttachmentFeatureGroup = UnitsDependentGroup | WorkOrdersGroup | PurchasingGroup | RequestsGroup;
		private TIAttachments() {
		}
		#region View Record Types
		#region - AttachmentNames
		public enum AttachmentName {
			PathAttachment,
			ImageAttachment
		}
		#endregion
		#endregion
		#region Named Tbls
		#region Specification
		// This is a standard browser/editor. The editor has special creation code which customizes the tbl. This tbl is used for editing
		// and for the specifications browsette within the Form editor (where the browsette filter becomes an init specifying the form ID)
		// This form names the custom editor class and
		// a creator delegate whose purpose is to fetch the SpecificationFormField records and build a customized tbl (based on this one)
		// for the actual EditControl to use. The Tbl is also a custom class that contains mapping information for the individual
		// data records.
		public static DelayedCreateTbl SpecificationWithFormEditTblCreator = new DelayedCreateTbl(delegate () {
			return new Tbl(dsMB.Schema.T.Specification, TId.Specification,
				new Tbl.IAttr[] {
					UnitsDependentGroup,
					new ETbl(
						ETbl.LogicClass(typeof(SpecificationEditLogic)),
						ETbl.EditorAccess(false, EdtMode.UnDelete)
					)
				},
				new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsMB.Path.T.Specification.F.SpecificationFormID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.SpecificationForm.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Specification.F.AttachmentID.F.Code, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Specification.F.AttachmentID.F.Desc, ECol.Normal),
					// The fields defined by the Form go here, instead of the node identified with the label CustomFieldPlaceholderLabel.
					// Because this node has no DCol or ECol it will be ignored if not removed.
					TblUnboundControlNode.Empty(SpecificationEditLogic.CustomFieldPlaceholderLabel),
					TblColumnNode.New(dsMB.Path.T.Specification.F.ReportText, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.Specification.F.AttachmentID.F.Comment, DCol.Normal, ECol.Normal)
					)
				));
		});
		public static DelayedCreateTbl SpecificationWithFormBrowseTblCreator = new DelayedCreateTbl(delegate () {
			return new CompositeTbl(dsMB.Schema.T.Specification, TId.Specification,
				new Tbl.IAttr[] {
					UnitsDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.Specification.F.AttachmentID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.Specification.F.AttachmentID.F.Desc),
						BTbl.ListColumn(dsMB.Path.T.Specification.F.SpecificationFormID.F.Code)
					),
				},
				CompositeView.ChangeEditTbl(SpecificationWithFormEditTblCreator)
			);
		});
		#endregion

		internal readonly static string SpecificationFormNodeId = KB.I("SpecificationFormId");
		internal static Key AddLinkAttachment = KB.K("Add Attachment");
		#region UnitAttachment

		static readonly TblLayoutNodeArray unitAttachmentLocationNodes = new TblLayoutNodeArray(
			TblGroupNode.New(dsMB.Path.T.UnitAttachment.F.UnitLocationID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
						TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.UnitLocationID, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.UnitLocationID.F.Desc, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Make, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Model, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.Serial, DCol.Normal, ECol.AllReadonly)
			})
		);
		static readonly TblLayoutNodeArray unitAttachmentPathAttachmentNodes = new TblLayoutNodeArray(
			TblGroupNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Attachment.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.Desc, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.AttachmentPathID.F.Path, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.Comment, DCol.Normal, ECol.AllReadonly)
			})
		);
		static readonly TblLayoutNodeArray unitAttachmentSpecificationNodes = new TblLayoutNodeArray(
			TblGroupNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Attachment.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.Desc, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.SpecificationID.F.SpecificationFormID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.SpecificationForm.F.Code)), ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.SpecificationID.F.ReportText, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.Comment, DCol.Normal, ECol.AllReadonly)
			})
		);
		static readonly DelayedCreateTbl UnitAttachmentAndPathAttachmentEditTbl = new DelayedCreateTbl(delegate () {
			return new Tbl(dsMB.Schema.T.UnitAttachment, TId.UnitAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
			},
			((TblLayoutNodeArray)unitAttachmentLocationNodes.Clone() + (TblLayoutNodeArray)unitAttachmentPathAttachmentNodes.Clone())
			);
		});
		static readonly DelayedCreateTbl UnitSpecificationEditTbl = new DelayedCreateTbl(delegate () {
			return new Tbl(dsMB.Schema.T.UnitAttachment, TId.UnitAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
			},
			((TblLayoutNodeArray)unitAttachmentLocationNodes.Clone() + (TblLayoutNodeArray)unitAttachmentSpecificationNodes.Clone())
			//TODO: Get the SpecificationFormNodeId value into the browser init for New Specification; part of CompositeView for Specification Etbl
			);
		});
		public static readonly DelayedCreateTbl UnitAttachmentBrowseTblCreator = new DelayedCreateTbl(delegate () {
			object attachmentInfo = KB.I("attachmentInfo");
			return new CompositeTbl(dsMB.Schema.T.UnitAttachment, TId.UnitAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.UnitAttachment.F.UnitLocationID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.Code),
								BTbl.PerViewListColumn(KB.K("Information"), attachmentInfo)
						)
				},
				CompositeView.ChangeEditTbl(UnitAttachmentAndPathAttachmentEditTbl,
					CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.AttachmentPathID).IsNotNull()),
					BTbl.PerViewColumnValue(attachmentInfo, dsMB.Path.T.UnitAttachment.F.AttachmentID.F.AttachmentPathID.F.Path),
					CompositeView.JoinedNewCommand(AddLinkAttachment),
					//					CompositeView.ExportNewVerb(true),
					CompositeView.IdentificationOverride(TId.AttachmentPath)
				)
			);
		});
		public static readonly DelayedCreateTbl UnitSpecificationBrowseTblCreator = new DelayedCreateTbl(delegate () {
			object attachmentInfo = KB.I("attachmentInfo");
			return new CompositeTbl(dsMB.Schema.T.UnitAttachment, TId.UnitAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.UnitAttachment.F.UnitLocationID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.Code),
								BTbl.PerViewListColumn(KB.K("Information"), attachmentInfo)
								, BTbl.LogicClass(typeof(AttachmentSpecificationBrowseLogic))
						)
				},
				CompositeView.ChangeEditTbl(UnitSpecificationEditTbl,
					CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.UnitAttachment.F.AttachmentID.F.SpecificationID).IsNotNull()),
					BTbl.PerViewColumnValue(attachmentInfo, dsMB.Path.T.UnitAttachment.F.AttachmentID.F.SpecificationID.F.SpecificationFormID.F.Code),
					CompositeView.JoinedNewCommand(AddLinkAttachment),
					//					CompositeView.ExportNewVerb(true),
					CompositeView.IdentificationOverride(TId.Specification)
				)
			);
		});
		#endregion

		#region WorkOrderAttachment
		static readonly TblLayoutNodeArray WorkOrderAttachmentWorkOrderNodes = new TblLayoutNodeArray(
			TblGroupNode.New(dsMB.Path.T.WorkOrderAttachment.F.WorkOrderID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
						TblColumnNode.New(dsMB.Path.T.WorkOrderAttachment.F.WorkOrderID, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderAttachment.F.WorkOrderID.F.Subject, DCol.Normal, ECol.AllReadonly)
			})
		);
		static readonly TblLayoutNodeArray WorkOrderAttachmentPathAttachmentNodes = new TblLayoutNodeArray(
			TblGroupNode.New(dsMB.Path.T.WorkOrderAttachment.F.AttachmentID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
					TblColumnNode.New(dsMB.Path.T.WorkOrderAttachment.F.AttachmentID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Attachment.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderAttachment.F.AttachmentID.F.Desc, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.WorkOrderAttachment.F.AttachmentID.F.AttachmentPathID.F.Path, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.WorkOrderAttachment.F.AttachmentID.F.Comment, DCol.Normal, ECol.AllReadonly)
			})
		);
		static readonly DelayedCreateTbl WorkOrderPathAttachmentEditTbl = new DelayedCreateTbl(delegate () {
			return new Tbl(dsMB.Schema.T.WorkOrderAttachment, TId.WorkOrderAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
			},
			((TblLayoutNodeArray)WorkOrderAttachmentWorkOrderNodes.Clone() + (TblLayoutNodeArray)WorkOrderAttachmentPathAttachmentNodes.Clone())
			);
		});
		public static readonly DelayedCreateTbl WorkOrderAttachmentBrowseTblCreator = new DelayedCreateTbl(delegate () {
			object attachmentInfo = KB.I("attachmentInfo");
			return new CompositeTbl(dsMB.Schema.T.WorkOrderAttachment, TId.WorkOrderAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderAttachment.F.WorkOrderID.F.Number),
								BTbl.ListColumn(dsMB.Path.T.WorkOrderAttachment.F.AttachmentID.F.Code),
								BTbl.PerViewListColumn(KB.K("Information"), attachmentInfo)
						)
				},
				CompositeView.ChangeEditTbl(WorkOrderPathAttachmentEditTbl,
					CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.WorkOrderAttachment.F.AttachmentID.F.AttachmentPathID).IsNotNull()),
					BTbl.PerViewColumnValue(attachmentInfo, dsMB.Path.T.WorkOrderAttachment.F.AttachmentID.F.AttachmentPathID.F.Path),
					CompositeView.JoinedNewCommand(AddLinkAttachment),
					//					CompositeView.ExportNewVerb(true),
					CompositeView.IdentificationOverride(TId.AttachmentPath)
				)
			);
		});
		#endregion

		#region WorkOrderTemplateAttachment
		static readonly TblLayoutNodeArray WorkOrderTemplateAttachmentWorkOrderNodes = new TblLayoutNodeArray(
			TblGroupNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.WorkOrderTemplateID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.WorkOrderTemplateID, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.WorkOrderTemplateID.F.Subject, DCol.Normal, ECol.AllReadonly)
			})
		);
		static readonly TblLayoutNodeArray WorkOrderTemplateAttachmentPathAttachmentNodes = new TblLayoutNodeArray(
			TblGroupNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, new TblLayoutNode[] {
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Attachment.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID.F.Desc, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID.F.AttachmentPathID.F.Path, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID.F.Comment, DCol.Normal, ECol.AllReadonly)
			})
		);
		static readonly DelayedCreateTbl WorkOrderTemplatePathAttachmentEditTbl = new DelayedCreateTbl(delegate () {
			return new Tbl(dsMB.Schema.T.WorkOrderTemplateAttachment, TId.WorkOrderTemplateAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
			},
			((TblLayoutNodeArray)WorkOrderTemplateAttachmentWorkOrderNodes.Clone() + (TblLayoutNodeArray)WorkOrderTemplateAttachmentPathAttachmentNodes.Clone())
			);
		});
		public static readonly DelayedCreateTbl WorkOrderTemplateAttachmentBrowseTblCreator = new DelayedCreateTbl(delegate () {
			object attachmentInfo = KB.I("attachmentInfo");
			return new CompositeTbl(dsMB.Schema.T.WorkOrderTemplateAttachment, TId.WorkOrderTemplateAttachment,
				new Tbl.IAttr[] {
						CommonTblAttrs.ViewCostsDefinedBySchema,
						AttachmentFeatureGroup,
						new BTbl(BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplateAttachment.F.WorkOrderTemplateID.F.Code),
								BTbl.ListColumn(dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID.F.Code),
								BTbl.PerViewListColumn(KB.K("Information"), attachmentInfo)
						)
				},
				CompositeView.ChangeEditTbl(WorkOrderTemplatePathAttachmentEditTbl,
					CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID.F.AttachmentPathID).IsNotNull()),
					BTbl.PerViewColumnValue(attachmentInfo, dsMB.Path.T.WorkOrderTemplateAttachment.F.AttachmentID.F.AttachmentPathID.F.Path),
					CompositeView.JoinedNewCommand(AddLinkAttachment),
					//					CompositeView.ExportNewVerb(true),
					CompositeView.IdentificationOverride(TId.AttachmentPath)
				)
			);
		});
		#endregion

		/// <summary>
		/// 
		/// </summary>
		private static readonly DelayedCreateTbl AttachmentPathEditTblCreator = new DelayedCreateTbl(delegate () {
			return new Tbl(dsMB.Schema.T.AttachmentPath, TId.AttachmentPath,
				new Tbl.IAttr[] {
						AttachmentFeatureGroup,
						new ETbl(ETbl.LogicClass(typeof(AttachmentPathEditLogic)))
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.AttachmentPath.F.AttachmentID.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.AttachmentPath.F.AttachmentID.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.AttachmentPath.F.Path, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.AttachmentPath.F.AttachmentID.F.Comment, DCol.Normal, ECol.Normal)
					)
				)
			);
		});
		public static readonly DelayedCreateTbl AttachmentPathBrowseTblCreator = new DelayedCreateTbl(delegate () {
			return new CompositeTbl(dsMB.Schema.T.AttachmentPath, TId.AttachmentPath,
				new Tbl.IAttr[] {
					AttachmentFeatureGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.AttachmentPath.F.AttachmentID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.AttachmentPath.F.AttachmentID.F.Desc),
						BTbl.ListColumn(dsMB.Path.T.AttachmentPath.F.Path)
					)
				},
				CompositeView.ChangeEditTbl(AttachmentPathEditTblCreator)
			);
		});
		#endregion

		#region FUTURE Image Attachments
#if IMAGEATTACHMENTS
		#region Attachment Viewers/Editors
			private static DelayedCreateTbl ImageViewerEditorTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.AttachmentImage, TId.AttachmentImage,
					new Tbl.IAttr[] {
					AttachmentFeatureGroup,
					new ETbl(ETbl.EditorDefaultAccess(false))
				},
					new TblLayoutNodeArray(
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.AttachmentID.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.AttachmentID.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.ContentType, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.ContentEncoding, DCol.Normal, ECol.Normal),
					TblGroupNode.New(KB.K("Image Content"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(null, dsMB.Path.T.AttachmentImage.F.Content, DCol.Image, new ECol(Fmt.SetUsage(DBI_Value.UsageType.Image)))
					)
					)
				);
			});
		#endregion
		#region AttachmentImageEdit
			private static DelayedCreateTbl AttachmentImageEditTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.AttachmentImage, TId.AttachmentImage,
					new Tbl.IAttr[] {
						AttachmentFeatureGroup,
						new ETbl()
					},
					new TblLayoutNodeArray(
						DetailsTabNode.New(
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.AttachmentID.F.Code, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.AttachmentID.F.Desc, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.ContentType, DCol.Normal, ECol.Normal),
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.ContentEncoding, DCol.Normal, ECol.Normal),
							TblGroupNode.New(KB.K("Image"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
								TblColumnNode.New(null, dsMB.Path.T.AttachmentImage.F.Content, DCol.Image, new ECol(Fmt.SetUsage(DBI_Value.UsageType.Image)))
							),
							TblColumnNode.New(dsMB.Path.T.AttachmentImage.F.AttachmentID.F.Comment, DCol.Normal, ECol.Normal)
						)
					)
				);
			});
		#endregion
#endif
		#endregion

		#region AttachmentBrowseTblCreator
		public static readonly DelayedCreateTbl AttachmentBrowseTblCreator = new DelayedCreateTbl(delegate () {
			return new CompositeTbl(dsMB.Schema.T.Attachment, TId.Attachment,
				new Tbl.IAttr[] {
						AttachmentFeatureGroup,
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Attachment.F.Code),
							BTbl.ListColumn(dsMB.Path.T.Attachment.F.Desc)
						)
				},
				new CompositeView(AttachmentPathEditTblCreator, dsMB.Path.T.Attachment.F.AttachmentPathID,
					CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.Attachment.F.AttachmentPathID).IsNotNull())
				)
#if IMAGEATTACHMENTS
						,
						new CompositeView(AttachmentImageEditTbl, dsMB.Path.T.Attachment.F.AttachmentImageID,
							CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.Attachment.F.AttachmentImageID).IsNotNull())
						)
#endif
					);
		});
		#endregion
		internal static void DefineTblEntries() {

			DefineBrowseTbl(dsMB.Schema.T.Attachment, AttachmentBrowseTblCreator);
			DefineTbl(dsMB.Schema.T.UnitAttachment, UnitAttachmentBrowseTblCreator);
			DefineTbl(dsMB.Schema.T.WorkOrderAttachment, WorkOrderAttachmentBrowseTblCreator);
			DefineTbl(dsMB.Schema.T.WorkOrderTemplateAttachment, WorkOrderTemplateAttachmentBrowseTblCreator);

			#region SpecificationData
			DefineTbl(dsMB.Schema.T.SpecificationData, delegate () {
				return new Tbl(dsMB.Schema.T.SpecificationData, TId.SpecificationData,
				new Tbl.IAttr[] {
					UnitsDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.SpecificationData.F.SpecificationFormFieldID.F.EditLabel),
							BTbl.ListColumn(dsMB.Path.T.SpecificationData.F.FieldValue)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.Delete, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.SpecificationData.F.SpecificationFormFieldID.F.EditLabel, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.SpecificationData.F.FieldValue, ECol.Normal)
				));
			});
			#endregion
			#region SpecificationForm
			DefineTbl(dsMB.Schema.T.SpecificationForm, delegate () {
				return new Tbl(dsMB.Schema.T.SpecificationForm, TId.SpecificationForm,
				new Tbl.IAttr[] {
					UnitsDependentGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.SpecificationForm.F.Code), BTbl.ListColumn(dsMB.Path.T.SpecificationForm.F.Desc),
						BTbl.SetReportTbl(new DelayedCreateTbl(() => TIReports.SpecificationFormReport))),
					new ETbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.SpecificationForm.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.SpecificationForm.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.SpecificationForm.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.SpecificationFormField, TId.SpecificationForm,
						TblColumnNode.NewBrowsette(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID, DCol.Normal, ECol.Normal)),
					TblTabNode.New(KB.K("Default Report Layout"), KB.K("Display the default report layout for this specification form"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.SpecificationForm.F.DefaultReportLayout, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)),
					TblTabNode.New(KB.K("Custom Report Layout"), KB.K("Display the custom report layout for this specification form"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.SpecificationForm.F.CustomizedReportLayout, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Specification, TId.SpecificationForm,
						TblColumnNode.NewBrowsette(SpecificationWithFormBrowseTblCreator, dsMB.Path.T.Specification.F.SpecificationFormID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion
			#region SpecificationFormField
			Key FieldSize = KB.K("Field Size");
			Key FieldName = KB.K("Field Name");
			DefineTbl(dsMB.Schema.T.SpecificationFormField, delegate () {
				return new Tbl(dsMB.Schema.T.SpecificationFormField, TId.SpecificationFormField,
				new Tbl.IAttr[] {
					UnitsDependentGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.SpecificationFormField.F.FieldName),
						BTbl.ListColumn(dsMB.Path.T.SpecificationFormField.F.EditLabel),
						BTbl.ListColumn(dsMB.Path.T.SpecificationFormField.F.FieldSize),
						BTbl.ListColumn(dsMB.Path.T.SpecificationFormField.F.FieldOrder)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.SpecificationFormID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.SpecificationForm.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.FieldName, DCol.Normal, new ECol(Fmt.SetId(FieldName))),
					TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.EditLabel, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.FieldSize, DCol.Normal, new ECol(Fmt.SetId(FieldSize))),
					// ??? should field order be editable or implied by a specicial editor interface?
					TblColumnNode.New(dsMB.Path.T.SpecificationFormField.F.FieldOrder, DCol.Normal, ECol.Normal)
				),
				new Check1<long?>(delegate (long? fieldSize) {
					if (fieldSize > 80)
						return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Field Size must be less than or equal to 80")));
					return null;
				})
				.Operand1(FieldSize),
				new Check1<string>(delegate (string fieldName) {
					if (fieldName != null)
						try {
							System.Xml.XmlConvert.VerifyName(fieldName);
						}
						catch (System.Xml.XmlException) {
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Field Name must be a valid XML name beginning with a alphabetic character followed by 0 or more alphabetic or numeric characters.")));
						}
					return null;
				})
				.Operand1(FieldName)
				);
			});
			#endregion
		}
	}
}