class StaffUser {
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final String role;
  final bool isActive;
  final DateTime? createdAt;

  const StaffUser({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.role,
    this.isActive = true,
    this.createdAt,
  });

  String get fullName => '$firstName $lastName'.trim();

  factory StaffUser.fromJson(Map<String, dynamic> json) {
    return StaffUser(
      id: json['id'] as String? ?? '',
      email: json['email'] as String? ?? '',
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      role: json['role'] as String? ?? 'USER',
      isActive: json['isActive'] as bool? ?? true,
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt'] as String) : null,
    );
  }
}
