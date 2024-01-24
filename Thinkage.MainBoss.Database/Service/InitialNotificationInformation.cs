
namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Information about someone to embed in an email message. Typically a mailto will be come a mailto: link of some form. A phone should
	/// turn into a phone link if possible.
	/// </summary>
	public class ContactInformation {
		public readonly string Name;
		public readonly string MailTo;
		public readonly string Phone;
		public readonly bool IsDeleted;
		public ContactInformation(bool deleted, string name, string mailto, string phone) {
			IsDeleted = deleted;
			Name = name;
			MailTo = mailto;
			Phone = phone;
		}
	}
	/// <summary>
	/// Information about a unit
	/// </summary>
	public class UnitInformation {
		public readonly string Unit;
		public readonly bool IsDeleted;
		public UnitInformation(bool deleted, string code) {
			Unit = code;
			IsDeleted = deleted;
		}
	}
	/// <summary>
	/// Information to include in initial notification to a assignee
	/// </summary>
	public class InitialNotificationInformation {
		public readonly ContactInformation RequestorInformation;
		public readonly UnitInformation UnitInformation;
		public readonly string WorkDescription;
		public InitialNotificationInformation(string work, UnitInformation unit, ContactInformation requestor) {
			WorkDescription = work;
			RequestorInformation = requestor;
			UnitInformation = unit;
		}
	}
}
