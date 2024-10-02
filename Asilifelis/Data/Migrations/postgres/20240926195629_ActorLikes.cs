using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Asilifelis.Data.Migrations.postgres;

/// <inheritdoc />
public partial class ActorLikes : Migration {
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder) {
		migrationBuilder.AddColumn<string>(
			name: "Uri",
			table: "Actors",
			type: "character varying(256)",
			maxLength: 256,
			nullable: true);

		migrationBuilder.CreateTable(
			name: "ActorLikes",
			columns: table => new {
				Id = table.Column<int>(type: "integer", nullable: false)
					.Annotation("Npgsql:ValueGenerationStrategy",
						NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
				LikedActorId = table.Column<Guid>(type: "uuid", nullable: false),
				LikesNoteId = table.Column<Guid>(type: "uuid", nullable: false)
			},
			constraints: table => {
				table.PrimaryKey("PK_ActorLikes", x => x.Id);
				table.ForeignKey(
					name: "FK_ActorLikes_Actors_LikedActorId",
					column: x => x.LikedActorId,
					principalTable: "Actors",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_ActorLikes_Notes_LikesNoteId",
					column: x => x.LikesNoteId,
					principalTable: "Notes",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_ActorLikes_LikedActorId",
			table: "ActorLikes",
			column: "LikedActorId");

		migrationBuilder.CreateIndex(
			name: "IX_ActorLikes_LikesNoteId",
			table: "ActorLikes",
			column: "LikesNoteId");
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder) {
		migrationBuilder.DropTable(
			name: "ActorLikes");

		migrationBuilder.DropColumn(
			name: "Uri",
			table: "Actors");
	}
}