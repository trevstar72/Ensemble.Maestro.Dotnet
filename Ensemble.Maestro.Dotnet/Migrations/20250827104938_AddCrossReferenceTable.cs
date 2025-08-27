using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensemble.Maestro.Dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddCrossReferenceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrossReferences",
                columns: table => new
                {
                    PrimaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SqlId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Neo4jId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ElasticsearchId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IntegrityHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastIntegrityCheck = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossReferences", x => x.PrimaryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrossReferences_CreatedAt",
                table: "CrossReferences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrossReferences_EntityType",
                table: "CrossReferences",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_CrossReferences_EntityType_Status",
                table: "CrossReferences",
                columns: new[] { "EntityType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrossReferences_LastIntegrityCheck",
                table: "CrossReferences",
                column: "LastIntegrityCheck");

            migrationBuilder.CreateIndex(
                name: "IX_CrossReferences_Status",
                table: "CrossReferences",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrossReferences");
        }
    }
}
