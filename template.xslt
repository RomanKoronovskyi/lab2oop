<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output method="html" indent="no" />

	<xsl:template match="libraryDatabase">
		<html>
			<body>
				<h2>Кафедральна бібліотека</h2>
				<table border="2">
					<tr>
						<th>Назва</th>
						<th>Інформація</th>
						<th>Категорія</th>
						<th>Автори</th>
					</tr>
					<xsl:apply-templates select="book" />
				</table>
			</body>
		</html>
	</xsl:template>

	<xsl:template match="book">
		<tr>
			<td>
				<xsl:value-of select="@BK_NAME" />
			</td>
			<td>
				<xsl:value-of select="@BK_INFO" />
			</td>
			<td>
				<xsl:value-of select="@DC_NAME" />
			</td>
			<td>
				<xsl:for-each select="author">
					<div>
						<xsl:value-of select="@AU_NAME" />
					</div>
				</xsl:for-each>
			</td>
		</tr>
	</xsl:template>
</xsl:stylesheet>
