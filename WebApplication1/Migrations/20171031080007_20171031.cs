using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WebApplication1.Migrations
{
    public partial class _20171031 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Track",
                type: "varchar(800)",
                maxLength: 800,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 400,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Track",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(800)",
                oldMaxLength: 800,
                oldNullable: true);
        }
    }
}
