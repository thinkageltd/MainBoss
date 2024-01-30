<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:t="http://www.thinkage.ca/XmlNamespaces/TranslatableStrings"
	xmlns:strings="strings">
	<xsl:output method="xml" indent="yes" version="2.0" encoding="utf-8"/>


<xsl:template match="/">
<root>
	<xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema">		<xsd:element name="root">
			<xsd:complexType>
				<xsd:choice maxOccurs="unbounded">
					<xsd:element name="helptopic" type="xsd:string"/>
				</xsd:choice>
			</xsd:complexType>
		</xsd:element>
	</xsd:schema>
<xsl:apply-templates select="t:ContextStrings/t:assembly/t:string[contains(@context,'.HelpTopic')]"/>
</root>
</xsl:template>

<xsl:template match="t:string">
	<helptopic><xsl:value-of select="."/></helptopic>
</xsl:template>
</xsl:stylesheet>
 
