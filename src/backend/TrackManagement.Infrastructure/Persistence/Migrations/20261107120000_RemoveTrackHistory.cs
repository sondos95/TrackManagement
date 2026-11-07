using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackManagement.Infrastructure.Persistence.Migrations
{
    public partial class RemoveTrackHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TrackHistory");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PreviousStatus = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ResultingStatus = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AssignedDspIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackHistory_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackHistory_TrackId",
                table: "TrackHistory",
                column: "TrackId");
        }
    }
}
