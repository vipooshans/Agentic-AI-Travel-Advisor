import 'dart:io';

class ApiConfig {
  static const int apiPort = 5000;

  static String get baseUrl {
    if (Platform.isAndroid) {
      return 'http://10.0.2.2:$apiPort';
    }
    return 'http://localhost:$apiPort';
  }
}
