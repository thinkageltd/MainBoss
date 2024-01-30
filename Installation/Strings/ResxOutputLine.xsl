<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:t="http://www.thinkage.ca/XmlNamespaces/TranslatableStrings"
	xmlns:strings="strings">
	<xsl:output method="xml" indent="yes" version="2.0" encoding="utf-8"/>

<xsl:template name="ResxOutputLine">
	<xsl:if test="not(boolean(@Translatable)) or string(@Translatable) != 'false'">
	<data>
		<xsl:attribute name="name"><xsl:value-of select="@context"/>&#167;<xsl:value-of select="."/></xsl:attribute>
			<value><xsl:value-of select="strings:Substitute(., '¯', '')"/></value>
	</data>
	</xsl:if>
</xsl:template>
</xsl:stylesheet>
