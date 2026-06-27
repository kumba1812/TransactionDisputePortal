using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TransactionDisputePortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndDisputeLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    transaction_uid = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Merchant = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Disputes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    refund_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    transaction_id_fk = table.Column<int>(type: "integer", nullable: false),
                    locked_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    locked_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disputes_Transactions_transaction_id_fk",
                        column: x => x.transaction_id_fk,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "Amount", "Category", "created_at", "customer_id", "Description", "Merchant", "Status", "transaction_date", "transaction_uid" },
                values: new object[,]
                {
                    { 1, 1250.50m, "ATM Withdrawal", new DateTime(2026, 6, 17, 0, 0, 0, 0, DateTimeKind.Utc), 4, "ATM Withdrawal", "FNB ATM - Sandton", 0, new DateTime(2026, 6, 17, 0, 0, 0, 0, DateTimeKind.Utc), "TXN20260604001" },
                    { 2, 899.99m, "Insurance", new DateTime(2026, 6, 22, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Monthly Insurance Premium", "Old Mutual Insurance", 0, new DateTime(2026, 6, 22, 0, 0, 0, 0, DateTimeKind.Utc), "TXN20260609002" },
                    { 3, 450.00m, "Utilities", new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Utility Payment", "Eskom - Electricity", 0, new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Utc), "TXN20260612003" },
                    { 4, 2000.00m, "Wire Transfer", new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), 4, "International Wire Transfer", "Standard Chartered Bank - USA", 0, new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Utc), "TXN20260520004" },
                    { 5, 325.50m, "Retail", new DateTime(2026, 6, 26, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Card Purchase", "Pick n Pay - Westgate", 0, new DateTime(2026, 6, 26, 0, 0, 0, 0, DateTimeKind.Utc), "TXN20260614005" },
                    { 6, 150.00m, "Mobile Services", new DateTime(2026, 6, 27, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Mobile Top-Up", "Vodacom South Africa", 0, new DateTime(2026, 6, 27, 0, 0, 0, 0, DateTimeKind.Utc), "TXN20260615006" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "FullName", "IsActive", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin User", true, "AQAAAAIAAYagAAAAEHAs/RIAc73AdUmTE7pE4RVjRetn/52Beuma5Na3CJMmGHUdapIb+5sGC9YsQtAJpQ==", "Admin", "admin" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Banker One", true, "AQAAAAIAAYagAAAAEOHyOmsU4MC6cbFkFAa+waEQdy+yk4gTy5+rYWpjb4czzqDtnl2zJo1VzcwUCl66Hw==", "Banker", "banker" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Banker Two", true, "AQAAAAIAAYagAAAAEIyDrUA+t6w5xyCy4Ukf/48KkJEIfkud1OztXcUdbN4vWmU5dNE3AhWH0PdEBjLF6g==", "Banker", "banker2" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Client User", true, "AQAAAAIAAYagAAAAEOVwhOITRao7qU1Cf2fQ1xXychGjsufGSLJ5LDdkzoDg5AmskD5O1mdUyW3J8xXznQ==", "Client", "client" },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ReadOnly User", true, "AQAAAAIAAYagAAAAEKZeA/2pDqEoc2E/79npcng/NBYPhpwZeLWQd0PnaxFEkATi7CgrnHuvyVFCH8We1Q==", "ReadOnly", "readonly" }
                });

            migrationBuilder.InsertData(
                table: "Disputes",
                columns: new[] { "Id", "created_at", "customer_id", "Description", "locked_at", "locked_by_name", "locked_by_user_id", "Reason", "refund_amount", "resolution_notes", "resolved_at", "Status", "transaction_id", "transaction_id_fk" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), 4, "I did not authorize this ATM withdrawal", null, null, null, "Unauthorized", 1250.50m, null, null, 1, 1, 1 },
                    { 2, new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Wire transfer was charged twice", null, null, null, "Incorrect Amount", 2000.00m, "Duplicate charge refunded to account", new DateTime(2026, 6, 12, 0, 0, 0, 0, DateTimeKind.Utc), 2, 4, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_transaction_id_fk",
                table: "Disputes",
                column: "transaction_id_fk");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Disputes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
