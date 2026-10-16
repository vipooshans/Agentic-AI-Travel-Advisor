class ReportSummary {
  final int userCount;
  final int hotelOwnerCount;
  final int travelAgentCount;
  final int hotelCount;
  final int packageCount;
  final int pendingHotelApprovals;
  final int pendingPackageApprovals;
  final int pendingBookings;
  final int confirmedBookings;
  final int cancelledBookings;
  final int completedBookings;
  final double revenue;

  const ReportSummary({
    this.userCount = 0,
    this.hotelOwnerCount = 0,
    this.travelAgentCount = 0,
    this.hotelCount = 0,
    this.packageCount = 0,
    this.pendingHotelApprovals = 0,
    this.pendingPackageApprovals = 0,
    this.pendingBookings = 0,
    this.confirmedBookings = 0,
    this.cancelledBookings = 0,
    this.completedBookings = 0,
    this.revenue = 0.0,
  });

  int get totalBookings => pendingBookings + confirmedBookings + completedBookings + cancelledBookings;
  int get activeBookings => pendingBookings + confirmedBookings;
  int get totalPendingApprovals => pendingHotelApprovals + pendingPackageApprovals;

  factory ReportSummary.fromJson(Map<String, dynamic> json) {
    return ReportSummary(
      userCount: (json['userCount'] as num?)?.toInt() ?? 0,
      hotelOwnerCount: (json['hotelOwnerCount'] as num?)?.toInt() ?? 0,
      travelAgentCount: (json['travelAgentCount'] as num?)?.toInt() ?? 0,
      hotelCount: (json['hotelCount'] as num?)?.toInt() ?? 0,
      packageCount: (json['packageCount'] as num?)?.toInt() ?? 0,
      pendingHotelApprovals: (json['pendingHotelApprovals'] as num?)?.toInt() ?? 0,
      pendingPackageApprovals: (json['pendingPackageApprovals'] as num?)?.toInt() ?? 0,
      pendingBookings: (json['pendingBookings'] as num?)?.toInt() ?? 0,
      confirmedBookings: (json['confirmedBookings'] as num?)?.toInt() ?? 0,
      cancelledBookings: (json['cancelledBookings'] as num?)?.toInt() ?? 0,
      completedBookings: (json['completedBookings'] as num?)?.toInt() ?? 0,
      revenue: (json['revenue'] as num?)?.toDouble() ?? 0.0,
    );
  }
}
