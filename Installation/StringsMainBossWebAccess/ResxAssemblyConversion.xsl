<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:t="http://www.thinkage.ca/XmlNamespaces/TranslatableStrings"
	xmlns:strings="strings">
	<xsl:output method="xml" indent="yes" version="2.0" encoding="utf-8"/>

<xsl:param name="assemblyName"/>
<xsl:include href="ResxStandardHeader.xsl"/>
<xsl:include href="ResxOutputLine.xsl"/>

<xsl:template match="/">
<root>
<xsl:call-template name="ResxStandardHeader"/>
<xsl:apply-templates select="t:ContextStrings/t:assembly[starts-with(@name,$assemblyName)]/t:string"/>
</root>
</xsl:template>

<xsl:template match="t:string">
	<xsl:call-template name="ResxOutputLine"/>
</xsl:template>
</xsl:stylesheet>
 
