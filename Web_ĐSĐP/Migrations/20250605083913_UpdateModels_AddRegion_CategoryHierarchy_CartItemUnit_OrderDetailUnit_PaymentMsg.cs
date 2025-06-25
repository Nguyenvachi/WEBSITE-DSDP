using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Sport.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels_AddRegion_CategoryHierarchy_CartItemUnit_OrderDetailUnit_PaymentMsg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Size",
                table: "OrderDetails",
                newName: "Unit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "OrderDetails",
                newName: "Size");
        }
    }
}
