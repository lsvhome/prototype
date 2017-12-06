<xsl:stylesheet version="1.0"
            xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
            xmlns:msxsl="urn:schemas-microsoft-com:xslt"
            xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
            exclude-result-prefixes="wix"
            xmlns:my="my:my">

  <xsl:output method="xml" indent="yes" />

  <xsl:strip-space elements="*"/>

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="/wix:Wix/wix:Fragment/wix:ComponentGroup[@Id='ComponentGroupFexSync']/wix:Component[@Directory!='INSTALLDIR']/@Directory">
    <xsl:attribute name="Directory"  >
      <xsl:text>_</xsl:text>
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>

  <xsl:template match="/wix:Wix/wix:Fragment/wix:DirectoryRef[@Id='INSTALLDIR']/wix:Directory/@Id">
    <xsl:attribute name="Id"  >
      <xsl:text>_</xsl:text>
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>

</xsl:stylesheet>