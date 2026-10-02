using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBiblioteca.Migrations
{
    /// <inheritdoc />
    public partial class AddEmprestimos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Emprestimos",
                columns: table => new
                {
                    IdEmprestimo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdLeitor = table.Column<int>(type: "int", nullable: false),
                    DataEmprestimo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrazoDevolucao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emprestimos", x => x.IdEmprestimo);
                    table.ForeignKey(
                        name: "FK_Emprestimos_Leitores_IdLeitor",
                        column: x => x.IdLeitor,
                        principalTable: "Leitores",
                        principalColumn: "IdLeitor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmprestimoDetalhes",
                columns: table => new
                {
                    IdEmprestimoDetalhe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmprestimo = table.Column<int>(type: "int", nullable: false),
                    IdLivro = table.Column<int>(type: "int", nullable: false),
                    DataDevolucao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmprestimoDetalhes", x => x.IdEmprestimoDetalhe);
                    table.ForeignKey(
                        name: "FK_EmprestimoDetalhes_Emprestimos_IdEmprestimo",
                        column: x => x.IdEmprestimo,
                        principalTable: "Emprestimos",
                        principalColumn: "IdEmprestimo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmprestimoDetalhes_Livros_IdLivro",
                        column: x => x.IdLivro,
                        principalTable: "Livros",
                        principalColumn: "IdLivro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmprestimoDetalhes_IdEmprestimo",
                table: "EmprestimoDetalhes",
                column: "IdEmprestimo");

            migrationBuilder.CreateIndex(
                name: "IX_EmprestimoDetalhes_IdLivro",
                table: "EmprestimoDetalhes",
                column: "IdLivro");

            migrationBuilder.CreateIndex(
                name: "IX_Emprestimos_IdLeitor",
                table: "Emprestimos",
                column: "IdLeitor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmprestimoDetalhes");

            migrationBuilder.DropTable(
                name: "Emprestimos");
        }
    }
}
