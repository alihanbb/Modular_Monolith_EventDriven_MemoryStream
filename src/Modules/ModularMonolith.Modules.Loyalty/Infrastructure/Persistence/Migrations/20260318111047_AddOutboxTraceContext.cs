using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModularMonolith.Modules.Loyalty.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxTraceContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceContext",
                schema: "loyalty",
                table: "outbox_messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceContext",
                schema: "loyalty",
                table: "outbox_messages");
        }
    }
}
