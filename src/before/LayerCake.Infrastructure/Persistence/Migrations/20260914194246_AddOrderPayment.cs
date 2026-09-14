using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerCake.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentAuthorizationId",
                schema: "before",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                schema: "before",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentAuthorizationId",
                schema: "before",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                schema: "before",
                table: "Orders");
        }
    }
}
