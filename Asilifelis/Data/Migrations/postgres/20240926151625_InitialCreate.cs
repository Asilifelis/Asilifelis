using System;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Asilifelis.Data.Migrations.postgres;

// Add-Migration InitialCreate -OutputDir Data/Migrations/postgres -Project Asilifelis -StartupProject Asilifelis -Context ApplicationContext -Args "ConnectionStrings:postgres=Host=localhost;Port=5432;AllowAnonymousConnections=true"
// Remove-Migration -force -Project Asilifelis -StartupProject Asilifelis -Context ApplicationContext -Args "ConnectionStrings:postgres=Host=localhost;Port=5432;AllowAnonymousConnections=true"

/// <inheritdoc />
public partial class InitialCreate : Migration {
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder) {
		migrationBuilder.AlterDatabase()
			.Annotation("Npgsql:CollationDefinition:default_case_insensitive",
				"und-u-ks-level1,und-u-ks-level1,icu,False");

		migrationBuilder.CreateTable(
			name: "PublicKeyCredentialDescriptor",
			columns: table => new {
				Id = table.Column<byte[]>(type: "bytea", nullable: false),
				Type = table.Column<int>(type: "integer", nullable: false),
				Transports = table.Column<AuthenticatorTransport[]>(type: "jsonb", nullable: true)
			},
			constraints: table => { table.PrimaryKey("PK_PublicKeyCredentialDescriptor", x => x.Id); });

		migrationBuilder.CreateTable(
			name: "UserIdentity",
			columns: table => new {
				Id = table.Column<byte[]>(type: "bytea", nullable: false),
				Counter = table.Column<long>(type: "bigint", nullable: false)
			},
			constraints: table => { table.PrimaryKey("PK_UserIdentity", x => x.Id); });

		migrationBuilder.CreateTable(
			name: "Actors",
			columns: table => new {
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				IdentityId = table.Column<byte[]>(type: "bytea", nullable: true),
				Type = table.Column<int>(type: "integer", nullable: false),
				Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false,
					collation: "default_case_insensitive"),
				DisplayName = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false)
			},
			constraints: table => {
				table.PrimaryKey("PK_Actors", x => x.Id);
				table.ForeignKey(
					name: "FK_Actors_UserIdentity_IdentityId",
					column: x => x.IdentityId,
					principalTable: "UserIdentity",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "Credential",
			columns: table => new {
				Id = table.Column<int>(type: "integer", nullable: false)
					.Annotation("Npgsql:ValueGenerationStrategy",
						NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
				UserHandle = table.Column<byte[]>(type: "bytea", nullable: false),
				PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
				DescriptorId = table.Column<byte[]>(type: "bytea", nullable: false),
				UserIdentityId = table.Column<byte[]>(type: "bytea", nullable: false)
			},
			constraints: table => {
				table.PrimaryKey("PK_Credential", x => x.Id);
				table.ForeignKey(
					name: "FK_Credential_PublicKeyCredentialDescriptor_DescriptorId",
					column: x => x.DescriptorId,
					principalTable: "PublicKeyCredentialDescriptor",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_Credential_UserIdentity_UserIdentityId",
					column: x => x.UserIdentityId,
					principalTable: "UserIdentity",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "Notes",
			columns: table => new {
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
				Content = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
				PublishDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
			},
			constraints: table => {
				table.PrimaryKey("PK_Notes", x => x.Id);
				table.ForeignKey(
					name: "FK_Notes_Actors_AuthorId",
					column: x => x.AuthorId,
					principalTable: "Actors",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_Actors_IdentityId",
			table: "Actors",
			column: "IdentityId");

		migrationBuilder.CreateIndex(
			name: "IX_Credential_DescriptorId",
			table: "Credential",
			column: "DescriptorId");

		migrationBuilder.CreateIndex(
			name: "IX_Credential_UserIdentityId",
			table: "Credential",
			column: "UserIdentityId");

		migrationBuilder.CreateIndex(
			name: "IX_Notes_AuthorId",
			table: "Notes",
			column: "AuthorId");
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder) {
		migrationBuilder.DropTable(
			name: "Credential");

		migrationBuilder.DropTable(
			name: "Notes");

		migrationBuilder.DropTable(
			name: "PublicKeyCredentialDescriptor");

		migrationBuilder.DropTable(
			name: "Actors");

		migrationBuilder.DropTable(
			name: "UserIdentity");
	}
}