-- Seed application roles (also applied automatically by DbSeeder on API startup)
INSERT INTO "Roles" ("Name") VALUES
    ('USER'),
    ('HOTEL_OWNER'),
    ('TRAVEL_AGENT'),
    ('ADMIN')
ON CONFLICT DO NOTHING;

-- Demo admin/owner/agent users are created by DbSeeder via ASP.NET Identity:
--   admin@traveladvisor.com / Admin@123  (ADMIN)
--   owner@traveladvisor.com / Owner@123  (HOTEL_OWNER)
--   agent@traveladvisor.com / Agent@123  (TRAVEL_AGENT)
