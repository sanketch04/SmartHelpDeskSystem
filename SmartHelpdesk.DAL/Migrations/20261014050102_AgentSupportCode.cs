using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHelpdesk.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AgentSupportCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupportAgentCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupportAgentCode",
                table: "AspNetUsers");
        }
    }
}
