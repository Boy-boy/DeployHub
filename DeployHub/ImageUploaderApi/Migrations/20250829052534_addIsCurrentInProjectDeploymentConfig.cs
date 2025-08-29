using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageUploaderApi.Migrations
{
    /// <inheritdoc />
    public partial class addIsCurrentInProjectDeploymentConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "ProjectDeploymentConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "ProjectDeploymentConfigs");
        }
    }
}
