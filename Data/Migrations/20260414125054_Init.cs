using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopQuanAo.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductReview_Product_ProductId",
                table: "ProductReview");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductReview",
                table: "ProductReview");

            migrationBuilder.RenameTable(
                name: "ProductReview",
                newName: "ProductReviews");

            migrationBuilder.RenameIndex(
                name: "IX_ProductReview_ProductId",
                table: "ProductReviews",
                newName: "IX_ProductReviews_ProductId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ProductReviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "ProductReviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductReviews",
                table: "ProductReviews",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_Product_ProductId",
                table: "ProductReviews",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_Product_ProductId",
                table: "ProductReviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductReviews",
                table: "ProductReviews");

            migrationBuilder.RenameTable(
                name: "ProductReviews",
                newName: "ProductReview");

            migrationBuilder.RenameIndex(
                name: "IX_ProductReviews_ProductId",
                table: "ProductReview",
                newName: "IX_ProductReview_ProductId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ProductReview",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "ProductReview",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductReview",
                table: "ProductReview",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReview_Product_ProductId",
                table: "ProductReview",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
