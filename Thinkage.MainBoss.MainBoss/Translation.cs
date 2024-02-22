using System;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.MainBoss
{
	/// <summary>
	///	Keybuilder for the MB3.MainBoss context
	/// </summary>
	internal class KB : Thinkage.Libraries.Translation.KB
	{
		static KB()
		{
			DeclareAssemblyProvidesTranslationsUsingResource(K(null), System.Reflection.Assembly.GetExecutingAssembly());
		}

		static readonly KB Instance = new KB();
		public static SimpleKey K([Context(Level = 1)] string s)
		{
			return Instance.BuildKey(s);
		}
	}
}