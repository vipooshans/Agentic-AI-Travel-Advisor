# After generating the Android platform folder:
#   flutter create . --platforms=android
# copy this overlay and set applicationId in android/app/build.gradle(.kts).
#
# 1. Copy network_security_config.xml to android/app/src/main/res/xml/
# 2. In AndroidManifest.xml <application> add:
#      android:usesCleartextTraffic="true"
#      android:networkSecurityConfig="@xml/network_security_config"
# 3. applicationId com.traveladvisor.app, minSdk 21
