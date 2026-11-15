using EMS.Domain.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations
{
    /// <summary>
    /// Adds the manager/HR decision comment and an optimistic concurrency token to leave requests.
    /// Both changes are additive: existing rows get a NULL comment and SQL Server populates the rowversion.
    /// Hand-written (no Designer file); regenerate with <c>dotnet ef migrations add</c> locally if preferred.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261115100000_AddLeaveRequestDecisionFields")]
    public partial class AddLeaveRequestDecisionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DecisionComment",
                schema: "org",
                table: "LeaveRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "org",
                table: "LeaveRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "org",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "DecisionComment",
                schema: "org",
                table: "LeaveRequests");
        }
    }
}
