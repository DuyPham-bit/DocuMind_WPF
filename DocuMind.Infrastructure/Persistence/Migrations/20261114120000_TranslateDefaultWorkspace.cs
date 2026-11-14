using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocuMind.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261114120000_TranslateDefaultWorkspace")]
public partial class TranslateDefaultWorkspace : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Only translate the untouched seeded workspace; never overwrite a user's own name or prompt.
        migrationBuilder.Sql("""
            UPDATE Workspaces
            SET Name = 'My Workspace',
                Description = 'A personal workspace for document research and knowledge discovery.',
                SystemPrompt = 'You are a precise assistant for document research and learning. Keep answers clear, concise, and grounded in the available documents.'
            WHERE Id = '11111111-1111-1111-1111-111111111111'
              AND Name = 'Workspace Mặc Định'
              AND SystemPrompt = 'Bạn là trợ lý học tập và nghiên cứu tài liệu chuẩn xác. Hãy trả lời súc tích, mạch lạc và bám sát tài liệu.';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE Workspaces
            SET Name = 'Workspace Mặc Định',
                Description = 'Không gian làm việc và tra cứu tài liệu thông minh cá nhân.',
                SystemPrompt = 'Bạn là trợ lý học tập và nghiên cứu tài liệu chuẩn xác. Hãy trả lời súc tích, mạch lạc và bám sát tài liệu.'
            WHERE Id = '11111111-1111-1111-1111-111111111111'
              AND Name = 'My Workspace'
              AND SystemPrompt = 'You are a precise assistant for document research and learning. Keep answers clear, concise, and grounded in the available documents.';
            """);
    }
}
