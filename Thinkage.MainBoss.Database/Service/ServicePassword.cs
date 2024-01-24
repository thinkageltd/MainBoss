using System;
using System.Security.Cryptography;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database.Service
{
	/// <summary>
	/// Class to provide encryption/decryption of service passwords stored for mainboss applications.
	/// </summary>
	public class ServicePassword
	{
		private static PasswordDeriveBytes PDB
		{
			get
			{
				byte[] sel = new byte[]{66, 43, 22, 34, 11, 39, 2, 13};
				return new PasswordDeriveBytes(KB.I("frappuccino-281-ML"), sel, KB.I("MD5"), 9999);
			}
		}
		/// <summary>
		/// The Data Encryption Standard Key
		/// </summary>
		private static byte[] DESKey
		{
			get
			{
				return PDB.GetBytes(16);
			}
		}
		/// <summary>
		/// The Data Encryption Standard Initialization Vector
		/// </summary>
		private static byte[] DESIV
		{
			get
			{
				return PDB.GetBytes(8);
			}
		}

		public static string Decode(Byte[] pw)
		{
			return Crypt.DecryptPassword(pw, DESKey, DESIV);
		}

		public static Byte[] Encode(string pw)
		{
			return Crypt.EncryptPassword(pw, DESKey, DESIV);
		}
	}
}
