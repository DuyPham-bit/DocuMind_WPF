using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocuMind.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionedRagAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "Workspaces",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsEmbeddingActive",
                table: "ProviderSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                table: "DocumentChunks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentVersionId",
                table: "DocumentChunks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "ChatMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "ChatMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "ChatMessages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "ChatMessages",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    SourcePath = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SourceHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmbeddingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: false),
                    Dimension = table.Column<int>(type: "INTEGER", nullable: false),
                    Normalization = table.Column<string>(type: "TEXT", nullable: false),
                    PipelineVersion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbeddingProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    EmbeddingProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_EmbeddingProfiles_EmbeddingProfileId",
                        column: x => x.EmbeddingProfileId,
                        principalTable: "EmbeddingProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "ConversationId",
                value: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettings_IsActive",
                table: "ProviderSettings",
                column: "IsActive",
                unique: true,
                filter: "\"IsActive\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSettings_IsEmbeddingActive",
                table: "ProviderSettings",
                column: "IsEmbeddingActive",
                unique: true,
                filter: "\"IsEmbeddingActive\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentVersionId_ChunkIndex",
                table: "DocumentChunks",
                columns: new[] { "DocumentVersionId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_WorkspaceId_ConversationId_Timestamp",
                table: "ChatMessages",
                columns: new[] { "WorkspaceId", "ConversationId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_WorkspaceId_SourcePath",
                table: "Documents",
                columns: new[] { "WorkspaceId", "SourcePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId",
                table: "DocumentVersions",
                column: "DocumentId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_EmbeddingProfileId",
                table: "DocumentVersions",
                column: "EmbeddingProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EmbeddingProfiles_Provider_Model_Endpoint_Dimension_Normalization_PipelineVersion",
                table: "EmbeddingProfiles",
                columns: new[] { "Provider", "Model", "Endpoint", "Dimension", "Normalization", "PipelineVersion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_DocumentVersions_DocumentVersionId",
                table: "DocumentChunks",
                column: "DocumentVersionId",
                principalTable: "DocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_DocumentVersions_DocumentVersionId",
                table: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "EmbeddingProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettings_IsActive",
                table: "ProviderSettings");

            migrationBuilder.DropIndex(
                name: "IX_ProviderSettings_IsEmbeddingActive",
                table: "ProviderSettings");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_DocumentVersionId_ChunkIndex",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_WorkspaceId_ConversationId_Timestamp",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "IsEmbeddingActive",
                table: "ProviderSettings");

            migrationBuilder.DropColumn(
                name: "ChunkIndex",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "DocumentVersionId",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "ChatMessages");
        }
    }
}
