-- Travel Advisor — Initial Schema Reference
-- Source of truth: EF Core migration in shared/Infrastructure/Migrations/

-- Application roles (custom RBAC)
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL UNIQUE
);

-- ASP.NET Identity users (extends with profile + role FK)
-- Table name: AspNetUsers (see EF migration for full Identity schema)

-- Domain tables
CREATE TABLE IF NOT EXISTS "Destinations" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Country" TEXT NOT NULL,
    "Description" TEXT,
    "ImageUrl" TEXT
);

CREATE TABLE IF NOT EXISTS "Hotels" (
    "Id" SERIAL PRIMARY KEY,
    "OwnerId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id"),
    "Name" TEXT NOT NULL,
    "Address" TEXT NOT NULL,
    "City" TEXT NOT NULL,
    "Country" TEXT NOT NULL,
    "Description" TEXT
);

CREATE TABLE IF NOT EXISTS "Rooms" (
    "Id" SERIAL PRIMARY KEY,
    "HotelId" INTEGER NOT NULL REFERENCES "Hotels"("Id"),
    "Name" TEXT NOT NULL,
    "RoomType" TEXT NOT NULL,
    "PricePerNight" NUMERIC NOT NULL,
    "Capacity" INTEGER NOT NULL,
    "IsAvailable" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS "TravelPackages" (
    "Id" SERIAL PRIMARY KEY,
    "AgentId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id"),
    "DestinationId" INTEGER NOT NULL REFERENCES "Destinations"("Id"),
    "Title" TEXT NOT NULL,
    "Description" TEXT,
    "Price" NUMERIC NOT NULL,
    "DurationDays" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "Bookings" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id"),
    "RoomId" INTEGER REFERENCES "Rooms"("Id"),
    "TravelPackageId" INTEGER REFERENCES "TravelPackages"("Id"),
    "CheckIn" TIMESTAMP NOT NULL,
    "CheckOut" TIMESTAMP NOT NULL,
    "Status" INTEGER NOT NULL,
    "TotalPrice" NUMERIC NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "CK_Bookings_RoomOrPackage" CHECK (
        ("RoomId" IS NOT NULL AND "TravelPackageId" IS NULL) OR
        ("RoomId" IS NULL AND "TravelPackageId" IS NOT NULL)
    )
);

CREATE TABLE IF NOT EXISTS "TravelPreferences" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL UNIQUE REFERENCES "AspNetUsers"("Id"),
    "BudgetMin" NUMERIC,
    "BudgetMax" NUMERIC,
    "PreferredClimate" TEXT,
    "Interests" TEXT
);

CREATE TABLE IF NOT EXISTS "Itineraries" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id"),
    "Title" TEXT NOT NULL,
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL,
    "Status" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "ItineraryItems" (
    "Id" SERIAL PRIMARY KEY,
    "ItineraryId" INTEGER NOT NULL REFERENCES "Itineraries"("Id") ON DELETE CASCADE,
    "DayNumber" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT,
    "StartTime" INTERVAL,
    "SortOrder" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "AIConversations" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id"),
    "Title" TEXT NOT NULL,
    "Messages" TEXT NOT NULL DEFAULT '[]',
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NOT NULL
);
