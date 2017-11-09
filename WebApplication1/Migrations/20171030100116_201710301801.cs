using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WebApplication1.Migrations
{
    public partial class _201710301801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Track",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 200,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Track",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(400)",
                oldMaxLength: 400,
                oldNullable: true);
        }
    }
}
