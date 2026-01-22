using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMT.Data.Migrations
{
    /// <inheritdoc />
    public partial class Changed_UserShift_Field_Name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Confirmed",
                table: "UserShifts",
                newName: "Planned");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Planned",
                table: "UserShifts",
                newName: "Confirmed");
        }
    }
}
