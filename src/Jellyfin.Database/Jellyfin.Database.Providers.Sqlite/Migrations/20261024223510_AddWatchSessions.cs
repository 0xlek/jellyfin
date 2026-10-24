using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Database.Providers.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WatchSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Client = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PlaySessionId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    IsFinished = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateLastPlayed = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchSessions_BaseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "BaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchSessions_ItemId",
                table: "WatchSessions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchSessions_UserId_DateLastPlayed",
                table: "WatchSessions",
                columns: new[] { "UserId", "DateLastPlayed" });

            migrationBuilder.CreateIndex(
                name: "IX_WatchSessions_UserId_ItemId_DeviceId",
                table: "WatchSessions",
                columns: new[] { "UserId", "ItemId", "DeviceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WatchSessions");
        }
    }
}
