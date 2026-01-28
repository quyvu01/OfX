using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service1.OtherContextMigrations
{
    /// <inheritdoc />
    public partial class OtherDbContextInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberAddresses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProvinceId = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberAddresses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberAddresses");
        }
    }
}
