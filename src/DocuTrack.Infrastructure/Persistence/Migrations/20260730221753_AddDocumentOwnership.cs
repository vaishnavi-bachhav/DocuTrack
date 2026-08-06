using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocuTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedByUserId",
                table: "Documents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_LastModifiedByUserId",
                table: "Documents",
                column: "LastModifiedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_CreatedByUserId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_LastModifiedByUserId",
                table: "Documents");
        }
    }
}
