<?xml version="1.0"?>
<xsl:stylesheet version="1.0"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
	exclude-result-prefixes="wix">
	<xsl:output method="xml" encoding="UTF-8" indent="yes"/>
	<xsl:template match="@*|*">
		<xsl:copy>
			<!-- Copy Attributes-->
			<xsl:apply-templates select="@*"/>
			<!-- Copy rest -->
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>
	<xsl:template match="Component">
		<xsl:apply-templates select="@*"/>
		<!-- Copy rest -->
		<xsl:apply-templates/>
	</xsl:template>
	<xsl:template match="/wix:Wix/wix:Fragment/wix:DirectoryRef">
		<xsl:apply-templates select="@*"/>
		<xsl:apply-templates select="*"/>
	</xsl:template>
	<xsl:template match="/wix:Wix/wix:Fragment">
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates select="*"/>
	</xsl:template>
	<xsl:template match="/wix:Wix">
		<Include xmlns="http://schemas.microsoft.com/wix/2006/wi">
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates select="*"/>
		</Include>
	</xsl:template>
</xsl:stylesheet>