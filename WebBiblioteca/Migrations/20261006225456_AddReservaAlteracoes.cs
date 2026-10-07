using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBiblioteca.Migrations
{
    /// <inheritdoc />
    public partial class AddReservaAlteracoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservaAlteracoes",
                columns: table => new
                {
                    IdReservaAlteracao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdReserva = table.Column<int>(type: "int", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdLivroAnterior = table.Column<int>(type: "int", nullable: false),
                    LivroAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdLivroNovo = table.Column<int>(type: "int", nullable: false),
                    LivroNovo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrdemAnterior = table.Column<int>(type: "int", nullable: false),
                    OrdemNova = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaAlteracoes", x => x.IdReservaAlteracao);
                    table.ForeignKey(
                        name: "FK_ReservaAlteracoes_Reservas_IdReserva",
                        column: x => x.IdReserva,
                        principalTable: "Reservas",
                        principalColumn: "IdReserva",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaAlteracoes_IdReserva",
                table: "ReservaAlteracoes",
                column: "IdReserva");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservaAlteracoes");
        }
    }
}
