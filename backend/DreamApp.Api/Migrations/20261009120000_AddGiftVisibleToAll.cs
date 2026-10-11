using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftVisibleToAll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every existing gift starts as "visible to everyone" (default true)...
            migrationBuilder.AddColumn<bool>(
                name: "VisibleToAll",
                table: "Gifts",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            // ...except the ones that were already restricted to specific friends
            // (they had at least one GiftVisibility row): those keep their list.
            // Before this migration "no rows" meant "everyone"; from now on an
            // empty list means "nobody", so the flag preserves the old behaviour.
            migrationBuilder.Sql(
                "UPDATE \"Gifts\" SET \"VisibleToAll\" = 0 WHERE \"Id\" IN (SELECT \"GiftId\" FROM \"GiftVisibilities\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old model had no flag, so there is nothing to convert back:
            // gifts shared with nobody simply lose that distinction.
            migrationBuilder.DropColumn(
                name: "VisibleToAll",
                table: "Gifts");
        }
    }
}
