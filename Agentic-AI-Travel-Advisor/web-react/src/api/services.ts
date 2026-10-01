import { api } from './client';
import type { ApprovalStatus, BookingStatus, PaymentStatus, ReviewStatus } from './enums';
import type {
  AiRecommendation,
  AuthResponse,
  AvailabilityQuery,
  AvailabilityQuote,
  Booking,
  ChatRequest,
  ChatResponse,
  ConversationDetail,
  ConversationSummary,
  CreateActivityRequest,
  CreateBookingRequest,
  CreateItineraryRequest,
  CreatePaymentRequest,
  CreateStaffUserRequest,
  Destination,
  Hotel,
  HotelDetail,
  HotelSearchQuery,
  Itinerary,
  ItineraryDetail,
  PackageActivity,
  PackageSearchQuery,
  Payment,
  PublicSettings,
  RegisterRequest,
  ReportSummary,
  Review,
  Room,
  SaveDestinationRequest,
  SaveHotelRequest,
  SavePackageRequest,
  SaveRoomRequest,
  SaveTransportationRequest,
  Statistics,
  SystemSetting,
  Transportation,
  TravelPackage,
  TravelPackageDetail,
  TravelPreferences,
  User,
  UserProfile,
} from './types';

const data = <T>(p: Promise<{ data: T }>) => p.then((r) => r.data);

export const authApi = {
  login: (email: string, password: string) =>
    data(api.post<AuthResponse>('/api/auth/login', { email, password })),
  register: (request: RegisterRequest) => data(api.post<AuthResponse>('/api/auth/register', request)),
  me: () => data(api.get<User>('/api/auth/me')),
  updateMe: (firstName: string, lastName: string) =>
    data(api.put<User>('/api/auth/me', { firstName, lastName })),
};

export const usersApi = {
  getProfile: () => data(api.get<UserProfile>('/api/users/me/profile')),
  saveProfile: (profile: UserProfile) => data(api.put<UserProfile>('/api/users/me/profile', profile)),
  getPreferences: () => data(api.get<TravelPreferences>('/api/users/me/preferences')),
  savePreferences: (prefs: TravelPreferences) =>
    data(api.put<TravelPreferences>('/api/users/me/preferences', prefs)),
  list: () => data(api.get<User[]>('/api/users')),
  createStaff: (request: CreateStaffUserRequest) => data(api.post<User>('/api/users', request)),
  setActive: (id: string, isActive: boolean) =>
    data(api.patch<User>(`/api/users/${encodeURIComponent(id)}/active`, { isActive })),
};

export const destinationsApi = {
  list: () => data(api.get<Destination[]>('/api/destinations')),
  get: (id: number) => data(api.get<Destination>(`/api/destinations/${id}`)),
  create: (request: SaveDestinationRequest) => data(api.post<Destination>('/api/destinations', request)),
  update: (id: number, request: SaveDestinationRequest) =>
    data(api.put<Destination>(`/api/destinations/${id}`, request)),
  remove: (id: number) => api.delete(`/api/destinations/${id}`).then(() => undefined),
};

export const hotelsApi = {
  search: (query: HotelSearchQuery = {}) => data(api.get<Hotel[]>('/api/hotels', { params: query })),
  mine: () => data(api.get<Hotel[]>('/api/hotels/mine')),
  get: (id: number) => data(api.get<HotelDetail>(`/api/hotels/${id}`)),
  create: (request: SaveHotelRequest) => data(api.post<Hotel>('/api/hotels', request)),
  update: (id: number, request: SaveHotelRequest) => data(api.put<Hotel>(`/api/hotels/${id}`, request)),
  remove: (id: number) => api.delete(`/api/hotels/${id}`).then(() => undefined),
  setApproval: (id: number, status: ApprovalStatus) =>
    data(api.patch<Hotel>(`/api/hotels/${id}/approval`, { status })),
  reviews: (id: number) => data(api.get<Review[]>(`/api/hotels/${id}/reviews`)),
};

export const roomsApi = {
  list: (hotelId: number) => data(api.get<Room[]>(`/api/hotels/${hotelId}/rooms`)),
  create: (hotelId: number, request: SaveRoomRequest) =>
    data(api.post<Room>(`/api/hotels/${hotelId}/rooms`, request)),
  update: (hotelId: number, roomId: number, request: SaveRoomRequest) =>
    data(api.put<Room>(`/api/hotels/${hotelId}/rooms/${roomId}`, request)),
  setAvailability: (hotelId: number, roomId: number, isAvailable: boolean) =>
    data(api.patch<Room>(`/api/hotels/${hotelId}/rooms/${roomId}/availability`, { isAvailable })),
  remove: (hotelId: number, roomId: number) =>
    api.delete(`/api/hotels/${hotelId}/rooms/${roomId}`).then(() => undefined),
};

export const packagesApi = {
  search: (query: PackageSearchQuery = {}) =>
    data(api.get<TravelPackage[]>('/api/packages', { params: query })),
  mine: () => data(api.get<TravelPackage[]>('/api/packages/mine')),
  get: (id: number) => data(api.get<TravelPackageDetail>(`/api/packages/${id}`)),
  create: (request: SavePackageRequest) => data(api.post<TravelPackage>('/api/packages', request)),
  update: (id: number, request: SavePackageRequest) =>
    data(api.put<TravelPackage>(`/api/packages/${id}`, request)),
  remove: (id: number) => api.delete(`/api/packages/${id}`).then(() => undefined),
  addActivity: (id: number, request: CreateActivityRequest) =>
    data(api.post<PackageActivity>(`/api/packages/${id}/activities`, request)),
  removeActivity: (id: number, activityId: number) =>
    api.delete(`/api/packages/${id}/activities/${activityId}`).then(() => undefined),
  setApproval: (id: number, status: ApprovalStatus) =>
    data(api.patch<TravelPackage>(`/api/packages/${id}/approval`, { status })),
  reviews: (id: number) => data(api.get<Review[]>(`/api/packages/${id}/reviews`)),
};

export const bookingsApi = {
  list: () => data(api.get<Booking[]>('/api/bookings')),
  get: (id: number) => data(api.get<Booking>(`/api/bookings/${id}`)),
  availability: (query: AvailabilityQuery) =>
    data(api.get<AvailabilityQuote>('/api/bookings/availability', { params: query })),
  create: (request: CreateBookingRequest) => data(api.post<Booking>('/api/bookings', request)),
  setStatus: (id: number, status: BookingStatus) =>
    data(api.patch<Booking>(`/api/bookings/${id}/status`, { status })),
};

export const paymentsApi = {
  list: (bookingId: number) => data(api.get<Payment[]>(`/api/bookings/${bookingId}/payments`)),
  create: (bookingId: number, request: CreatePaymentRequest) =>
    data(api.post<Payment>(`/api/bookings/${bookingId}/payments`, request)),
  setStatus: (bookingId: number, paymentId: number, status: PaymentStatus) =>
    data(api.patch<Payment>(`/api/bookings/${bookingId}/payments/${paymentId}/status`, { status })),
};

export const reviewsApi = {
  list: (query: { status?: ReviewStatus; hotelId?: number; travelPackageId?: number } = {}) =>
    data(api.get<Review[]>('/api/reviews', { params: query })),
  create: (bookingId: number, rating: number, comment: string) =>
    data(api.post<Review>('/api/reviews', { bookingId, rating, comment })),
  remove: (id: number) => api.delete(`/api/reviews/${id}`).then(() => undefined),
  setStatus: (id: number, status: ReviewStatus) =>
    data(api.patch<Review>(`/api/reviews/${id}/status`, { status })),
};

export const transportApi = {
  search: (query: Record<string, string | number | undefined> = {}) =>
    data(api.get<Transportation[]>('/api/transportation', { params: query })),
  mine: () => data(api.get<Transportation[]>('/api/transportation/mine')),
  create: (request: SaveTransportationRequest) =>
    data(api.post<Transportation>('/api/transportation', request)),
  update: (id: number, request: SaveTransportationRequest) =>
    data(api.put<Transportation>(`/api/transportation/${id}`, request)),
  remove: (id: number) => api.delete(`/api/transportation/${id}`).then(() => undefined),
};

export const settingsApi = {
  public: () => data(api.get<PublicSettings>('/api/settings/public')),
  list: () => data(api.get<SystemSetting[]>('/api/settings')),
  update: (key: string, value: string) =>
    data(api.put<SystemSetting>(`/api/settings/${encodeURIComponent(key)}`, { value })),
};

export const reportsApi = {
  summary: () => data(api.get<ReportSummary>('/api/reports/summary')),
  statistics: (query: { from?: string; to?: string; top?: number } = {}) =>
    data(api.get<Statistics>('/api/reports/statistics', { params: query })),
};

export const itinerariesApi = {
  list: () => data(api.get<Itinerary[]>('/api/itineraries')),
  get: (id: number) => data(api.get<ItineraryDetail>(`/api/itineraries/${id}`)),
  create: (request: CreateItineraryRequest) => data(api.post<ItineraryDetail>('/api/itineraries', request)),
  remove: (id: number) => api.delete(`/api/itineraries/${id}`).then(() => undefined),
};

export const aiApi = {
  chat: (request: ChatRequest) => data(api.post<ChatResponse>('/api/ai/chat', request, { timeout: 90_000 })),
  conversations: () => data(api.get<ConversationSummary[]>('/api/ai/conversations')),
  conversation: (id: number) => data(api.get<ConversationDetail>(`/api/ai/conversations/${id}`)),
  recommendations: (conversationId?: number) =>
    data(api.get<AiRecommendation[]>('/api/ai/recommendations', { params: { conversationId } })),
};
