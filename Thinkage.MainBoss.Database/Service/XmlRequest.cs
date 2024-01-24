using System;
using System.Xml;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.Xml;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// A tool used to parse an xml string for request details (beyond subject and description)
	/// XML element and attribute names are case sensitive
	/// </summary>
	// An example XML snippet

	//<?xml version = "1.0" encoding="UTF-8"?>
	//<Request>
	//  <AccessCode>06_Code_AccessCode</AccessCode>
	//  <Priority>00_Code_RequestPriority</Priority>
	//  <Description>This is an XML test description</Description> 
	//  <Comment>These are the comments</Comment> 
	//  <Unit>Tractorx @ Farm @ 000Farm</Unit>
	//</Request>
	/// 
	public class XmlRequest {
		delegate object ValueGetter(dsMB lookupDs, XmlRequestMapping mapping, string val);
		private class XmlRequestMapping {
			public string tagName;
			public DBI_Column targetColumn;
			public ValueGetter getValue;
			public XmlRequestMapping(DBI_Column column, ValueGetter getter) :
				this(column.LabelKey.IdentifyingName, column, getter) {
			}
			public XmlRequestMapping([Invariant]string tName, DBI_Column column, ValueGetter getter) {
				tagName = tName.Replace(" ", "");
				targetColumn = column;
				getValue = getter;
			}
			public void SetValue(dsMB lookupDs, dsMB.RequestRow rrow, string v) {
				object newValue = getValue(lookupDs, this, v);
				if (newValue == null)
					return;
				if (targetColumn[rrow] != null)
					throw new GeneralException(KB.K("XML Request: The request element {0} cannot be used; the request field {1} has already been set by another element"), tagName, targetColumn.Name);
				targetColumn[rrow] = newValue;
			}
		};
		private static ValueGetter StringGetter = delegate (dsMB lookupDs, XmlRequestMapping mapping, string val) {
			return val;
		};
		static private XmlRequestMapping[] RequestFieldMappings = new XmlRequestMapping[]
		{
			new XmlRequestMapping(dsMB.Schema.T.Request.F.Description, StringGetter),
			new XmlRequestMapping(dsMB.Schema.T.Request.F.Comment, StringGetter),
			new XmlRequestMapping(dsMB.Schema.T.Request.F.AccessCodeID, delegate(dsMB lookupDs, XmlRequestMapping mapping, string val){
				lookupDs.EnsureDataTableExists(dsMB.Schema.T.AccessCode);
				var lookupRow = (dsMB.AccessCodeRow)lookupDs.DB.ViewAdditionalRow( lookupDs, dsMB.Schema.T.AccessCode, new SqlExpression(dsMB.Path.T.AccessCode.F.Code).Eq(SqlExpression.Constant(val)));
				if( lookupRow == null)
					throw new GeneralException(KB.K("Cannot find '{0}' that matches '{1}'"), mapping.tagName, val);
				return lookupRow.F.Id;
			}),
			new XmlRequestMapping(dsMB.Schema.T.Request.F.RequestPriorityID, delegate(dsMB lookupDs, XmlRequestMapping mapping, string val){
				lookupDs.EnsureDataTableExists(dsMB.Schema.T.RequestPriority);
				var lookupRow = (dsMB.RequestPriorityRow)lookupDs.DB.ViewAdditionalRow( lookupDs, dsMB.Schema.T.RequestPriority, new SqlExpression(dsMB.Path.T.RequestPriority.F.Code).Eq(SqlExpression.Constant(val)));
				if( lookupRow == null)
					throw new GeneralException(KB.K("Cannot find '{0}' that matches '{1}'"), mapping.tagName, val);
				return lookupRow.F.Id;
			}),
			new XmlRequestMapping(dsMB.Schema.T.Request.F.UnitLocationID, delegate(dsMB lookupDs, XmlRequestMapping mapping, string val){
				lookupDs.EnsureDataTableExists(dsMB.Schema.T.Location);
				var lookupRow = (dsMB.LocationRow)lookupDs.DB.ViewAdditionalRow( lookupDs, dsMB.Schema.T.Location, new SqlExpression(dsMB.Path.T.Location.F.Code).Eq(SqlExpression.Constant(val)));
				if( lookupRow == null)
					throw new GeneralException(KB.K("Cannot find '{0}' that matches '{1}'"), mapping.tagName, val);
				return lookupRow.F.Id;
			}),
			new XmlRequestMapping(KB.I("ExternalTag"), dsMB.Schema.T.Request.F.UnitLocationID, delegate(dsMB lookupDs, XmlRequestMapping mapping, string val){
				lookupDs.EnsureDataTableExists(dsMB.Schema.T.ExternalTag);
				var lookupRow = (dsMB.ExternalTagRow)lookupDs.DB.ViewAdditionalRow( lookupDs, dsMB.Schema.T.ExternalTag, new SqlExpression(dsMB.Path.T.ExternalTag.F.ExternalTag).Eq(SqlExpression.Constant(val)));
				if( lookupRow == null)
					throw new GeneralException(KB.K("Cannot find '{0}' that matches '{1}'"), mapping.tagName, val);
				return lookupRow.F.LocationID;
			})
		};
		const string xmlRequestElementName = "Request";
		/// <summary>
		/// Parses the xml and extracts the request information into the given request row
		/// </summary>
		/// <param name="rrow">the request row that will receive the request information</param>
		/// <param name="xml">the xml encoded request information</param>
		/// <returns>true if xml is properly formatted</returns>
		public static bool SetRequest(XAFClient DB, dsMB.RequestRow rrow, string xml) {
			if (xml.StartsWith(KB.I("<?xml")) || xml.StartsWith(KB.I("<?XML"))) {
				try {
					TXmlDocument xdoc = new Thinkage.Libraries.Xml.TXmlDocument();
					xdoc.LoadXml(xml);
					XmlNodeList nodelist = xdoc.GetElementsByTagName(KB.I(xmlRequestElementName));
					if (nodelist.Count == 0)
						throw new GeneralException(KB.K("XML Request: Missing '{0}' element"), KB.I(xmlRequestElementName));
					if (nodelist.Count > 1)
						throw new GeneralException(KB.K("XML Request: Only one '{0}' element permitted"), KB.I(xmlRequestElementName));
					// Validate all the tag Names we can find against our list and report those that do not match
					XmlNodeList allElements = nodelist[0].ChildNodes;
					System.Text.StringBuilder errors = new System.Text.StringBuilder();
					foreach (XmlNode node in allElements) {
						if (node.NodeType != XmlNodeType.Element)
							continue;
						if (Array.Exists(RequestFieldMappings, t => t.tagName == node.Name))
							continue;
						if (errors.Length == 0)
							errors.AppendLine(KB.K("The following elements are not recognized").Translate());
						errors.AppendLine(node.Name);
					}
					if (errors.Length > 0) {
						errors.AppendLine();
						errors.AppendLine(KB.K("Only the following element names can be used to provide the request information").Translate());
						Array.ForEach(RequestFieldMappings, t => errors.AppendLine(t.tagName));
						throw new GeneralException(KB.K("XML Request: Unrecognized XML elements")).WithContext(new Libraries.MessageExceptionContext(KB.T(errors.ToString())));
					}
					using (var ds = new dsMB(DB)) {
						foreach (XmlRequestMapping m in RequestFieldMappings) {
							XmlNodeList requestTagNodes = xdoc.GetElementsByTagName(m.tagName);
							if (requestTagNodes.Count > 1)
								throw new GeneralException(KB.K("XML Request: Only one '{0}' element permitted"), m.tagName);
							if (requestTagNodes.Count == 1)
								m.SetValue(ds, rrow, requestTagNodes[0].InnerXml);
						}
					}
					return true;
				}
				catch (System.Exception ex) {
					throw new Thinkage.Libraries.GeneralException(ex, KB.K("XML Request error"));
				}
			}
			return false;
		}
	}
}
