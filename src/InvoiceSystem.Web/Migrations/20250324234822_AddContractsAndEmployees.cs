using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddContractsAndEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Employees_EmployeeId",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Employees_EmployeeId",
                table: "Invoices",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Employees_EmployeeId",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Employees_EmployeeId",
                table: "Invoices",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
