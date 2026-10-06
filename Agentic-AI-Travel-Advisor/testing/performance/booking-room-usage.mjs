// Read-only check after k6 booking.js: per room of a k6 hotel, how many bookings were made, by how many
// travelers, and how many are still active. Usage: node booking-room-usage.mjs [hotelId]  (default: newest k6 hotel)
// Needs E2E_DB_CONNECTION (or the E2E_DB_* variables) as for the Playwright E2E database checks.
import { dbConfigFromEnv, connectDb } from '../../web-react/e2e/db.ts';

const db = await connectDb(dbConfigFromEnv());
try {
  const hotelId = process.argv[2];
  const hotel = (await db.query(
    hotelId
      ? `SELECT "Id", "Name" FROM "Hotels" WHERE "Id" = $1`
      : `SELECT "Id", "Name" FROM "Hotels" WHERE "Name" LIKE 'k6 Hotel %' ORDER BY "Id" DESC LIMIT 1`,
    hotelId ? [Number(hotelId)] : [],
  )).rows[0];
  if (!hotel) throw new Error('No k6 hotel found.');
  console.log(`hotel ${hotel.Id} ${hotel.Name}`);
  const rows = (await db.query(`
    SELECT r."Name" AS room, COUNT(b."Id") AS bookings, COUNT(DISTINCT b."UserId") AS travelers,
           COUNT(b."Id") FILTER (WHERE b."Status" IN (0, 1)) AS active
    FROM "Rooms" r LEFT JOIN "Bookings" b ON b."RoomId" = r."Id"
    WHERE r."HotelId" = $1 GROUP BY r."Name", r."Id" ORDER BY r."Id"`, [hotel.Id])).rows;
  for (const row of rows) console.log(`${row.room}: bookings=${row.bookings} distinct travelers=${row.travelers} still active=${row.active}`);
  const total = rows.reduce((sum, row) => sum + Number(row.bookings), 0);
  console.log(`total bookings=${total}, rooms with bookings=${rows.filter((row) => Number(row.bookings) > 0).length} of ${rows.length}`);
} finally {
  await db.end();
}
