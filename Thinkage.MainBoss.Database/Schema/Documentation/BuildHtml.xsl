<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:xaf="http://www.thinkage.ca/XmlNamespaces/XAF">

<xsl:output method="html" indent="yes" version="1.0" encoding="utf-8"/>
<!--
This set of templates process all the .xafdb database schema definitions
of a program. It recursively processes ALL <merge uri="xxx"> nodes found
within any <database> definitions. It first sets two variables called
"variable-node-list" and "table-node-list" with the node set of all <variable>
and <table> elements. Duplicate <table> elements are not removed from this list.

Generation of html is done with xsl templates with a mode="html"
Generation of a TOC is done with xsl templates with a mode="toc"
-->
<!-- Global Variables -->
<xsl:variable name="variable-node-list" select="xaf:database/xaf:variable|document(xaf:database/xaf:merge/@uri)/xaf:database/xaf:variable"/>
<xsl:variable name="table-node-list" select="xaf:database/xaf:table|document(xaf:database/xaf:merge/@uri)/xaf:database/xaf:table"/>
<xsl:variable name="object-node-list" select="xaf:database/xaf:object|document(xaf:database/xaf:merge/@uri)/xaf:database/xaf:object"/>

<!-- Main line template -->
<xsl:template match="/">
    <xsl:call-template name="GenerateHTML"/>
</xsl:template>

<!-- Generation Templates -->
<xsl:template name="GenerateHTML">
<html>
<title>MainBoss Advanced Database Schema, Version 3.1</title>
<body>
<xsl:call-template name="GenerateTOC"/>
<xsl:call-template name="GenerateTablesTable"/>
<xsl:call-template name="GenerateVariablesTable"/>
<xsl:call-template name="GenerateObjectsTable"/>
</body>
</html>
</xsl:template>

<xsl:template name="GenerateTOC">
<h1>Table of Contents</h1>
<h2><a href="#Variables">Variables</a></h2>
<h2><a href="#Objects">Functions and Triggers</a></h2>
<h2><a href="#Tables">Tables</a></h2>
  <xsl:apply-templates mode="toc" select="$table-node-list">
	<xsl:sort select="@name"/>
  </xsl:apply-templates>
</xsl:template>

<xsl:template mode="toc" match="xaf:table">
<xsl:if test="count(xaf:field)>0">
<h3><a href="#table-{@name}"><xsl:value-of select="@name"/></a></h3>
</xsl:if>
</xsl:template>

<xsl:template name="GenerateTablesTable">
<h1><a name="Tables">Tables</a></h1>
  <xsl:apply-templates mode="html" select="$table-node-list">
	<xsl:sort select="@name"/>
  </xsl:apply-templates>
</xsl:template>

<xsl:template name="GenerateVariablesTable">
<h1><a name="Variables">Variables</a></h1>
<table>
  <xsl:apply-templates mode="html" select="$variable-node-list">
	<xsl:sort select="@name"/>
  </xsl:apply-templates>
</table>
</xsl:template>

<xsl:template name="GenerateObjectsTable">
<h1><a name="Objects">Database functions and triggers</a></h1>
<table>
  <xsl:apply-templates mode="html" select="$object-node-list">
	<xsl:sort select="@name"/>
  </xsl:apply-templates>
</table>
</xsl:template>

<xsl:template name="variable" mode="html" match="xaf:variable">
<xsl:call-template name="DocRow">
  <xsl:with-param name="name" select="@name"/>
  <xsl:with-param name="type" select="@type"/>
  <xsl:with-param name="doc" select="xaf:doc"/>
</xsl:call-template>
</xsl:template>

<xsl:template name="object" mode="html" match="xaf:object">
<xsl:call-template name="DocRow">
  <xsl:with-param name="name" select="@name"/>
  <xsl:with-param name="type" select="@class"/>
  <xsl:with-param name="doc" select="xaf:doc"/>
</xsl:call-template>
</xsl:template>

<xsl:template mode="html" match="xaf:table">
<xsl:if test="count(xaf:field)>0">
<h2><a name="table-{@name}"><xsl:value-of select="@name"/></a></h2>
<p><xsl:value-of select="xaf:doc"/></p>
<table>
  <xsl:apply-templates mode="html" select="xaf:field"/>
</table>
</xsl:if>
</xsl:template>

<xsl:template name="field" mode="html" match="xaf:field">
<xsl:call-template name="DocRow">
  <xsl:with-param name="name" select="@name"/>
  <xsl:with-param name="type" select="@type|@read"/>
  <xsl:with-param name="doc" select="xaf:doc"/>
  <xsl:with-param name="tablelink" select="@link|@base"/>
</xsl:call-template>
</xsl:template>

<!-- output a single row containing the name, type and <doc> contents -->
<xsl:template name="DocRow">
  <xsl:param name="name"/>
  <xsl:param name="type"/>
  <xsl:param name="doc"/>
  <xsl:param name="tablelink" select="''"/>
<tr>
<td class="name">
<xsl:choose>
<xsl:when test="string-length($tablelink)>0">
<a href="#table-{$tablelink}"><xsl:value-of select="$name"/></a>
</xsl:when>
<xsl:otherwise>
<xsl:value-of select="$name"/>
</xsl:otherwise>
</xsl:choose>
</td>
<td class="type">
<xsl:choose>
<xsl:when test="string-length($type)>0">
<xsl:value-of select="$type"/>
</xsl:when>
<xsl:otherwise>
<xsl:text>
*</xsl:text>
</xsl:otherwise>
</xsl:choose>
</td>
<td class="desc">
<xsl:choose>
<xsl:when test="string-length($doc)>0">
<xsl:value-of select="$doc"/>
</xsl:when>
<xsl:otherwise>
<xsl:text>
*</xsl:text>
</xsl:otherwise>
</xsl:choose>
</td>
</tr>
</xsl:template>

</xsl:stylesheet>

