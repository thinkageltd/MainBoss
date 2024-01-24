using System;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.WebApi
{
	/// <summary>
	/// Keybuilder for the Thinkage.MainBoss.Controls context
	/// </summary>
	internal class KB : Thinkage.Libraries.Translation.KB
	{
		static KB()
		{
			DeclareAssemblyProvidesTranslationsUsingResource(K(null), System.Reflection.Assembly.GetExecutingAssembly());
		}
		static KB Instance = new KB();
		public static SimpleKey K([Context(Level = 1)] string s)
		{
#if FINDMISSINGTIPS
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(s));
#endif
			return Instance.BuildKey(s);
		}
	}
}