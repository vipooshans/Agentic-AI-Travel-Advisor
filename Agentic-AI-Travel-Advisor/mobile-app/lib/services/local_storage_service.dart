import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Service managing client-side secure preferences, search history, and offline state.
class LocalStorageService {
  final FlutterSecureStorage _storage;

  LocalStorageService([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  static const _recentDestinationsKey = 'recent_searched_destinations';
  static const _preferredCurrencyKey = 'preferred_currency';

  Future<void> saveRecentSearch(String destination) async {
    final existing = await getRecentSearches();
    if (!existing.contains(destination)) {
      existing.insert(0, destination);
      if (existing.length > 5) existing.removeLast();
      await _storage.write(
        key: _recentDestinationsKey,
        value: existing.join(','),
      );
    }
  }

  Future<List<String>> getRecentSearches() async {
    final val = await _storage.read(key: _recentDestinationsKey);
    if (val == null || val.trim().isEmpty) return [];
    return val.split(',').where((e) => e.trim().isNotEmpty).toList();
  }

  Future<void> setPreferredCurrency(String currency) async {
    await _storage.write(key: _preferredCurrencyKey, value: currency);
  }

  Future<String> getPreferredCurrency() async {
    return await _storage.read(key: _preferredCurrencyKey) ?? 'LKR';
  }
}
