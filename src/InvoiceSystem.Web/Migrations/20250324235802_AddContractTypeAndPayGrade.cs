using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddContractTypeAndPayGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContractType",
                table: "Contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PayGrade",
                table: "Contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PayGrade",
                table: "Contracts");
        }
    }
}
