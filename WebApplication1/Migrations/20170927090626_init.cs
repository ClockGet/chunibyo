using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WebApplication1.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Dt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LoginDt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PassWord = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    UserName = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "PlayList",
                columns: table => new
                {
                    PlayListId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CoverImgUrl = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayList", x => x.PlayListId);
                    table.ForeignKey(
                        name: "FK_PlayList_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Track",
                columns: table => new
                {
                    TrackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Album = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    AlbumId = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    Artist = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    ArtistId = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    ImgUrl = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    PlayListId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    SourceUrl = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    Url = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Track", x => x.TrackId);
                    table.ForeignKey(
                        name: "FK_Track_PlayList_PlayListId",
                        column: x => x.PlayListId,
                        principalTable: "PlayList",
                        principalColumn: "PlayListId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayList_UserId",
                table: "PlayList",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Track_PlayListId",
                table: "Track",
                column: "PlayListId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Track");

            migrationBuilder.DropTable(
                name: "PlayList");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
