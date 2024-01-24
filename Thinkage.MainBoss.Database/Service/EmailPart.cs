using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Dart.Mail;
namespace Thinkage.MainBoss.Database {
	[System.Diagnostics.DebuggerDisplay("{Part} Text={Text}")]
	public class EmailPart {
		public readonly Part Part;
		public bool PartOfBody = false;
		//bool Primary = false;
		public int Index;
		public int Parent;
		public EmailPart(Part p, int parent, int index) {
			Part = p;
			Index = index;
			Parent = parent;
		}
		public string Text {
			get {
				var t = Part as Textpart;
				if (t != null)
					return t.Content;
				var h = Part as Htmlpart;
				if (h != null)
					return EmailMessage.StripHTML(h.Content);
				return null;
			}
		}
		public string ContentDisposition {
			get {
				var t = Part as Textpart;
				if (t != null && t.ContentDisposition != null)
					return t.ContentDisposition.DispositionType;
				var h = Part as Htmlpart;
				if (h != null && h.ContentDisposition != null)
					return h.ContentDisposition.DispositionType;
				return null;
			}
		}
		public byte[] Content {
			get {
				var a = Part as Attachment;
				if (a != null)
					return System.IO.File.ReadAllBytes(a.Content.FullName);
				var r = Part as Resource;
				if (r != null && r.Length != 0)
					return r.Content;
				return null;
			}
		}
		public string Headers => string.Join(Environment.NewLine, Part.Headers.Select(d => d.Value.ToString())); 
		public static List<EmailPart> PartList(EmailMessage message) {
			var parts = Linear(message.Parts, -1);
			//FindPrimary(parts, -1, true, message.IsAlternative);
			return parts;
		}
		public static string  ConstructBody(List<EmailPart> parts) {
			var body = new StringBuilder();
			string separator = String.Empty;
			foreach (var p in parts)
				if (p.PartOfBody) {
					body.Append(separator);
					body.Append(p.Text);
					separator = Environment.NewLine;
				}
			return body.ToString();
		}
		static private List<EmailPart> Linear(MultipartContent partlist, int parent) {
			int index = 0;
			var parts = new List<EmailPart>();
			foreach (var p in partlist) {
				index++;
				var ep = new EmailPart(p, parent,  parent+index);
				parts.Add(ep);
				var mp = p as Multipart;
				if (mp != null) {
					var subParts = Linear(mp.Parts, parent + index);
					index += subParts.Count;
					parts.AddRange(subParts);
                }
			}
			return parts;
		}
	}
}
