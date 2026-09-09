using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationProject.Migrations
{
    /// <inheritdoc />
    public partial class AddE1PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_E1s_ConnectedE1Id_StmId_E1Number'
              AND object_id = OBJECT_ID('dbo.E1s')
        )
        CREATE INDEX IX_E1s_ConnectedE1Id_StmId_E1Number
        ON dbo.E1s
        (
            ConnectedE1Id,
            StmId,
            E1Number
        );
        """);

            migrationBuilder.Sql("""
        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_E1s_CrossConnectionState_StmId_E1Number'
              AND object_id = OBJECT_ID('dbo.E1s')
        )
        CREATE INDEX IX_E1s_CrossConnectionState_StmId_E1Number
        ON dbo.E1s
        (
            CrossConnectionState,
            StmId,
            E1Number
        );
        """);

            migrationBuilder.Sql("""
        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_E1s_PathId_NotNull'
              AND object_id = OBJECT_ID('dbo.E1s')
        )
        CREATE INDEX IX_E1s_PathId_NotNull
        ON dbo.E1s (PathId)
        WHERE PathId IS NOT NULL;
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        IF EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_E1s_ConnectedE1Id_StmId_E1Number'
              AND object_id = OBJECT_ID('dbo.E1s')
        )
        DROP INDEX IX_E1s_ConnectedE1Id_StmId_E1Number
        ON dbo.E1s;
        """);

            migrationBuilder.Sql("""
        IF EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_E1s_CrossConnectionState_StmId_E1Number'
              AND object_id = OBJECT_ID('dbo.E1s')
        )
        DROP INDEX IX_E1s_CrossConnectionState_StmId_E1Number
        ON dbo.E1s;
        """);

            migrationBuilder.Sql("""
        IF EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_E1s_PathId_NotNull'
              AND object_id = OBJECT_ID('dbo.E1s')
        )
        DROP INDEX IX_E1s_PathId_NotNull
        ON dbo.E1s;
        """);
        }
    }
}
