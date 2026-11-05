using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocuMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveLegacyIndexData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve legacy chunks, but mark their unknown embedding space as Failed until explicit re-index.
            migrationBuilder.Sql("""
                INSERT INTO Documents (Id, WorkspaceId, DisplayName, SourcePath, SourceHash)
                SELECT MIN(Id), WorkspaceId, FileName, 'legacy:' || FileName, '' FROM DocumentChunks GROUP BY WorkspaceId, FileName;
                INSERT INTO DocumentVersions (Id, DocumentId, Status, EmbeddingProfileId, IndexedAt)
                SELECT d.Id, d.Id, 2, NULL, MIN(c.CreatedAt) FROM Documents d
                JOIN DocumentChunks c ON c.WorkspaceId = d.WorkspaceId AND c.FileName = d.DisplayName GROUP BY d.Id;
                UPDATE DocumentChunks SET
                    ChunkIndex = (SELECT COUNT(*) FROM DocumentChunks other WHERE other.WorkspaceId = DocumentChunks.WorkspaceId AND other.FileName = DocumentChunks.FileName AND other.Id < DocumentChunks.Id),
                    DocumentVersionId = (SELECT d.Id FROM Documents d WHERE d.WorkspaceId = DocumentChunks.WorkspaceId AND d.DisplayName = DocumentChunks.FileName);
                UPDATE Workspaces SET ConversationId = Id WHERE ConversationId = '00000000-0000-0000-0000-000000000000';
                UPDATE ChatMessages SET ConversationId = (SELECT w.ConversationId FROM Workspaces w WHERE w.Id = ChatMessages.WorkspaceId);
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
