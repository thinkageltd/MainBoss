using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Thinkage.Libraries.CommandLineParsing;
using Thinkage.Libraries.Xml;
using Thinkage.MainBoss.Database;
using Thinkage.Libraries;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.IO;
using System.Security.Cryptography.X509Certificates;
//
// A note on SIGNATURE_PASSES_SCHEMA_VALIDATION:
// the MS validating reader in .NET 2 rejects values which the schema declares as the built-in type 'integer' but which cannot be parsed into
// a System.Decimal because the value is too large. This is incorrect, since the XML schema type 'integer' has no intrinsic range limits.
// Unfortunately, the X509SerialNumebr element of the Signature is declared as integer, but the serial number of Thinkage's code-signing
// signature is about 1.5e38, while the max for Decimal is around 7.9e28.
// Thus XML signed with our signature will not schema-validate using the MX validating reader.
// As a work-around, when SIGNATURE_PASSES_SCHEMA_VALIDATION is NOT defined, the code initially reads the document without validation,
// does minimal ad hoc checking, and if there is a signature, varifies it and (assuming it passed) strips it out of the document. Then
// the code re-reads the in-memory document (with no signature) using a validating reader.
//
namespace Thinkage.MainBoss.MBUtility {
	internal class ScriptVerb {
		public class Definition : UtilityVerbWithDatabaseDefinition {
			public Definition()
				: base() {
				Add(ScriptName = new StringValueOption("Script", KB.K("Specify the name of the script file to be run").Translate(), true));
			}
			public readonly StringValueOption ScriptName;
			public override string Verb {
				[return: Thinkage.Libraries.Translation.Invariant]
				get {
					return "Script";
				}
			}
			public override void RunVerb() {
				new ScriptVerb(this).Run();
			}
		}
		private ScriptVerb(Definition options) {
			Options = options;
		}
		private readonly Definition Options;
		public static readonly string XMLNamespace = KB.I("http://www.thinkage.ca/XmlNamespaces/MBUtility/Script");
		private void Run() {
			// Read the script file; validate it by checking the signature.
			// We use the "Enveloped Signature" model.
			bool isSigned = false;
			// ReadXmlDocument strips off XML comments so a script that begins with XML comments may fail to signature-check. Similarly there may be
			// inconsistent settings on preserving white space, also critical to checking the signature. As a result we read the document ourselves.
			XmlReaderSettings settings = null;
#if SIGNATURE_PASSES_SCHEMA_VALIDATION
			settings = GetSchemaValidatingXmlReaderSettings();
#else
			settings = new XmlReaderSettings {
				ValidationType = ValidationType.None
			};
#endif

			TXmlDocument doc = new TXmlDocument {
				PreserveWhitespace = true   // This setting must match from when the file was signed.
			};
			Stream scriptStream = new FileStream(Options.ScriptName.Value, FileMode.Open, FileAccess.Read);
			using (XmlReader reader = XmlReader.Create(scriptStream, settings))
				doc.Load(reader);

#if !SIGNATURE_PASSES_SCHEMA_VALIDATION
			// In lieu of validated initial reading of the file, do a minimum check here so we can safely determine if the file is signed.
			if (doc.DocumentElement == null || doc.DocumentElement.ChildNodes.Count == 0)
				throw new GeneralException(KB.K("The root element in script '{0}' is missing or has no content"), Options.ScriptName.Value);
#endif
			// If the doc adheres to the schema (or the non-schema ad-hoc checks passed), the last child of the root element will be a signature.
			// If so, verify it, otherwise we have a non-signed script.
			if (doc.DocumentElement.LastChild.Name == KB.I("Signature") && doc.DocumentElement.LastChild.NamespaceURI == KB.I("http://www.w3.org/2000/09/xmldsig#")) {
				isSigned = true;

				// Create a new SignedXml object and pass it the XML document instance.
				SignedXml signedXml = new SignedXml(doc);
				// Create a reference describing the virtual document to be verified against the signature
				Reference reference = new Reference {
					Uri = ""
				};
				// Add an enveloped transformation to the reference. This transformation locates the appropriate section of the document to be signed.
				XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
				reference.AddTransform(env);
				// Add the reference to the SignedXml object.
				signedXml.AddReference(reference);

				// Load the signature node.
				signedXml.LoadXml((XmlElement)doc.DocumentElement.LastChild);

				// Someone may have signed the file with their own certificate's private key,
				// and attached Thinkage's certificate as well as their own.
				// Thus the signature checks OK but not because Thinkage produced it.
				// So we locate Thinkage's certificate specifically and explicitly use it to verify the signing of the file.
				X509Certificate2 thinkageCertificate = null;
				foreach (KeyInfoClause kic in signedXml.KeyInfo) {
					KeyInfoX509Data kic509 = kic as KeyInfoX509Data;
					if (kic509 == null)
						continue;
					for (int i = kic509.Certificates.Count; --i >= 0; ) {
						// TODO: Perhaps there is some other field we should check since we may get different serial numbers for renewal certificates.
						if (kic509.Certificates[i] is X509Certificate2 cert && cert.Issuer == Thinkage.Libraries.Security.ThinkageCertificateIssuer
							&& string.Compare(cert.GetSerialNumberString(), Thinkage.Libraries.Security.ThinkageCertificateSerialNumber, true) == 0)
							thinkageCertificate = cert;
					}
				}
				if (thinkageCertificate == null)
					throw new GeneralException(KB.K("No certificate issued to Thinkage Ltd. was found in the script <Signature> element"));

				// Check the signature and return the result.
				if (!signedXml.CheckSignature(thinkageCertificate, false))
					throw new GeneralException(KB.K("The script signature does not verify against the Thinkage Ltd. certificate in the <Signature> element or this certificate is invalid"));

#if !SIGNATURE_PASSES_SCHEMA_VALIDATION
				// Having verified the signature we now remove its document element so the rest of the document can be schema-validated.
				doc.DocumentElement.RemoveChild(doc.DocumentElement.LastChild);
				// Make sure there wasn't another signature element there, since the schema allows for the signature element (so other document validators that work
				// properly can be used on a signed script file).
				if (doc.DocumentElement.ChildNodes.Count > 0
					&& doc.DocumentElement.LastChild.Name == KB.I("Signature") && doc.DocumentElement.LastChild.NamespaceURI == KB.I("http://www.w3.org/2000/09/xmldsig#"))
					throw new GeneralException(KB.K("Script '{0}' contains multiple <Signature> elements"), Options.ScriptName.Value);
#endif
			}
			else
				isSigned = false;
			if (isSigned) // USE the variable to avoid warning
			{
			}
#if !SIGNATURE_PASSES_SCHEMA_VALIDATION
			// Now we re-read the in-memory doc (whose signature if any has been removed) with a validating reader and discard the original to
			// check that the content follows the schema.
			// For now we wrap a StringReader around doc.OuterText but that seems to me like overkill. I expect there is a way of making an
			// XmlReader derivation that takes an in-memory XmlNode tree as "input".
			// It also could mess up any attempts at line number reporting, especially if the error happens after the removed <Signature> element (unlikely)
			using (XmlReader reader = XmlReader.Create(new StringReader(doc.OuterXml), GetSchemaValidatingXmlReaderSettings(), Options.ScriptName.Value)) {
				doc = new TXmlDocument();
				doc.Load(reader);
			}

#endif

			// Handle LicenseKey options from the command line based on what the script says.
			MB3Client.ConnectionDefinition connect = MB3Client.OptionSupport.ResolveSavedOrganization(Options.OrganizationName, Options.DataBaseServer, Options.DataBaseName, out string oName);
		}
		private XmlReaderSettings GetSchemaValidatingXmlReaderSettings() {
			XmlSchemaSet schemaCollection = new XmlSchemaSet();
			XmlResolver schemaResolver = new ManifestXmlResolver(Assembly.GetExecutingAssembly());

			XmlReaderSettings schemaReaderSettings = new XmlReaderSettings {
				XmlResolver = schemaResolver
			};
			XmlReader schemaReader = XmlReader.Create("manifest://localhost/Thinkage/MainBoss/MBUtility/Script/ScriptSchema.xsd", schemaReaderSettings);
			schemaCollection.XmlResolver = schemaResolver;
			schemaCollection.ValidationEventHandler += delegate(object sender, ValidationEventArgs e) {
				throw new GeneralException(e.Exception, KB.K("Error in XML Schema")).WithContext(Statics.GetReaderLocation((XmlReader)sender));
			};
			schemaCollection.Add(null, schemaReader);

			XmlReaderSettings result = new XmlReaderSettings();
			result.Schemas.Add(schemaCollection);
			result.ValidationType = ValidationType.Schema;
			result.ValidationEventHandler += delegate(object sender, ValidationEventArgs e) {
				throw new GeneralException(e.Exception, KB.K("Error in XML Document")).WithContext(Statics.GetReaderLocation((XmlReader)sender));
			};

			return result;
		}
	}
}