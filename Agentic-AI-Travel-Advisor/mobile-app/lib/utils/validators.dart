/// Form validators that mirror the API's own rules, so users see problems
/// before a request is sent. The server still validates everything.
class Validators {
  Validators._();

  static final _email = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');

  static String? email(String? value) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return 'Email is required';
    if (text.length > 256 || !_email.hasMatch(text)) return 'Enter a valid email address';
    return null;
  }

  static String? loginPassword(String? value) {
    if (value == null || value.isEmpty) return 'Password is required';
    if (value.length > 128) return 'Password must be at most 128 characters';
    return null;
  }

  /// ASP.NET Identity policy configured on the API: at least 6 characters
  /// with an uppercase letter, a lowercase letter, a digit and a symbol.
  static String? newPassword(String? value) {
    if (value == null || value.isEmpty) return 'Password is required';
    final missing = <String>[
      if (value.length < 6) 'at least 6 characters',
      if (!value.contains(RegExp(r'[A-Z]'))) 'an uppercase letter',
      if (!value.contains(RegExp(r'[a-z]'))) 'a lowercase letter',
      if (!value.contains(RegExp(r'[0-9]'))) 'a number',
      if (!value.contains(RegExp(r'[^A-Za-z0-9]'))) 'a symbol',
    ];
    if (value.length > 128) return 'Password must be at most 128 characters';
    if (missing.isEmpty) return null;
    return 'Password needs ${missing.join(', ')}';
  }

  static String? Function(String?) personName(String label) {
    return (value) {
      final text = value?.trim() ?? '';
      if (text.isEmpty) return '$label is required';
      if (text.length > 100) return '$label must be at most 100 characters';
      return null;
    };
  }

  static String? guests(String? value, {int? max}) {
    final count = int.tryParse(value?.trim() ?? '');
    if (count == null) return 'Enter the number of guests';
    if (count < 1) return 'At least 1 guest is required';
    final limit = max ?? 50;
    if (count > limit) return 'At most $limit guests';
    return null;
  }

  static String? maxLength(String? value, int max, String label) {
    if (value != null && value.length > max) return '$label must be at most $max characters';
    return null;
  }

  static String? optionalPrice(String? value) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return null;
    final price = double.tryParse(text);
    if (price == null || price <= 0) return 'Enter a positive amount';
    return null;
  }
}
