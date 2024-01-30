<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:x="http://www.thinkage.ca/XmlNamespaces/XAF"
	xmlns:strings="strings"
	exclude-result-prefixes="strings x ">
	<xsl:output method="xml" indent="yes" version="2.0" encoding="utf-8"/>

	<xsl:template match="/">
		<root>
			<xsl:apply-templates select="x:rightset/x:role"/>
			<xsl:apply-templates select="x:rightset/x:role/x:description"/>
			<xsl:apply-templates select="x:rightset/x:role/x:comment"/>
		</root>
	</xsl:template>

	<xsl:template match="x:role">
		<data>
			<xsl:attribute name="name">Thinkage.MainBoss.Database&#167;<xsl:value-of select="@name"/>_Name</xsl:attribute>
			<value>
				<xsl:value-of select="@name"/>
			</value>
		</data>
	</xsl:template>
	<xsl:template match="x:description">
		<data>
			<xsl:attribute name="name">Thinkage.MainBoss.Database&#167;<xsl:value-of select="../@name"/>_Desc</xsl:attribute>
			<value>
				<xsl:value-of select="strings:Substitute(., '¯', '')"/>
			</value>
		</data>
	</xsl:template>
	<xsl:template match="x:comment">
		<data>
			<xsl:attribute name="name">Thinkage.MainBoss.Database&#167;<xsl:value-of select="../@name"/>_Comment</xsl:attribute>
			<value>
				<xsl:value-of select="strings:Substitute(., '¯', '')"/>
			</value>
		</data>
	</xsl:template>
</xsl:stylesheet>
