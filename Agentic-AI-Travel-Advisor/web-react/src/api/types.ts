// Shapes of the JSON returned by the ASP.NET Core API (camelCase). Dates arrive as ISO strings.
import type {
  ApprovalStatus,
  BookingStatus,
  PaymentMethod,
  PaymentStatus,
  RecommendationType,
  ReviewStatus,
  Role,
  TransportMode,
} from './enums';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
  isActive: boolean;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface CreateStaffUserRequest extends RegisterRequest {
  role: Role;
}

export interface UserProfile {
  phoneNumber?: string | null;
  nationality?: string | null;
  dateOfBirth?: string | null;
  avatarUrl?: string | null;
  bio?: string | null;
  preferredCurrency: string;
}

export interface TravelPreferences {
  budgetMin?: number | null;
  budgetMax?: number | null;
  preferredClimate?: string | null;
  interests?: string | null;
  accommodationPreference?: string | null;
  transportPreference?: string | null;
}

export interface Destination {
  id: number;
  name: string;
  country: string;
  description?: string | null;
  imageUrl?: string | null;
  packageCount?: number;
}

export interface SaveDestinationRequest {
  name: string;
  country: string;
  description?: string | null;
  imageUrl?: string | null;
}

export interface Hotel {
  id: number;
  name: string;
  address: string;
  city: string;
  country: string;
  description?: string | null;
  imageUrl?: string | null;
  roomCount: number;
  approvalStatus: ApprovalStatus;
  minPricePerNight?: number | null;
  averageRating?: number | null;
  reviewCount: number;
}

export interface Room {
  id: number;
  hotelId: number;
  name: string;
  roomType: string;
  pricePerNight: number;
  capacity: number;
  isAvailable: boolean;
}

export interface HotelDetail extends Hotel {
  rooms: Room[];
}

export interface SaveHotelRequest {
  name: string;
  address: string;
  city: string;
  country: string;
  description?: string | null;
  imageUrl?: string | null;
}

export interface SaveRoomRequest {
  name: string;
  roomType: string;
  pricePerNight: number;
  capacity: number;
}

export interface HotelSearchQuery {
  q?: string;
  city?: string;
  country?: string;
  approvalStatus?: ApprovalStatus;
  maxPrice?: number;
  guests?: number;
}

export interface TravelPackage {
  id: number;
  title: string;
  description?: string | null;
  price: number;
  durationDays: number;
  destinationId: number;
  destinationName: string;
  destinationCountry: string;
  imageUrl?: string | null;
  activityCount: number;
  approvalStatus: ApprovalStatus;
  totalPrice: number;
  maxTravelers: number;
  averageRating?: number | null;
  reviewCount: number;
}

export interface PackageActivity {
  id: number;
  travelPackageId: number;
  title: string;
  description?: string | null;
  dayNumber: number;
  price: number;
  sortOrder: number;
}

export interface TravelPackageDetail extends TravelPackage {
  activities: PackageActivity[];
}

export interface SavePackageRequest {
  destinationId: number;
  title: string;
  description?: string | null;
  price: number;
  durationDays: number;
  imageUrl?: string | null;
  maxTravelers: number;
}

export interface CreateActivityRequest {
  title: string;
  description?: string | null;
  dayNumber: number;
  price: number;
  sortOrder: number;
}

export interface PackageSearchQuery {
  q?: string;
  destinationId?: number;
  approvalStatus?: ApprovalStatus;
  maxPrice?: number;
  maxDurationDays?: number;
}

export interface Booking {
  id: number;
  userId: string;
  roomId?: number | null;
  travelPackageId?: number | null;
  hotelId?: number | null;
  checkIn: string;
  checkOut: string;
  guests: number;
  notes?: string | null;
  status: BookingStatus;
  totalPrice: number;
  createdAt: string;
  cancelledAt?: string | null;
  roomName?: string | null;
  hotelName?: string | null;
  packageTitle?: string | null;
  userEmail?: string | null;
}

export interface CreateBookingRequest {
  roomId?: number;
  travelPackageId?: number;
  checkIn: string;
  checkOut?: string;
  guests: number;
  notes?: string;
}

export interface AvailabilityQuery {
  roomId?: number;
  travelPackageId?: number;
  checkIn: string;
  checkOut?: string;
  guests: number;
}

export interface AvailabilityQuote {
  available: boolean;
  reason?: string | null;
  roomId?: number | null;
  travelPackageId?: number | null;
  checkIn: string;
  checkOut: string;
  nights: number;
  guests: number;
  totalPrice: number;
  remainingPlaces?: number | null;
}

export interface Payment {
  id: number;
  bookingId: number;
  amount: number;
  currency: string;
  method: PaymentMethod;
  status: PaymentStatus;
  transactionReference: string;
  paidAt?: string | null;
  createdAt: string;
}

export interface CreatePaymentRequest {
  method: PaymentMethod;
  cardNumber?: string;
}

export interface Review {
  id: number;
  bookingId: number;
  hotelId?: number | null;
  hotelName?: string | null;
  travelPackageId?: number | null;
  packageTitle?: string | null;
  rating: number;
  comment?: string | null;
  status: ReviewStatus;
  authorName: string;
  createdAt: string;
  updatedAt: string;
}

export interface Transportation {
  id: number;
  providerId: string;
  travelPackageId?: number | null;
  packageTitle?: string | null;
  destinationId?: number | null;
  destinationName?: string | null;
  mode: TransportMode;
  fromLocation: string;
  toLocation: string;
  departureTime?: string | null;
  durationMinutes: number;
  pricePerPerson: number;
  capacity: number;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SaveTransportationRequest {
  travelPackageId?: number | null;
  destinationId?: number | null;
  mode: TransportMode;
  fromLocation: string;
  toLocation: string;
  departureTime?: string | null;
  durationMinutes: number;
  pricePerPerson: number;
  capacity: number;
  description?: string | null;
  isActive: boolean;
}

export interface SystemSetting {
  key: string;
  value: string;
  description?: string | null;
  updatedBy?: string | null;
  updatedAt: string;
}

export interface PublicSettings {
  defaultCurrency: string;
  aiAssistantEnabled: boolean;
  maintenanceMessage?: string | null;
  maxAdvanceBookingDays: number;
  guestCancellationCutoffHours: number;
}

export interface ReportSummary {
  userCount: number;
  hotelOwnerCount: number;
  travelAgentCount: number;
  hotelCount: number;
  packageCount: number;
  pendingHotelApprovals: number;
  pendingPackageApprovals: number;
  pendingBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  completedBookings: number;
  revenue: number;
}

export interface Statistics {
  from: string;
  to: string;
  totalBookings: number;
  /** Keys are BookingStatus names or numbers depending on the serializer; see statusCount(). */
  bookingsByStatus: Record<string, number>;
  revenue: number;
  averageBookingValue: number;
  cancellationRate: number;
  totalGuests: number;
  averageRating?: number | null;
  reviewCount: number;
  monthly: { month: string; bookings: number; revenue: number }[];
  topListings: {
    id: number;
    type: string;
    name: string;
    bookings: number;
    revenue: number;
    averageRating?: number | null;
    reviewCount: number;
  }[];
}

export interface Itinerary {
  id: number;
  title: string;
  startDate: string;
  endDate: string;
  status: number;
  estimatedCost?: number | null;
  budget?: number | null;
  travelers: number;
  destinationId?: number | null;
  destinationName?: string | null;
  conversationId?: number | null;
  summary?: string | null;
  itemCount: number;
}

export interface ItineraryItem {
  id?: number;
  dayNumber: number;
  title: string;
  description?: string | null;
  startTime?: string | null;
  sortOrder: number;
}

export interface ItineraryDetail extends Itinerary {
  items: ItineraryItem[];
}

export interface CreateItineraryRequest {
  title: string;
  startDate: string;
  endDate: string;
  destinationId?: number | null;
  estimatedCost?: number | null;
  budget?: number | null;
  travelers?: number;
  conversationId?: number | null;
  summary?: string | null;
  items: ItineraryItem[];
}

// ---- AI ----

export type ChatStatus =
  | 'plan'
  | 'clarification'
  | 'no_match'
  | 'over_budget'
  | 'booking_proposal'
  | 'booking_created'
  | 'booking_failed'
  | 'refused'
  | 'info';

export interface PlanHotel {
  hotelId: number;
  roomId: number;
  name: string;
  roomName: string;
  city: string;
  pricePerNight: number;
  nights: number;
  rooms: number;
  capacity: number;
  totalCost: number;
  averageRating?: number | null;
  selected: boolean;
  availabilityChecked: boolean;
}

export interface PlanPackage {
  packageId: number;
  title: string;
  durationDays: number;
  pricePerPerson: number;
  totalCost: number;
  remainingPlaces?: number | null;
  averageRating?: number | null;
  selected: boolean;
  availabilityChecked: boolean;
}

export interface PlanActivity {
  title: string;
  category?: string | null;
  day: number;
  pricePerPerson: number;
  packageId?: number | null;
  includedInCost: boolean;
}

export interface PlanTransport {
  transportationId: number;
  mode: string;
  from: string;
  to: string;
  departureTime?: string | null;
  durationMinutes: number;
  pricePerPerson: number;
  trips: number;
  totalCost: number;
  selected: boolean;
}

export interface PlanDayItem {
  time: string;
  type: string;
  title: string;
  description?: string | null;
}

export interface PlanDay {
  day: number;
  date: string;
  items: PlanDayItem[];
}

export interface TravelPlan {
  destination: string;
  destinationId?: number | null;
  country?: string | null;
  duration: number;
  nights: number;
  startDate: string;
  endDate: string;
  travelers: number;
  budget: number;
  currency: string;
  estimatedTotal: number;
  withinBudget: boolean;
  costBreakdown: { accommodation: number; packages: number; transportation: number };
  hotels: PlanHotel[];
  travelPackages: PlanPackage[];
  activities: PlanActivity[];
  transportation: PlanTransport[];
  itinerary: PlanDay[];
  assumptions: string[];
  warnings: string[];
}

export interface BookingProposal {
  id: string;
  kind: 'room' | 'package';
  hotelId?: number | null;
  roomId?: number | null;
  travelPackageId?: number | null;
  title: string;
  checkIn: string;
  checkOut?: string | null;
  guests: number;
  quotedTotal: number;
  currency: string;
  expiresAt: string;
}

export interface ToolCallSummary {
  name: string;
  success: boolean;
  error?: string | null;
  durationMs: number;
}

export interface ChatRequest {
  conversationId?: number;
  message: string;
  confirmBookingId?: string;
}

export interface ChatResponse {
  conversationId: number;
  message: string;
  status: ChatStatus;
  plan?: TravelPlan | null;
  pendingBooking?: BookingProposal | null;
  booking?: Booking | null;
  mode: 'llm' | 'deterministic';
  agents: string[];
  toolCalls: ToolCallSummary[];
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  status?: ChatStatus | null;
  plan?: TravelPlan | null;
  pendingBooking?: BookingProposal | null;
  bookingId?: number | null;
}

export interface ConversationSummary {
  id: number;
  title: string;
  updatedAt: string;
}

export interface ConversationDetail extends ConversationSummary {
  createdAt: string;
  messages: ChatMessage[];
}

export interface AiRecommendation {
  id: number;
  conversationId: number;
  itemType: RecommendationType;
  hotelId?: number | null;
  roomId?: number | null;
  travelPackageId?: number | null;
  transportationId?: number | null;
  destinationId?: number | null;
  title: string;
  estimatedCost: number;
  score: number;
  reason?: string | null;
  createdAt: string;
}
