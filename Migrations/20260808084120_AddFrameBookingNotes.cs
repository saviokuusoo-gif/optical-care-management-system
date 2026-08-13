using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace optical_care_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameBookingNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FrameBookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FrameBookings");
        }
    }
}
