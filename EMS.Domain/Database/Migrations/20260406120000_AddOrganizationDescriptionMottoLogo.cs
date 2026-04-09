using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Domain.Database.Migrations;

/// <inheritdoc />
public partial class AddOrganizationDescriptionMottoLogo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            schema: "org",
            table: "Organizations",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LogoRelativePath",
            schema: "org",
            table: "Organizations",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Motto",
            schema: "org",
            table: "Organizations",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Description",
            schema: "org",
            table: "Organizations");

        migrationBuilder.DropColumn(
            name: "LogoRelativePath",
            schema: "org",
            table: "Organizations");

        migrationBuilder.DropColumn(
            name: "Motto",
            schema: "org",
            table: "Organizations");
    }
}
