# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Informational | 3 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Low | Warning |  | ZAP warnings logged - see the zap.log file for details | 1,553    |
| Low | Exceeded Low |  | Percentage of network failures | 23 % |
| Low | Exceeded High | http://host.docker.internal:5080 | Percentage of responses with status code 4xx | 93 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of responses with status code 2xx | 6 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with content type application/json | 12 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with content type application/problem+json | 47 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method DELETE | 6 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method GET | 64 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method PATCH | 5 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method POST | 16 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method PUT | 8 % |
| Info | Informational | http://host.docker.internal:5080 | Count of total endpoints | 131    |
| Info | Informational | http://host.docker.internal:5080 | Percentage of slow responses | 1 % |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| A Client Error response code was returned by the server | Informational | 125 |
| Authentication Request Identified | Informational | 2 |
| Non-Storable Content | Informational | Systemic |




## Alert Detail



### [ A Client Error response code was returned by the server ](https://www.zaproxy.org/docs/alerts/100000/)



##### Informational (High)

### Description

A response code of 404 was returned by the server.
This may indicate that the application is failing to handle unexpected input correctly.
Raised by the 'Alert on HTTP Response Code Error' script

* URL: http://host.docker.internal:5080/api/Destinations/10
  * Node Name: `http://host.docker.internal:5080/api/Destinations/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries/10
  * Node Name: `http://host.docker.internal:5080/api/Itineraries/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10
  * Node Name: `http://host.docker.internal:5080/api/Packages/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/activities/10
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10
  * Node Name: `http://host.docker.internal:5080/api/reviews/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation/10
  * Node Name: `http://host.docker.internal:5080/api/transportation/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080
  * Node Name: `http://host.docker.internal:5080`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/5186037562523186838
  * Node Name: `http://host.docker.internal:5080/5186037562523186838`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api
  * Node Name: `http://host.docker.internal:5080/api`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/8701350255680518298
  * Node Name: `http://host.docker.internal:5080/api/8701350255680518298`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth
  * Node Name: `http://host.docker.internal:5080/api/Auth`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/5292649454398604983
  * Node Name: `http://host.docker.internal:5080/api/Auth/5292649454398604983`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/10
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/10/1007143228647357347
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10/1007143228647357347`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/2846053754217070572
  * Node Name: `http://host.docker.internal:5080/api/Bookings/2846053754217070572`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/availability%3FRoomId=10&TravelPackageId=10&CheckIn=CheckIn&CheckOut=CheckOut&Guests=10
  * Node Name: `http://host.docker.internal:5080/api/Bookings/availability (CheckIn,CheckOut,Guests,RoomId,TravelPackageId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Destinations/10
  * Node Name: `http://host.docker.internal:5080/api/Destinations/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Destinations/4650352115130867524
  * Node Name: `http://host.docker.internal:5080/api/Destinations/4650352115130867524`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels%3FQ=Q&City=East+Romaineburgh&Country=Country&ApprovalStatus=http%253A%252F%252Fwww.google.com%252F&MaxPrice=1.2&Guests=10
  * Node Name: `http://host.docker.internal:5080/api/Hotels (ApprovalStatus,City,Country,Guests,MaxPrice,Q)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10/1867767396094326928
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10/1867767396094326928`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/1787347832934458627
  * Node Name: `http://host.docker.internal:5080/api/Hotels/1787347832934458627`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/mine
  * Node Name: `http://host.docker.internal:5080/api/Hotels/mine`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries/10
  * Node Name: `http://host.docker.internal:5080/api/Itineraries/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries/4644602826175526788
  * Node Name: `http://host.docker.internal:5080/api/Itineraries/4644602826175526788`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages%3FQ=Q&DestinationId=http%253A%252F%252Fwww.google.com%252F&ApprovalStatus=0&MaxPrice=1.2&MaxDurationDays=10
  * Node Name: `http://host.docker.internal:5080/api/Packages (ApprovalStatus,DestinationId,MaxDurationDays,MaxPrice,Q)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10
  * Node Name: `http://host.docker.internal:5080/api/Packages/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/7043996653834073338
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/7043996653834073338`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/activities
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/activities/596225152715098733
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities/596225152715098733`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/6021700158706573955
  * Node Name: `http://host.docker.internal:5080/api/Packages/6021700158706573955`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/mine
  * Node Name: `http://host.docker.internal:5080/api/Packages/mine`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai
  * Node Name: `http://host.docker.internal:5080/api/ai`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/7262058013741963293
  * Node Name: `http://host.docker.internal:5080/api/ai/7262058013741963293`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/conversations/10
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/conversations/5118144636772650050
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations/5118144636772650050`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/recommendations%3FconversationId=http%253A%252F%252Fwww.google.com%252F
  * Node Name: `http://host.docker.internal:5080/api/ai/recommendations (conversationId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10
  * Node Name: `http://host.docker.internal:5080/api/bookings/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/6838956486717304186
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/6838956486717304186`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/10
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/10/1335311791411308544
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/10/1335311791411308544`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/8431080328209668099
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/8431080328209668099`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/4837993211428835084
  * Node Name: `http://host.docker.internal:5080/api/bookings/4837993211428835084`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10
  * Node Name: `http://host.docker.internal:5080/api/hotels/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/4123864802293829459
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/4123864802293829459`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/reviews
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/reviews`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/3721840379217706487
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/3721840379217706487`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/calendar%3Ffrom=from&to=to
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/calendar (from,to)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/6552003666011198738
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/6552003666011198738`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/8632856757116507200
  * Node Name: `http://host.docker.internal:5080/api/hotels/8632856757116507200`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/packages/10
  * Node Name: `http://host.docker.internal:5080/api/packages/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/packages/10/2616521097875747367
  * Node Name: `http://host.docker.internal:5080/api/packages/10/2616521097875747367`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/packages/10/reviews
  * Node Name: `http://host.docker.internal:5080/api/packages/10/reviews`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/packages/7838777062792127926
  * Node Name: `http://host.docker.internal:5080/api/packages/7838777062792127926`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reports
  * Node Name: `http://host.docker.internal:5080/api/reports`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reports/4654408719516368982
  * Node Name: `http://host.docker.internal:5080/api/reports/4654408719516368982`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reports/statistics%3FFrom=From&To=To&Top=10
  * Node Name: `http://host.docker.internal:5080/api/reports/statistics (From,To,Top)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews%3FStatus=http%253A%252F%252Fwww.google.com%252F&HotelId=10&TravelPackageId=10
  * Node Name: `http://host.docker.internal:5080/api/reviews (HotelId,Status,TravelPackageId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10
  * Node Name: `http://host.docker.internal:5080/api/reviews/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10/581317043478517004
  * Node Name: `http://host.docker.internal:5080/api/reviews/10/581317043478517004`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/4237586818814535447
  * Node Name: `http://host.docker.internal:5080/api/reviews/4237586818814535447`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/settings
  * Node Name: `http://host.docker.internal:5080/api/settings`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/settings/7461070055460123606
  * Node Name: `http://host.docker.internal:5080/api/settings/7461070055460123606`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation%3FFrom=From&To=To&DestinationId=http%253A%252F%252Fwww.google.com%252F&TravelPackageId=10&Mode=0&MaxPrice=1.2
  * Node Name: `http://host.docker.internal:5080/api/transportation (DestinationId,From,MaxPrice,Mode,To,TravelPackageId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation/10
  * Node Name: `http://host.docker.internal:5080/api/transportation/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation/1933759519397311150
  * Node Name: `http://host.docker.internal:5080/api/transportation/1933759519397311150`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation/mine
  * Node Name: `http://host.docker.internal:5080/api/transportation/mine`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users
  * Node Name: `http://host.docker.internal:5080/api/users`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users%3Frole=role
  * Node Name: `http://host.docker.internal:5080/api/users (role)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/5668100627943706752
  * Node Name: `http://host.docker.internal:5080/api/users/5668100627943706752`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/id
  * Node Name: `http://host.docker.internal:5080/api/users/id`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/id/7519067357420269586
  * Node Name: `http://host.docker.internal:5080/api/users/id/7519067357420269586`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me
  * Node Name: `http://host.docker.internal:5080/api/users/me`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me/8956592563663296922
  * Node Name: `http://host.docker.internal:5080/api/users/me/8956592563663296922`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/swagger/1189012386290924029
  * Node Name: `http://host.docker.internal:5080/swagger/1189012386290924029`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/swagger/v1
  * Node Name: `http://host.docker.internal:5080/swagger/v1`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/swagger/v1/4053137297293068556
  * Node Name: `http://host.docker.internal:5080/swagger/v1/4053137297293068556`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/10/status
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/10/status
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10/approval
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10/approval ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/approval
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/approval ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/10/status
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/availability
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/availability ()({isAvailable})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10/status
  * Node Name: `http://host.docker.internal:5080/api/reviews/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/id/active
  * Node Name: `http://host.docker.internal:5080/api/users/id/active ()({isActive})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/login
  * Node Name: `http://host.docker.internal:5080/api/Auth/login ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/login
  * Node Name: `http://host.docker.internal:5080/api/Auth/login ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/register
  * Node Name: `http://host.docker.internal:5080/api/Auth/register ()({email,password,firstName,lastName})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/register
  * Node Name: `http://host.docker.internal:5080/api/Auth/register ()({email,password,firstName,lastName})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `409`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings
  * Node Name: `http://host.docker.internal:5080/api/Bookings ()({roomId,travelPackageId,checkIn,checkOut,guests,notes})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Destinations
  * Node Name: `http://host.docker.internal:5080/api/Destinations ()({name,country,description,imageUrl})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels
  * Node Name: `http://host.docker.internal:5080/api/Hotels ()({name,address,city,country,description,imageUrl})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries
  * Node Name: `http://host.docker.internal:5080/api/Itineraries ()({title,startDate,endDate,destinationId,estimatedCost,budget,travelers,conversationId,summary,items:[{dayNumber,title,description,startTime,sortOrder}]})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages
  * Node Name: `http://host.docker.internal:5080/api/Packages ()({destinationId,title,description,price,durationDays,imageUrl,maxTravelers})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/activities
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities ()({title,description,dayNumber,price,sortOrder})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/chat
  * Node Name: `http://host.docker.internal:5080/api/ai/chat ()({conversationId,message,confirmBookingId})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/chat
  * Node Name: `http://host.docker.internal:5080/api/ai/chat ()({conversationId,message,confirmBookingId})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments ()({method,cardNumber})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms ()({name,roomType,pricePerNight,capacity})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews
  * Node Name: `http://host.docker.internal:5080/api/reviews ()({bookingId,rating,comment})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation
  * Node Name: `http://host.docker.internal:5080/api/transportation ()({travelPackageId,destinationId,mode,fromLocation,toLocation,departureTime,durationMinutes,pricePerPerson,capacity,description,isActive})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users
  * Node Name: `http://host.docker.internal:5080/api/users ()({email,password,firstName,lastName,role})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/computeMetadata/v1/
  * Node Name: `http://host.docker.internal:5080/computeMetadata/v1/ ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/latest/meta-data/
  * Node Name: `http://host.docker.internal:5080/latest/meta-data/ ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/metadata/instance
  * Node Name: `http://host.docker.internal:5080/metadata/instance ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/metadata/v1
  * Node Name: `http://host.docker.internal:5080/metadata/v1 ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/opc/v1/instance/
  * Node Name: `http://host.docker.internal:5080/opc/v1/instance/ ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/opc/v2/instance/
  * Node Name: `http://host.docker.internal:5080/opc/v2/instance/ ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/openstack/latest/meta_data.json
  * Node Name: `http://host.docker.internal:5080/openstack/latest/meta_data.json ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/me
  * Node Name: `http://host.docker.internal:5080/api/Auth/me ()({firstName,lastName})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Destinations/10
  * Node Name: `http://host.docker.internal:5080/api/Destinations/10 ()({name,country,description,imageUrl})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10 ()({name,address,city,country,description,imageUrl})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10
  * Node Name: `http://host.docker.internal:5080/api/Packages/10 ()({destinationId,title,description,price,durationDays,imageUrl,maxTravelers})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10 ()({name,roomType,pricePerNight,capacity})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/calendar
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/calendar ()({entries:[{date,isBlocked,priceOverride,note}]})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10
  * Node Name: `http://host.docker.internal:5080/api/reviews/10 ()({rating,comment})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/settings/key
  * Node Name: `http://host.docker.internal:5080/api/settings/key ()({value})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation/10
  * Node Name: `http://host.docker.internal:5080/api/transportation/10 ()({travelPackageId,destinationId,mode,fromLocation,toLocation,departureTime,durationMinutes,pricePerPerson,capacity,description,isActive})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me/preferences
  * Node Name: `http://host.docker.internal:5080/api/users/me/preferences ()({budgetMin,budgetMax,preferredClimate,interests,accommodationPreference,transportPreference})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me/profile
  * Node Name: `http://host.docker.internal:5080/api/users/me/profile ()({phoneNumber,nationality,dateOfBirth,avatarUrl,bio,preferredCurrency})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `400`
  * Other Info: ``


Instances: 125

### Solution



### Reference



#### CWE Id: [ 388 ](https://cwe.mitre.org/data/definitions/388.html)


#### WASC Id: 20

#### Source ID: 4

### [ Authentication Request Identified ](https://www.zaproxy.org/docs/alerts/10111/)



##### Informational (High)

### Description

The given request has been identified as an authentication request. The 'Other Info' field contains a set of key=value lines which identify any relevant fields. If the request is in a context which has an Authentication Method set to "Auto-Detect" then this rule will change the authentication to match the request identified.

* URL: http://host.docker.internal:5080/api/users
  * Node Name: `http://host.docker.internal:5080/api/users ()({email,password,firstName,lastName,role})`
  * Method: `POST`
  * Parameter: `email`
  * Attack: ``
  * Evidence: `password`
  * Other Info: `userParam=email
userValue=zaproxy@example.com
passwordParam=password`
* URL: http://host.docker.internal:5080/api/Auth/login
  * Node Name: `http://host.docker.internal:5080/api/Auth/login ()({email,password})`
  * Method: `POST`
  * Parameter: `email`
  * Attack: ``
  * Evidence: `password`
  * Other Info: `userParam=email
userValue=zaproxy@example.com
passwordParam=password`


Instances: 2

### Solution

This is an informational alert rather than a vulnerability and so there is nothing to fix.

### Reference


* [ https://www.zaproxy.org/docs/desktop/addons/authentication-helper/auth-req-id/ ](https://www.zaproxy.org/docs/desktop/addons/authentication-helper/auth-req-id/)



#### Source ID: 3

### [ Non-Storable Content ](https://www.zaproxy.org/docs/alerts/10049/)



##### Informational (Medium)

### Description

The response contents are not storable by caching components such as proxy servers. If the response does not contain sensitive, personal or user-specific information, it may benefit from being stored and cached, to improve performance.

* URL: http://host.docker.internal:5080/api/ai/conversations/10
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-store`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/recommendations%3FconversationId=10
  * Node Name: `http://host.docker.internal:5080/api/ai/recommendations (conversationId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-store`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/10/status
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `PATCH `
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/login
  * Node Name: `http://host.docker.internal:5080/api/Auth/login ()({email,password})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-store`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/chat
  * Node Name: `http://host.docker.internal:5080/api/ai/chat ()({conversationId,message,confirmBookingId})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-store`
  * Other Info: ``

Instances: Systemic


### Solution

The content may be marked as storable by ensuring that the following conditions are satisfied:
The request method must be understood by the cache and defined as being cacheable ("GET", "HEAD", and "POST" are currently defined as cacheable)
The response status code must be understood by the cache (one of the 1XX, 2XX, 3XX, 4XX, or 5XX response classes are generally understood)
The "no-store" cache directive must not appear in the request or response header fields
For caching by "shared" caches such as "proxy" caches, the "private" response directive must not appear in the response
For caching by "shared" caches such as "proxy" caches, the "Authorization" header field must not appear in the request, unless the response explicitly allows it (using one of the "must-revalidate", "public", or "s-maxage" Cache-Control response directives)
In addition to the conditions above, at least one of the following conditions must also be satisfied by the response:
It must contain an "Expires" header field
It must contain a "max-age" response directive
For "shared" caches such as "proxy" caches, it must contain a "s-maxage" response directive
It must contain a "Cache Control Extension" that allows it to be cached
It must have a status code that is defined as cacheable by default (200, 203, 204, 206, 300, 301, 404, 405, 410, 414, 501).

### Reference


* [ https://datatracker.ietf.org/doc/html/rfc7234 ](https://datatracker.ietf.org/doc/html/rfc7234)
* [ https://datatracker.ietf.org/doc/html/rfc7231 ](https://datatracker.ietf.org/doc/html/rfc7231)
* [ https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html ](https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html)


#### CWE Id: [ 524 ](https://cwe.mitre.org/data/definitions/524.html)


#### WASC Id: 13

#### Source ID: 3


