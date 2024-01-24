using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Thinkage.MainBoss.WebAccess.Models {
	public class Assignment {
		public AssignmentEntities.AttentionStatus AttentionStatus {
			get;
			set;
		}
		public AssignmentEntities.DatabaseStatus DatabaseStatus {
			get;
			set;
		}
	}
}