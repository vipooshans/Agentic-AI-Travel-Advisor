// Numeric values mirror the C# enums in shared/Core/Enums; the API serializes enums as numbers.

export const Roles = {
  User: 'USER',
  HotelOwner: 'HOTEL_OWNER',
  TravelAgent: 'TRAVEL_AGENT',
  Admin: 'ADMIN',
} as const;
export type Role = (typeof Roles)[keyof typeof Roles];

export const roleLabels: Record<Role, string> = {
  USER: 'Traveler',
  HOTEL_OWNER: 'Hotel owner',
  TRAVEL_AGENT: 'Travel agent',
  ADMIN: 'Administrator',
};

export const ApprovalStatus = { Pending: 0, Approved: 1, Rejected: 2 } as const;
export type ApprovalStatus = (typeof ApprovalStatus)[keyof typeof ApprovalStatus];
export const approvalLabels = ['Pending', 'Approved', 'Rejected'] as const;

export const BookingStatus = { Pending: 0, Confirmed: 1, Cancelled: 2, Completed: 3 } as const;
export type BookingStatus = (typeof BookingStatus)[keyof typeof BookingStatus];
export const bookingStatusLabels = ['Pending', 'Confirmed', 'Cancelled', 'Completed'] as const;

export const TransportMode = { Bus: 0, Train: 1, Car: 2, Van: 3, TukTuk: 4, Flight: 5, Ferry: 6 } as const;
export type TransportMode = (typeof TransportMode)[keyof typeof TransportMode];
export const transportModeLabels = ['Bus', 'Train', 'Car', 'Van', 'Tuk-tuk', 'Flight', 'Ferry'] as const;

export const PaymentMethod = { Card: 0, Cash: 1, BankTransfer: 2 } as const;
export type PaymentMethod = (typeof PaymentMethod)[keyof typeof PaymentMethod];
export const paymentMethodLabels = ['Card', 'Cash', 'Bank transfer'] as const;

export const PaymentStatus = { Pending: 0, Completed: 1, Refunded: 2, Failed: 3 } as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];
export const paymentStatusLabels = ['Pending', 'Completed', 'Refunded', 'Failed'] as const;

export const ReviewStatus = { Visible: 0, Hidden: 1 } as const;
export type ReviewStatus = (typeof ReviewStatus)[keyof typeof ReviewStatus];
export const reviewStatusLabels = ['Visible', 'Hidden'] as const;

export const RecommendationType = { Hotel: 0, Package: 1, Activity: 2, Transportation: 3 } as const;
export type RecommendationType = (typeof RecommendationType)[keyof typeof RecommendationType];
export const recommendationTypeLabels = ['Hotel', 'Package', 'Activity', 'Transport'] as const;
