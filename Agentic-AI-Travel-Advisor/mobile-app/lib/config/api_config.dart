class ApiConfig {
  static const String defaultProductionUrl = 'https://agentic-ai-travel-advisor-4.onrender.com';

  static String get baseUrl {
    const defined = String.fromEnvironment('API_BASE_URL');
    if (defined.isNotEmpty) return defined.replaceAll(RegExp(r'/+$'), '');
    return defaultProductionUrl;
  }
}
