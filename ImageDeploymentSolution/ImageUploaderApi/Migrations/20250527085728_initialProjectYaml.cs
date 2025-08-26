using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageUploaderApi.Migrations
{
    /// <inheritdoc />
    public partial class initialProjectYaml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectYamls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    YamlContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectYamls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectYamls_ProjectName",
                table: "ProjectYamls",
                column: "ProjectName");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectYamls_ProjectName_Version",
                table: "ProjectYamls",
                columns: new[] { "ProjectName", "Version" },
                unique: true,
                filter: "[ProjectName] IS NOT NULL AND [Version] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectYamls");
        }
    }
}
