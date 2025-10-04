using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace missedpay.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class MissedPayInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Migrated = table.Column<string>(type: "text", nullable: true),
                    Authorisation = table.Column<string>(type: "text", nullable: false),
                    Credentials = table.Column<string>(type: "text", nullable: true),
                    Connection_Name = table.Column<string>(type: "text", nullable: false),
                    Connection_Logo = table.Column<string>(type: "text", nullable: false),
                    Connection_Id = table.Column<string>(type: "text", nullable: true),
                    Connection_ConnectionType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FormattedAccount = table.Column<string>(type: "text", nullable: true),
                    Meta = table.Column<string>(type: "jsonb", nullable: false),
                    Refreshed_Balance = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Refreshed_Meta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Refreshed_Transactions = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Refreshed_Party = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Balance_Current = table.Column<decimal>(type: "numeric", nullable: false),
                    Balance_Available = table.Column<decimal>(type: "numeric", nullable: true),
                    Balance_Limit = table.Column<decimal>(type: "numeric", nullable: true),
                    Balance_Overdrawn = table.Column<bool>(type: "boolean", nullable: true),
                    Balance_Currency = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Attributes = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "jsonb", nullable: true),
                    Merchant = table.Column<string>(type: "jsonb", nullable: true),
                    Meta = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
