using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace starterapi.Migrations
{
    /// <inheritdoc />
    public partial class deviceregistraions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegisteredDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegisteredDeviceId = table.Column<int>(type: "int", nullable: false),
                    TopicName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceSubscriptions_RegisteredDevices_RegisteredDeviceId",
                        column: x => x.RegisteredDeviceId,
                        principalTable: "RegisteredDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSubscriptions_RegisteredDeviceId",
                table: "DeviceSubscriptions",
                column: "RegisteredDeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceSubscriptions");

            migrationBuilder.DropTable(
                name: "RegisteredDevices");
        }
    }
}
