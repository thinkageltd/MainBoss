using System;
using System.Text;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Summary description for MBAboutForm.
	/// </summary>
	public class MBAboutForm : TblForm<UIPanel> {
		// Name of Teamviewer executable for Help menu references; put here as a common place accessible to MainBoss
		static string TeamviewerFileName = KB.I("TeamViewerQS.exe");
		public static void StartTeamviewer() {
			var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), MBAboutForm.TeamviewerFileName);
			try {
				using (System.Diagnostics.Process.Start(path)) { }
			}
			catch (System.Exception e) {
				Thinkage.Libraries.Exception.AddContext(e, new Thinkage.Libraries.MessageExceptionContext(KB.T(path)));
				throw;
			}
		}
		/// <summary>
		/// Link for the company website is active for the Logo as well.
		/// </summary>
		UILinkDisplay webLink;
		public MBAboutForm(UIFactory uiFactory, MB3Client db, string appName)
			: base(uiFactory, PanelHelper.NewCenterColumnPanel(uiFactory)) {
			Caption = KB.T(Strings.Format(KB.K("About {0}"), appName));
			MyLineBuilder aboutText = new MyLineBuilder(new StringBuilder());

			Version ver = VersionInfo.ProductVersion;
			aboutText.Append(Strings.Format(KB.K("Version {0}.{1}"), ver.Major, ver.Minor));
			aboutText.Append(" ");
			if ((long)ver.Build != 0) {
				aboutText.Append(Strings.Format(KB.K("Update {0}"), ver.Build));
				aboutText.Append(" ");
			}
			aboutText.Append(Strings.Format(KB.K("Revision {0}"), ver.Revision));
			aboutText.AppendLine(Strings.Format(KB.K("Culture {0} ({1})"), Thinkage.Libraries.Application.InstanceCultureInfo.NativeName, Thinkage.Libraries.Application.InstanceCultureInfo.Name));
			if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed) {
				// Determine the root of our application from the UpdateLocation Uri if we have been clickonce deployed
				var webRoot = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.UpdateLocation.GetLeftPart(UriPartial.Authority);
				aboutText.AppendLine(Strings.Format(KB.K("Network Deployed {0}"), webRoot));
			}
			if (db != null) {
				aboutText.AppendLine(Strings.Format(KB.K("Database {0}"), db.ConnectionInfo.DBName));
				aboutText.AppendLine(Strings.Format(KB.K("Database version {0}"), Thinkage.Libraries.Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().VersionHandler.CurrentVersion));
				aboutText.AppendLine(Strings.Format(KB.K("Server {0}"), db.ConnectionInfo.DBServer));
				aboutText.AppendLine(db.DatabaseServerProductIdentification);
			}
			else
				aboutText.AppendLine(KB.K("Not connected to a database").Translate());
			var mixin = Libraries.Application.Instance.QueryInterface<IApplicationWithSingleDatabaseConnection>();
			if (mixin != null)
				aboutText.AppendLine(mixin.LicenseMessage);

			aboutText.AppendLine(
				((System.Reflection.AssemblyCopyrightAttribute)
				(Attribute.GetCustomAttribute(
				System.Reflection.Assembly.GetCallingAssembly(),
				typeof(System.Reflection.AssemblyCopyrightAttribute)))).Copyright);
			uint desiredWidth = aboutText.MaxLineLength * 10 / 8;
			TextSizePreference preference = new TextSizePreference() { DefaultWidthInCharacters = desiredWidth, MinWidthInCharacters = desiredWidth, MaxPreferredWidthInCharacters = desiredWidth };
			UITextDisplay tbAbout = uiFactory.CreateTextDisplay(new StringTypeInfo(0, null, aboutText.LineCount + 1, false, false, false).GetTypeFormatter(Thinkage.Libraries.Application.InstanceCultureInfo), null, false, preference);

			tbAbout.HorizontalScrollbar = UIScollbarVisibility.Never;
			tbAbout.VerticalScrollbar = UIScollbarVisibility.Never;
			tbAbout.HorizontalAlignment = System.Drawing.StringAlignment.Center;
			tbAbout.Value = aboutText.ToString();

			webLink = uiFactory.CreateLinkDisplay(new StringTypeInfo(0, 20, 0, false, false, false).GetTypeFormatter(Thinkage.Libraries.Application.InstanceCultureInfo),
				(tf, value) => Thinkage.Libraries.Xml.UriUtilities.SetImpliedProtocol(tf.Format(value), "http://"));
			webLink.Value = KB.I("www.mainboss.com");
			webLink.HorizontalAlignment = System.Drawing.StringAlignment.Center;
			webLink.VerticalAlignment = System.Drawing.StringAlignment.Center;

			UIImageDisplay pbLogo = uiFactory.CreateImageDisplay();
			pbLogo.Value = Images.MainBossLogo;
			pbLogo.ImagePosition = UIImagePosition.CenterClipped;
			pbLogo.Click += (sender, args) => { webLink.ClickLink(); };

			var btnOK = uiFactory.CreateButton(KB.K("OK"), UIDialogResult.Cancel);
			this.CancelButton = btnOK;

			// Arrange the tab order so the textBox is not the initial selected control with focus.
			//			btnOK.TabIndex = 0;
			//			tbAbout.TabIndex = 1;
			//			pbLogo.TabStop = false;
			//			webLink.TabStop = true;
			//			webLink.TabIndex = 2;

			FormContents.Add(pbLogo);
			FormContents.Add(tbAbout);
			FormContents.Add(webLink);
			FormContents.Add(btnOK);
		}

		private class MyLineBuilder {
			private StringBuilder sb;
			public MyLineBuilder(StringBuilder sb) {
				this.sb = sb;
			}
			public void Append(string s) {
				string[] lines = s.Split('\n');
				for (int l = 0; l < lines.Length; l++) {
					string line = lines[l];
					if (l > 0) {
						line = line.TrimStart();
						sb.AppendLine();
					}
					if (l < lines.Length - 1)
						line = line.TrimEnd();
					uint lineLength = (uint)line.Length;
					if (lineLength > MaxLineLength)
						MaxLineLength = lineLength;
					sb.Append(line);
				}
				LineCount += (uint)lines.Length - 1;
			}
			public void AppendLine(string s) {
				sb.AppendLine();
				sb.AppendLine();
				LineCount += 2;
				Append(s);
			}
			public override string ToString() {
				return sb.ToString();
			}
			public uint LineCount = 0;
			public uint MaxLineLength = 0;
		}
	}

}
