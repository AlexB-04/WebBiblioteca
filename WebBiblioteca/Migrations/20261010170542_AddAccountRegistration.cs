using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBiblioteca.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoLeitor",
                table: "Leitores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Aprovado",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeveAlterarPassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Leitores_CodigoLeitor",
                table: "Leitores",
                column: "CodigoLeitor",
                unique: true,
                filter: "[CodigoLeitor] IS NOT NULL");

            migrationBuilder.Sql(
                "UPDATE [AspNetUsers] SET [Aprovado] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leitores_CodigoLeitor",
                table: "Leitores");

            migrationBuilder.DropColumn(
                name: "CodigoLeitor",
                table: "Leitores");

            migrationBuilder.DropColumn(
                name: "Aprovado",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeveAlterarPassword",
                table: "AspNetUsers");
        }
    }
}
