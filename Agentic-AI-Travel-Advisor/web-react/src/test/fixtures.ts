import type {
  Booking,
  BookingProposal,
  ChatResponse,
  Destination,
  Hotel,
  ReportSummary,
  Room,
  Statistics,
  TravelPackage,
  TravelPackageDetail,
  TravelPlan,
} from '../api/types';

export const ellaHotel: Hotel = {
  id: 3,
  name: 'Ella Gap View Inn',
  address: 'Passara Road',
  city: 'Ella',
  country: 'Sri Lanka',
  description: 'Views of Ella Gap',
  imageUrl: null,
  roomCount: 2,
  approvalStatus: 1,
  minPricePerNight: 8000,
  averageRating: 4.2,
  reviewCount: 5,
};

export const ellaRooms: Room[] = [
  { id: 5, hotelId: 3, name: 'Garden Double', roomType: 'Double', pricePerNight: 8000, capacity: 2, isAvailable: true },
  { id: 6, hotelId: 3, name: 'Family Room', roomType: 'Family', pricePerNight: 12000, capacity: 4, isAvailable: true },
  { id: 9, hotelId: 3, name: 'Closed Suite', roomType: 'Suite', pricePerNight: 30000, capacity: 2, isAvailable: false },
];

export const destinations: Destination[] = [
  { id: 6, name: 'Ella', country: 'Sri Lanka' },
  { id: 7, name: 'Kandy', country: 'Sri Lanka' },
];

export const ellaPackage: TravelPackage = {
  id: 7,
  title: 'Ella Hiking Escape',
  description: 'Two days of trails',
  price: 28000,
  durationDays: 2,
  destinationId: 6,
  destinationName: 'Ella',
  destinationCountry: 'Sri Lanka',
  imageUrl: null,
  activityCount: 2,
  approvalStatus: 1,
  totalPrice: 36000,
  maxTravelers: 10,
  averageRating: null,
  reviewCount: 0,
};

export const ellaPackageDetail: TravelPackageDetail = { ...ellaPackage, activities: [] };

export const summary: ReportSummary = {
  userCount: 120,
  hotelOwnerCount: 8,
  travelAgentCount: 5,
  hotelCount: 14,
  packageCount: 9,
  pendingHotelApprovals: 2,
  pendingPackageApprovals: 1,
  pendingBookings: 6,
  confirmedBookings: 30,
  cancelledBookings: 4,
  completedBookings: 50,
  revenue: 1250000,
};

export const statistics: Statistics = {
  from: '2025-10-01',
  to: '2026-10-01',
  totalBookings: 90,
  bookingsByStatus: { Pending: 6, Confirmed: 30, Cancelled: 4, Completed: 50 },
  revenue: 1250000,
  averageBookingValue: 15625,
  cancellationRate: 0.044,
  totalGuests: 210,
  averageRating: 4.4,
  reviewCount: 37,
  monthly: [{ month: '2026-09', bookings: 12, revenue: 180000 }],
  topListings: [{ id: 3, type: 'Hotel', name: 'Ella Gap View Inn', bookings: 20, revenue: 320000, averageRating: 4.2, reviewCount: 5 }],
};

export const emptyStatistics: Statistics = {
  ...statistics,
  totalBookings: 0,
  bookingsByStatus: {},
  revenue: 0,
  averageBookingValue: 0,
  cancellationRate: 0,
  totalGuests: 0,
  averageRating: null,
  reviewCount: 0,
  monthly: [],
  topListings: [],
};

export const ellaPlan: TravelPlan = {
  destination: 'Ella',
  destinationId: 6,
  country: 'Sri Lanka',
  duration: 3,
  nights: 2,
  startDate: '2026-10-10',
  endDate: '2026-10-12',
  travelers: 2,
  budget: 100000,
  currency: 'LKR',
  estimatedTotal: 76000,
  withinBudget: true,
  costBreakdown: { accommodation: 16000, packages: 56000, transportation: 4000 },
  hotels: [
    {
      hotelId: 3,
      roomId: 5,
      name: 'Ella Gap View Inn',
      roomName: 'Garden Double',
      city: 'Ella',
      pricePerNight: 8000,
      nights: 2,
      rooms: 1,
      capacity: 2,
      totalCost: 16000,
      averageRating: 4.2,
      selected: true,
      availabilityChecked: true,
    },
  ],
  travelPackages: [
    {
      packageId: 7,
      title: 'Ella Hiking Escape',
      durationDays: 2,
      pricePerPerson: 28000,
      totalCost: 56000,
      remainingPlaces: 8,
      averageRating: null,
      selected: true,
      availabilityChecked: true,
    },
  ],
  activities: [],
  transportation: [
    {
      transportationId: 1,
      mode: 'Train',
      from: 'Kandy',
      to: 'Ella',
      departureTime: '08:47',
      durationMinutes: 400,
      pricePerPerson: 2000,
      trips: 1,
      totalCost: 4000,
      selected: true,
    },
  ],
  itinerary: [
    { day: 1, date: '2026-10-10', items: [{ time: '08:47', type: 'transport', title: 'Train Kandy → Ella', description: null }] },
    { day: 2, date: '2026-10-11', items: [{ time: '09:00', type: 'activity', title: 'Little Adam’s Peak hike', description: 'Sunrise hike' }] },
  ],
  assumptions: ['Prices are per person unless stated.'],
  warnings: [],
};

export const proposal: BookingProposal = {
  id: 'prop-123',
  kind: 'room',
  hotelId: 3,
  roomId: 5,
  travelPackageId: null,
  title: 'Ella Gap View Inn · Garden Double',
  checkIn: '2026-10-10',
  checkOut: '2026-10-12',
  guests: 2,
  quotedTotal: 16000,
  currency: 'LKR',
  expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
};

export const pendingBooking: Booking = {
  id: 42,
  userId: 'user-1',
  roomId: 5,
  travelPackageId: null,
  hotelId: 3,
  checkIn: '2026-10-10T00:00:00',
  checkOut: '2026-10-12T00:00:00',
  guests: 2,
  notes: null,
  status: 0,
  totalPrice: 16000,
  createdAt: '2026-10-01T09:00:00Z',
  cancelledAt: null,
  roomName: 'Garden Double',
  hotelName: 'Ella Gap View Inn',
  packageTitle: null,
  userEmail: 'user@example.test',
};

export function chatResponse(overrides: Partial<ChatResponse>): ChatResponse {
  return {
    conversationId: 7,
    message: '',
    status: 'info',
    plan: null,
    pendingBooking: null,
    booking: null,
    mode: 'deterministic',
    agents: [],
    toolCalls: [],
    ...overrides,
  };
}
