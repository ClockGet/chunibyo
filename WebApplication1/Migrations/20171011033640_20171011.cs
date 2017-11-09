using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WebApplication1.Migrations
{
    public partial class _20171011 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PassWord",
                table: "User",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 20,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PassWord",
                table: "User",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}
