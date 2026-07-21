using System.Globalization;

namespace IqcQms.Infrastructure.Migrations;

public sealed record ApprovedLegacyMasterPlanMapping(
    int MasterPlanId,
    string ExpectedBasic,
    string ApprovedCat,
    string BasicKey,
    string CatKey);

public static class LegacyMasterPlanMigrationSql
{
    public static string Build(params ApprovedLegacyMasterPlanMapping[] mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        if (mappings.Length == 0)
            throw new ArgumentException("At least one approved legacy MasterPlan mapping is required.", nameof(mappings));

        var values = string.Join(",\n", mappings.Select(mapping =>
            $"({mapping.MasterPlanId.ToString(CultureInfo.InvariantCulture)}, {Literal(mapping.ExpectedBasic)}, {Literal(mapping.ApprovedCat)}, {Literal(mapping.BasicKey)}, {Literal(mapping.CatKey)})"));

        return $$"""
            CREATE TEMP TABLE "__ApprovedLegacyMasterPlanCatMappings"
            (
                "MasterPlanId" INTEGER PRIMARY KEY,
                "ExpectedBasic" TEXT NOT NULL,
                "ApprovedCat" TEXT NOT NULL,
                "BasicKey" TEXT NOT NULL,
                "CatKey" TEXT NOT NULL
            );

            INSERT INTO "__ApprovedLegacyMasterPlanCatMappings"
                ("MasterPlanId", "ExpectedBasic", "ApprovedCat", "BasicKey", "CatKey")
            VALUES
                {{values}};

            CREATE TEMP TABLE "__LegacyMasterPlanMappingCheck"
            (
                "InvalidCount" INTEGER NOT NULL
                    CONSTRAINT "Legacy MasterPlan row is missing an approved Cat mapping or its reviewed Basic value changed"
                    CHECK ("InvalidCount" = 0)
            );

            INSERT INTO "__LegacyMasterPlanMappingCheck" ("InvalidCount")
            SELECT COUNT(*)
            FROM "MasterPlans" AS source
            LEFT JOIN "__ApprovedLegacyMasterPlanCatMappings" AS approved
                ON approved."MasterPlanId" = source."Id"
            WHERE approved."MasterPlanId" IS NULL
               OR source."Basic" <> approved."ExpectedBasic"
               OR trim(approved."ApprovedCat") = ''
               OR trim(approved."BasicKey") = ''
               OR trim(approved."CatKey") = '';

            DROP TABLE "__LegacyMasterPlanMappingCheck";

            UPDATE "MasterPlans"
            SET "Cat" = (
                    SELECT approved."ApprovedCat"
                    FROM "__ApprovedLegacyMasterPlanCatMappings" AS approved
                    WHERE approved."MasterPlanId" = "MasterPlans"."Id"
                ),
                "BasicKey" = (
                    SELECT approved."BasicKey"
                    FROM "__ApprovedLegacyMasterPlanCatMappings" AS approved
                    WHERE approved."MasterPlanId" = "MasterPlans"."Id"
                ),
                "CatKey" = (
                    SELECT approved."CatKey"
                    FROM "__ApprovedLegacyMasterPlanCatMappings" AS approved
                    WHERE approved."MasterPlanId" = "MasterPlans"."Id"
                );

            CREATE TEMP TABLE "__LegacyMasterPlanBackfillCheck"
            (
                "InvalidCount" INTEGER NOT NULL
                    CONSTRAINT "Legacy MasterPlan Cat and normalized keys must be fully backfilled"
                    CHECK ("InvalidCount" = 0)
            );

            INSERT INTO "__LegacyMasterPlanBackfillCheck" ("InvalidCount")
            SELECT COUNT(*) FROM "MasterPlans"
            WHERE trim("Cat") = '' OR trim("BasicKey") = '' OR trim("CatKey") = '';

            DROP TABLE "__LegacyMasterPlanBackfillCheck";

            CREATE TEMP TABLE "__LegacyMasterPlanCollisionCheck"
            (
                "CollisionCount" INTEGER NOT NULL
                    CONSTRAINT "Duplicate normalized MasterPlan BasicKey and CatKey require cleanup before migration"
                    CHECK ("CollisionCount" = 0)
            );

            INSERT INTO "__LegacyMasterPlanCollisionCheck" ("CollisionCount")
            SELECT COUNT(*)
            FROM
            (
                SELECT "BasicKey", "CatKey"
                FROM "MasterPlans"
                GROUP BY "BasicKey", "CatKey"
                HAVING COUNT(*) > 1
            );

            DROP TABLE "__LegacyMasterPlanCollisionCheck";
            DROP TABLE "__ApprovedLegacyMasterPlanCatMappings";
            """;
    }

    private static string Literal(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
