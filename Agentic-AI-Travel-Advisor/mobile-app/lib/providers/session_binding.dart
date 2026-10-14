import 'auth_provider.dart';
import 'chat_provider.dart';

/// Clears the in-memory conversation whenever the user signs out or their
/// session expires, so the next person on the device cannot read it.
void clearChatOnSignOut(AuthProvider auth, ChatProvider chat) {
  auth.addListener(() {
    if (auth.status == AuthStatus.unauthenticated && chat.hasState) {
      chat.reset();
    }
  });
}
