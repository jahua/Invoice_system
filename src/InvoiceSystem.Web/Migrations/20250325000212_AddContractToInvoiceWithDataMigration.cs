using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddContractToInvoiceWithDataMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ContractId column as nullable first
            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "Invoices",
                type: "integer",
                nullable: true);

            // Update existing invoices to link to the first contract of each employee
            migrationBuilder.Sql(@"
                UPDATE ""Invoices"" i
                SET ""ContractId"" = (
                    SELECT c.""Id""
                    FROM ""Contracts"" c
                    WHERE c.""EmployeeId"" = i.""EmployeeId""
                    ORDER BY c.""StartDate""
                    LIMIT 1
                )");

            // Make ContractId non-nullable after data migration
            migrationBuilder.AlterColumn<int>(
                name: "ContractId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Add foreign key
            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ContractId",
                table: "Invoices",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Contracts_ContractId",
                table: "Invoices",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Contracts_ContractId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ContractId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "Invoices");
        }
    }
}
