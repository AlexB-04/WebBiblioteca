using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBiblioteca.Migrations
{
    /// <inheritdoc />
    public partial class AddEmprestimosGestaoEPenalizacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataEliminacao",
                table: "Emprestimos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Emprestimos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmprestimoAlteracoes",
                columns: table => new
                {
                    IdEmprestimoAlteracao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmprestimo = table.Column<int>(type: "int", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrazoAnterior = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrazoNovo = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmprestimoAlteracoes", x => x.IdEmprestimoAlteracao);
                    table.ForeignKey(
                        name: "FK_EmprestimoAlteracoes_Emprestimos_IdEmprestimo",
                        column: x => x.IdEmprestimo,
                        principalTable: "Emprestimos",
                        principalColumn: "IdEmprestimo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Penalizacoes",
                columns: table => new
                {
                    IdPenalizacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmprestimo = table.Column<int>(type: "int", nullable: false),
                    IdEmprestimoDetalhe = table.Column<int>(type: "int", nullable: false),
                    DiasAtraso = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DataPenalizacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Pago = table.Column<bool>(type: "bit", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penalizacoes", x => x.IdPenalizacao);
                    table.ForeignKey(
                        name: "FK_Penalizacoes_EmprestimoDetalhes_IdEmprestimoDetalhe",
                        column: x => x.IdEmprestimoDetalhe,
                        principalTable: "EmprestimoDetalhes",
                        principalColumn: "IdEmprestimoDetalhe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Penalizacoes_Emprestimos_IdEmprestimo",
                        column: x => x.IdEmprestimo,
                        principalTable: "Emprestimos",
                        principalColumn: "IdEmprestimo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmprestimoAlteracoes_IdEmprestimo",
                table: "EmprestimoAlteracoes",
                column: "IdEmprestimo");

            migrationBuilder.CreateIndex(
                name: "IX_Penalizacoes_IdEmprestimo",
                table: "Penalizacoes",
                column: "IdEmprestimo");

            migrationBuilder.CreateIndex(
                name: "IX_Penalizacoes_IdEmprestimoDetalhe",
                table: "Penalizacoes",
                column: "IdEmprestimoDetalhe",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmprestimoAlteracoes");

            migrationBuilder.DropTable(
                name: "Penalizacoes");

            migrationBuilder.DropColumn(
                name: "DataEliminacao",
                table: "Emprestimos");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Emprestimos");
        }
    }
}
