using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	/// <summary>
	/// Determine whether a particular date is available for use. Also be able to calculate when
	/// the NextAvailable and NextNotAvailable date might be from the given date.
	/// </summary>
	public interface IAvailability {
		/// <summary>
		/// Is the given date available?
		/// </summary>
		/// <param name="d"></param>
		/// <returns>true if available</returns>
		bool IsAssuredlyAvailable(PMGeneration.FuzzyDate d);
		/// <summary>
		/// Calculate the date this availability will next be available given a date which IS NOT available
		/// </summary>
		/// <param name="d"></param>
		/// <returns>Future date of when next available</returns>
		PMGeneration.FuzzyDate NextAvailableOnOrAfter(PMGeneration.FuzzyDate d);
		/// <summary>
		/// The identity of this particular Availability for error message annotation.
		/// </summary>
		string IDText {
			get;
		}
		/// <summary>
		/// The text to include in the Details when this is not available.
		/// </summary>
		Key MessageText {
			get;
		}
	}

	public class AvailabilityException : GeneralException {
		public AvailabilityException(Key msg)
			: base(msg) {
		}
	}
}
