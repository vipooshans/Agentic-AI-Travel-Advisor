# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 0 |
| Medium | 0 |
| Low | 1 |
| Informational | 3 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Low | Warning |  | ZAP warnings logged - see the zap.log file for details | 1,554    |
| Low | Exceeded Low |  | Percentage of network failures | 23 % |
| Low | Exceeded High | http://host.docker.internal:5080 | Percentage of responses with status code 4xx | 95 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of responses with status code 2xx | 4 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with content type application/json | 5 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with content type application/problem+json | 55 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method DELETE | 6 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method GET | 64 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method PATCH | 5 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method POST | 15 % |
| Info | Informational | http://host.docker.internal:5080 | Percentage of endpoints with method PUT | 8 % |
| Info | Informational | http://host.docker.internal:5080 | Count of total endpoints | 133    |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| Cross-Origin-Resource-Policy Header Missing or Invalid | Low | Systemic |
| A Client Error response code was returned by the server | Informational | 132 |
| Authentication Request Identified | Informational | 2 |
| Non-Storable Content | Informational | Systemic |




## Alert Detail



### [ Cross-Origin-Resource-Policy Header Missing or Invalid ](https://www.zaproxy.org/docs/alerts/90004/)



##### Low (Medium)

### Description

Cross-Origin-Resource-Policy header is an opt-in header designed to counter side-channels attacks like Spectre. Resource should be specifically set as shareable amongst different origins.

* URL: http://host.docker.internal:5080/api/Destinations%3Fq=q
  * Node Name: `http://host.docker.internal:5080/api/Destinations (q)`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Health
  * Node Name: `http://host.docker.internal:5080/api/Health`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels%3FQ=Q&City=East%2520Romaineburgh&Country=Country&ApprovalStatus=0&MaxPrice=1.2&Guests=10
  * Node Name: `http://host.docker.internal:5080/api/Hotels (ApprovalStatus,City,Country,Guests,MaxPrice,Q)`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages%3FQ=Q&DestinationId=10&ApprovalStatus=0&MaxPrice=1.2&MaxDurationDays=10
  * Node Name: `http://host.docker.internal:5080/api/Packages (ApprovalStatus,DestinationId,MaxDurationDays,MaxPrice,Q)`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://host.docker.internal:5080/swagger/v1/swagger.json
  * Node Name: `http://host.docker.internal:5080/swagger/v1/swagger.json`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``

Instances: Systemic


### Solution

Ensure that the application/web server sets the Cross-Origin-Resource-Policy header appropriately, and that it sets the Cross-Origin-Resource-Policy header to 'same-origin' for all web pages.
'same-site' is considered as less secured and should be avoided.
If resources must be shared, set the header to 'cross-origin'.
If possible, ensure that the end user uses a standards-compliant and modern web browser that supports the Cross-Origin-Resource-Policy header (https://caniuse.com/mdn-http_headers_cross-origin-resource-policy).

### Reference


* [ https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy ](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 14

#### Source ID: 3

### [ A Client Error response code was returned by the server ](https://www.zaproxy.org/docs/alerts/100000/)



##### Informational (High)

### Description

A response code of 401 was returned by the server.
This may indicate that the application is failing to handle unexpected input correctly.
Raised by the 'Alert on HTTP Response Code Error' script

* URL: http://host.docker.internal:5080/api/Destinations/10
  * Node Name: `http://host.docker.internal:5080/api/Destinations/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries/10
  * Node Name: `http://host.docker.internal:5080/api/Itineraries/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10
  * Node Name: `http://host.docker.internal:5080/api/Packages/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/activities/10
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10
  * Node Name: `http://host.docker.internal:5080/api/reviews/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation/10
  * Node Name: `http://host.docker.internal:5080/api/transportation/10`
  * Method: `DELETE`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080
  * Node Name: `http://host.docker.internal:5080`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/293215157782776648
  * Node Name: `http://host.docker.internal:5080/293215157782776648`
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
* URL: http://host.docker.internal:5080/api/645479545175448749
  * Node Name: `http://host.docker.internal:5080/api/645479545175448749`
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
* URL: http://host.docker.internal:5080/api/Auth/6803289392352038713
  * Node Name: `http://host.docker.internal:5080/api/Auth/6803289392352038713`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Auth/me
  * Node Name: `http://host.docker.internal:5080/api/Auth/me`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings
  * Node Name: `http://host.docker.internal:5080/api/Bookings`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/10
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/10/5874565162451718147
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10/5874565162451718147`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/4869779135970702606
  * Node Name: `http://host.docker.internal:5080/api/Bookings/4869779135970702606`
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
* URL: http://host.docker.internal:5080/api/Destinations/4697628747133972746
  * Node Name: `http://host.docker.internal:5080/api/Destinations/4697628747133972746`
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
* URL: http://host.docker.internal:5080/api/Hotels/10/7668618734026404733
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10/7668618734026404733`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/4682283015413183093
  * Node Name: `http://host.docker.internal:5080/api/Hotels/4682283015413183093`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries
  * Node Name: `http://host.docker.internal:5080/api/Itineraries`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries/10
  * Node Name: `http://host.docker.internal:5080/api/Itineraries/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries/6164428210654698339
  * Node Name: `http://host.docker.internal:5080/api/Itineraries/6164428210654698339`
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
* URL: http://host.docker.internal:5080/api/Packages/10/5780305537095553988
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/5780305537095553988`
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
* URL: http://host.docker.internal:5080/api/Packages/10/activities/2878959103805008391
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities/2878959103805008391`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/77184026522775702
  * Node Name: `http://host.docker.internal:5080/api/Packages/77184026522775702`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai
  * Node Name: `http://host.docker.internal:5080/api/ai`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/1281829928312927427
  * Node Name: `http://host.docker.internal:5080/api/ai/1281829928312927427`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/conversations
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/conversations/10
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/conversations/331765903949149218
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations/331765903949149218`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/recommendations%3FconversationId=10
  * Node Name: `http://host.docker.internal:5080/api/ai/recommendations (conversationId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings
  * Node Name: `http://host.docker.internal:5080/api/bookings`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10
  * Node Name: `http://host.docker.internal:5080/api/bookings/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/9155905970739446135
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/9155905970739446135`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/10
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/10/8123349714813834324
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/10/8123349714813834324`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/1429420182719739977
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/1429420182719739977`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/3845154707926545502
  * Node Name: `http://host.docker.internal:5080/api/bookings/3845154707926545502`
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
* URL: http://host.docker.internal:5080/api/hotels/10/2257885760496427783
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/2257885760496427783`
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
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/2294641113139938256
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/2294641113139938256`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/4450941051315228795
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/4450941051315228795`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/8446297709986345746
  * Node Name: `http://host.docker.internal:5080/api/hotels/8446297709986345746`
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
* URL: http://host.docker.internal:5080/api/packages/10/4974845681951105880
  * Node Name: `http://host.docker.internal:5080/api/packages/10/4974845681951105880`
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
* URL: http://host.docker.internal:5080/api/packages/2825026022844509777
  * Node Name: `http://host.docker.internal:5080/api/packages/2825026022844509777`
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
* URL: http://host.docker.internal:5080/api/reports/8243180034710780395
  * Node Name: `http://host.docker.internal:5080/api/reports/8243180034710780395`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reports/summary
  * Node Name: `http://host.docker.internal:5080/api/reports/summary`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews
  * Node Name: `http://host.docker.internal:5080/api/reviews`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews%3FStatus=0&HotelId=10&TravelPackageId=10
  * Node Name: `http://host.docker.internal:5080/api/reviews (HotelId,Status,TravelPackageId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10
  * Node Name: `http://host.docker.internal:5080/api/reviews/10`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10/4062577821074238286
  * Node Name: `http://host.docker.internal:5080/api/reviews/10/4062577821074238286`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/2356438846356565324
  * Node Name: `http://host.docker.internal:5080/api/reviews/2356438846356565324`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/settings/7135636382955326072
  * Node Name: `http://host.docker.internal:5080/api/settings/7135636382955326072`
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
* URL: http://host.docker.internal:5080/api/transportation/4030350391835977043
  * Node Name: `http://host.docker.internal:5080/api/transportation/4030350391835977043`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users
  * Node Name: `http://host.docker.internal:5080/api/users`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users%3Frole=role
  * Node Name: `http://host.docker.internal:5080/api/users (role)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/5693746175164063204
  * Node Name: `http://host.docker.internal:5080/api/users/5693746175164063204`
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
* URL: http://host.docker.internal:5080/api/users/id/5323672260928259438
  * Node Name: `http://host.docker.internal:5080/api/users/id/5323672260928259438`
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
* URL: http://host.docker.internal:5080/api/users/me/7507967261801948786
  * Node Name: `http://host.docker.internal:5080/api/users/me/7507967261801948786`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me/preferences
  * Node Name: `http://host.docker.internal:5080/api/users/me/preferences`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me/profile
  * Node Name: `http://host.docker.internal:5080/api/users/me/profile`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/swagger/2561357870301129095
  * Node Name: `http://host.docker.internal:5080/swagger/2561357870301129095`
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
* URL: http://host.docker.internal:5080/swagger/v1/6015035504222533731
  * Node Name: `http://host.docker.internal:5080/swagger/v1/6015035504222533731`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10/approval
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10/approval ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/approval
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/approval ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/10/status
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/availability
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/availability ()({isAvailable})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10/status
  * Node Name: `http://host.docker.internal:5080/api/reviews/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/id/active
  * Node Name: `http://host.docker.internal:5080/api/users/id/active ()({isActive})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Destinations
  * Node Name: `http://host.docker.internal:5080/api/Destinations ()({name,country,description,imageUrl})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels
  * Node Name: `http://host.docker.internal:5080/api/Hotels ()({name,address,city,country,description,imageUrl})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Itineraries
  * Node Name: `http://host.docker.internal:5080/api/Itineraries ()({title,startDate,endDate,destinationId,estimatedCost,budget,travelers,conversationId,summary,items:[{dayNumber,title,description,startTime,sortOrder}]})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages
  * Node Name: `http://host.docker.internal:5080/api/Packages ()({destinationId,title,description,price,durationDays,imageUrl,maxTravelers})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10/activities
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities ()({title,description,dayNumber,price,sortOrder})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/chat
  * Node Name: `http://host.docker.internal:5080/api/ai/chat ()({conversationId,message,confirmBookingId})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments ()({method,cardNumber})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms ()({name,roomType,pricePerNight,capacity})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews
  * Node Name: `http://host.docker.internal:5080/api/reviews ()({bookingId,rating,comment})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation
  * Node Name: `http://host.docker.internal:5080/api/transportation ()({travelPackageId,destinationId,mode,fromLocation,toLocation,departureTime,durationMinutes,pricePerPerson,capacity,description,isActive})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users
  * Node Name: `http://host.docker.internal:5080/api/users ()({email,password,firstName,lastName,role})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
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
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Destinations/10
  * Node Name: `http://host.docker.internal:5080/api/Destinations/10 ()({name,country,description,imageUrl})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/10
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10 ()({name,address,city,country,description,imageUrl})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/10
  * Node Name: `http://host.docker.internal:5080/api/Packages/10 ()({destinationId,title,description,price,durationDays,imageUrl,maxTravelers})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10 ()({name,roomType,pricePerNight,capacity})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/calendar
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/calendar ()({entries:[{date,isBlocked,priceOverride,note}]})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/10
  * Node Name: `http://host.docker.internal:5080/api/reviews/10 ()({rating,comment})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/settings/key
  * Node Name: `http://host.docker.internal:5080/api/settings/key ()({value})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/transportation/10
  * Node Name: `http://host.docker.internal:5080/api/transportation/10 ()({travelPackageId,destinationId,mode,fromLocation,toLocation,departureTime,durationMinutes,pricePerPerson,capacity,description,isActive})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me/preferences
  * Node Name: `http://host.docker.internal:5080/api/users/me/preferences ()({budgetMin,budgetMax,preferredClimate,interests,accommodationPreference,transportPreference})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/users/me/profile
  * Node Name: `http://host.docker.internal:5080/api/users/me/profile ()({phoneNumber,nationality,dateOfBirth,avatarUrl,bio,preferredCurrency})`
  * Method: `PUT`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``


Instances: 132

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

* URL: http://host.docker.internal:5080/api/Bookings/availability%3FRoomId=10&TravelPackageId=10&CheckIn=CheckIn&CheckOut=CheckOut&Guests=10
  * Node Name: `http://host.docker.internal:5080/api/Bookings/availability (CheckIn,CheckOut,Guests,RoomId,TravelPackageId)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-store`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/ai/conversations
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-store`
  * Other Info: ``
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
* URL: http://host.docker.internal:5080/api/Auth/login
  * Node Name: `http://host.docker.internal:5080/api/Auth/login ()({email,password})`
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


