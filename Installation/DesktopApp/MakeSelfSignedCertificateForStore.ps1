#This makes a signing certificate for the Windows Store submission for Publisher = "CN=7D069FE8-6815-4B7C-88A9-87E6294070E8"
$PublisherStore = "CN=7D069FE8-6815-4B7C-88A9-87E6294070E8"
New-SelfSignedCertificate -Type Custom -Subject "$PublisherStore" -KeyUsage DigitalSignature -FriendlyName "ThinkageLtd.MainBoss" -CertStoreLocation "Cert:\LocalMachine\My"