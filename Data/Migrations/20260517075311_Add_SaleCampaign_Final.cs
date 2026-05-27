using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopQuanAo.Migrations
{
    /// <inheritdoc />
    public partial class Add_SaleCampaign_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AddColumn<int>(
                name: "SaleCampaignId",
                table: "Product",
                type: "int",
                nullable: true);

            

            migrationBuilder.CreateTable(
                name: "SaleCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleCampaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Product_SaleCampaignId",
                table: "Product",
                column: "SaleCampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_SaleCampaigns_SaleCampaignId",
                table: "Product",
                column: "SaleCampaignId",
                principalTable: "SaleCampaigns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Product_SaleCampaigns_SaleCampaignId",
                table: "Product");

            migrationBuilder.DropTable(
                name: "SaleCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_Product_SaleCampaignId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "SaleCampaignId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Order");
        }
    }
}
