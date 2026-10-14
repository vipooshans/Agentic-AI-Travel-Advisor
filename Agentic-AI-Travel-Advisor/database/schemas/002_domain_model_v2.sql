START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" DROP CONSTRAINT "FK_Bookings_Rooms_RoomId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" DROP CONSTRAINT "FK_Bookings_TravelPackages_TravelPackageId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" DROP CONSTRAINT "FK_TravelPackages_Destinations_DestinationId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    DROP INDEX "IX_TravelPackages_DestinationId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    DROP INDEX "IX_Rooms_HotelId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    DROP INDEX "IX_Bookings_RoomId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    DROP INDEX "IX_Bookings_TravelPackageId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    DROP INDEX "IX_Bookings_UserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    DROP INDEX "EmailIndex";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    DROP INDEX "IX_AIConversations_UserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE EXTENSION IF NOT EXISTS btree_gist;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ALTER COLUMN "PreferredClimate" TYPE character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ALTER COLUMN "Interests" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ALTER COLUMN "BudgetMin" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ALTER COLUMN "BudgetMax" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ADD "AccommodationPreference" character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ADD "TransportPreference" character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ALTER COLUMN "Title" TYPE character varying(150);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ALTER COLUMN "Price" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ALTER COLUMN "ImageUrl" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ALTER COLUMN "Description" TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ADD "MaxTravelers" integer NOT NULL DEFAULT 10;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Rooms" ALTER COLUMN "RoomType" TYPE character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Rooms" ALTER COLUMN "PricePerNight" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Rooms" ALTER COLUMN "Name" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Rooms" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Rooms" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Roles" ALTER COLUMN "Name" TYPE character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ALTER COLUMN "Title" TYPE character varying(150);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ALTER COLUMN "Price" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ALTER COLUMN "Description" TYPE character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ADD "Category" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "ItineraryItems" ALTER COLUMN "Title" TYPE character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "ItineraryItems" ALTER COLUMN "Description" TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "ItineraryItems" ADD "EstimatedCost" numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "ItineraryItems" ADD "ItemType" character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ALTER COLUMN "Title" TYPE character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ALTER COLUMN "Summary" TYPE character varying(4000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ALTER COLUMN "EstimatedCost" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD "Budget" numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD "ConversationId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD "Travelers" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ALTER COLUMN "Name" TYPE character varying(150);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ALTER COLUMN "ImageUrl" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ALTER COLUMN "Description" TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ALTER COLUMN "Country" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ALTER COLUMN "City" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ALTER COLUMN "Address" TYPE character varying(250);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Hotels" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Destinations" ALTER COLUMN "Name" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Destinations" ALTER COLUMN "ImageUrl" TYPE character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Destinations" ALTER COLUMN "Description" TYPE character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Destinations" ALTER COLUMN "Country" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Destinations" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Destinations" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ALTER COLUMN "TotalPrice" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD "CancelledAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD "Guests" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD "Notes" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "AspNetUsers" ALTER COLUMN "LastName" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "AspNetUsers" ALTER COLUMN "FirstName" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "AspNetUsers" ADD "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "AIConversations" ALTER COLUMN "Title" TYPE character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE TABLE "AIRecommendations" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "ConversationId" integer NOT NULL,
        "UserId" text NOT NULL,
        "ItemType" integer NOT NULL,
        "HotelId" integer,
        "RoomId" integer,
        "TravelPackageId" integer,
        "TransportationId" integer,
        "DestinationId" integer,
        "Title" character varying(200) NOT NULL,
        "EstimatedCost" numeric(18,2) NOT NULL,
        "Score" double precision NOT NULL,
        "Reason" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AIRecommendations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AIRecommendations_AIConversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "AIConversations" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AIRecommendations_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE TABLE "Payments" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "BookingId" integer NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Method" integer NOT NULL,
        "Status" integer NOT NULL,
        "TransactionReference" character varying(64) NOT NULL,
        "PaidAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Payments_Amount" CHECK ("Amount" > 0),
        CONSTRAINT "FK_Payments_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE TABLE "Reviews" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "UserId" text NOT NULL,
        "BookingId" integer NOT NULL,
        "HotelId" integer,
        "TravelPackageId" integer,
        "Rating" integer NOT NULL,
        "Comment" character varying(2000),
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Reviews_Rating" CHECK ("Rating" BETWEEN 1 AND 5),
        CONSTRAINT "CK_Reviews_Target" CHECK (("HotelId" IS NOT NULL AND "TravelPackageId" IS NULL) OR ("HotelId" IS NULL AND "TravelPackageId" IS NOT NULL)),
        CONSTRAINT "FK_Reviews_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Reviews_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Reviews_Hotels_HotelId" FOREIGN KEY ("HotelId") REFERENCES "Hotels" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Reviews_TravelPackages_TravelPackageId" FOREIGN KEY ("TravelPackageId") REFERENCES "TravelPackages" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE TABLE "RoomAvailability" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "RoomId" integer NOT NULL,
        "Date" date NOT NULL,
        "IsBlocked" boolean NOT NULL,
        "PriceOverride" numeric(18,2),
        "Note" character varying(250),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RoomAvailability" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_RoomAvailability_Price" CHECK ("PriceOverride" IS NULL OR "PriceOverride" >= 0),
        CONSTRAINT "FK_RoomAvailability_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE TABLE "SystemSettings" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "Key" character varying(100) NOT NULL,
        "Value" character varying(1000) NOT NULL,
        "Description" character varying(500),
        "UpdatedBy" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SystemSettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE TABLE "Transportation" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "ProviderId" text NOT NULL,
        "TravelPackageId" integer,
        "DestinationId" integer,
        "Mode" integer NOT NULL,
        "FromLocation" character varying(150) NOT NULL,
        "ToLocation" character varying(150) NOT NULL,
        "DepartureTime" interval,
        "DurationMinutes" integer NOT NULL,
        "PricePerPerson" numeric(18,2) NOT NULL,
        "Capacity" integer NOT NULL,
        "Description" character varying(1000),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Transportation" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Transportation_Capacity" CHECK ("Capacity" > 0),
        CONSTRAINT "CK_Transportation_Duration" CHECK ("DurationMinutes" > 0),
        CONSTRAINT "CK_Transportation_Price" CHECK ("PricePerPerson" >= 0),
        CONSTRAINT "FK_Transportation_AspNetUsers_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Transportation_Destinations_DestinationId" FOREIGN KEY ("DestinationId") REFERENCES "Destinations" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Transportation_TravelPackages_TravelPackageId" FOREIGN KEY ("TravelPackageId") REFERENCES "TravelPackages" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE TABLE "UserProfiles" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "UserId" text NOT NULL,
        "PhoneNumber" character varying(32),
        "Nationality" character varying(64),
        "DateOfBirth" date,
        "AvatarUrl" character varying(500),
        "Bio" character varying(1000),
        "PreferredCurrency" character varying(3) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserProfiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserProfiles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPreferences" ADD CONSTRAINT "CK_TravelPreferences_Budget" CHECK ("BudgetMin" IS NULL OR "BudgetMax" IS NULL OR "BudgetMin" <= "BudgetMax");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_TravelPackages_DestinationId_ApprovalStatus" ON "TravelPackages" ("DestinationId", "ApprovalStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ADD CONSTRAINT "CK_TravelPackages_Duration" CHECK ("DurationDays" > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ADD CONSTRAINT "CK_TravelPackages_MaxTravelers" CHECK ("MaxTravelers" > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ADD CONSTRAINT "CK_TravelPackages_Price" CHECK ("Price" >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "IX_Rooms_HotelId_Name" ON "Rooms" ("HotelId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Rooms" ADD CONSTRAINT "CK_Rooms_Capacity" CHECK ("Capacity" > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Rooms" ADD CONSTRAINT "CK_Rooms_Price" CHECK ("PricePerNight" >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ADD CONSTRAINT "CK_PackageActivities_Day" CHECK ("DayNumber" > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "PackageActivities" ADD CONSTRAINT "CK_PackageActivities_Price" CHECK ("Price" >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Itineraries_ConversationId" ON "Itineraries" ("ConversationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD CONSTRAINT "CK_Itineraries_Dates" CHECK ("EndDate" >= "StartDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD CONSTRAINT "CK_Itineraries_Travelers" CHECK ("Travelers" > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Hotels_ApprovalStatus" ON "Hotels" ("ApprovalStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Hotels_City" ON "Hotels" ("City");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "IX_Destinations_Name_Country" ON "Destinations" ("Name", "Country");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Bookings_RoomId_CheckIn_CheckOut" ON "Bookings" ("RoomId", "CheckIn", "CheckOut");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Bookings_TravelPackageId_CheckIn" ON "Bookings" ("TravelPackageId", "CheckIn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Bookings_UserId_CreatedAt" ON "Bookings" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "CK_Bookings_Dates" CHECK ("CheckOut" > "CheckIn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "CK_Bookings_Guests" CHECK ("Guests" > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "CK_Bookings_TotalPrice" CHECK ("TotalPrice" >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "UX_AspNetUsers_NormalizedEmail" ON "AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_AIConversations_UserId_UpdatedAt" ON "AIConversations" ("UserId", "UpdatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_AIRecommendations_ConversationId" ON "AIRecommendations" ("ConversationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_AIRecommendations_UserId" ON "AIRecommendations" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Payments_BookingId" ON "Payments" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "IX_Payments_TransactionReference" ON "Payments" ("TransactionReference");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "IX_Reviews_BookingId" ON "Reviews" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Reviews_HotelId" ON "Reviews" ("HotelId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Reviews_TravelPackageId" ON "Reviews" ("TravelPackageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Reviews_UserId" ON "Reviews" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "IX_RoomAvailability_RoomId_Date" ON "RoomAvailability" ("RoomId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "IX_SystemSettings_Key" ON "SystemSettings" ("Key");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Transportation_DestinationId" ON "Transportation" ("DestinationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Transportation_ProviderId" ON "Transportation" ("ProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE INDEX "IX_Transportation_TravelPackageId" ON "Transportation" ("TravelPackageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    CREATE UNIQUE INDEX "IX_UserProfiles_UserId" ON "UserProfiles" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "FK_Bookings_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "FK_Bookings_TravelPackages_TravelPackageId" FOREIGN KEY ("TravelPackageId") REFERENCES "TravelPackages" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Itineraries" ADD CONSTRAINT "FK_Itineraries_AIConversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "AIConversations" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "TravelPackages" ADD CONSTRAINT "FK_TravelPackages_Destinations_DestinationId" FOREIGN KEY ("DestinationId") REFERENCES "Destinations" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_Room_NoOverlap"
    EXCLUDE USING gist ("RoomId" WITH =, tstzrange("CheckIn", "CheckOut", '[)') WITH &&)
    WHERE ("Status" <> 2 AND "RoomId" IS NOT NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001105903_AddDomainModelV2') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261001105903_AddDomainModelV2', '8.0.11');
    END IF;
END $EF$;
COMMIT;

