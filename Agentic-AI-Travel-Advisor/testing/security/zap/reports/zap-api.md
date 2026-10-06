# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Informational | 4 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Low | Warning |  | ZAP warnings logged - see the zap.log file for details | 1,543    |
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
| Info | Informational | http://host.docker.internal:5080 | Percentage of slow responses | 8 % |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| A Client Error response code was returned by the server | Informational | 132 |
| Authentication Request Identified | Informational | 2 |
| Non-Storable Content | Informational | Systemic |
| Storable and Cacheable Content | Informational | 1 |




## Alert Detail



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
* URL: http://host.docker.internal:5080/442539938707056319
  * Node Name: `http://host.docker.internal:5080/442539938707056319`
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
* URL: http://host.docker.internal:5080/api/5196766569327346220
  * Node Name: `http://host.docker.internal:5080/api/5196766569327346220`
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
* URL: http://host.docker.internal:5080/api/Auth/7911255839706573365
  * Node Name: `http://host.docker.internal:5080/api/Auth/7911255839706573365`
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
* URL: http://host.docker.internal:5080/api/Bookings/10/4909673801673519910
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10/4909673801673519910`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Bookings/4985705748647585542
  * Node Name: `http://host.docker.internal:5080/api/Bookings/4985705748647585542`
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
* URL: http://host.docker.internal:5080/api/Destinations/1665906033998409602
  * Node Name: `http://host.docker.internal:5080/api/Destinations/1665906033998409602`
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
* URL: http://host.docker.internal:5080/api/Hotels/10/504118952577890436
  * Node Name: `http://host.docker.internal:5080/api/Hotels/10/504118952577890436`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Hotels/2056500663238201249
  * Node Name: `http://host.docker.internal:5080/api/Hotels/2056500663238201249`
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
* URL: http://host.docker.internal:5080/api/Itineraries/4448454627615866992
  * Node Name: `http://host.docker.internal:5080/api/Itineraries/4448454627615866992`
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
* URL: http://host.docker.internal:5080/api/Packages/10/176339182727586584
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/176339182727586584`
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
* URL: http://host.docker.internal:5080/api/Packages/10/activities/8753584083949157635
  * Node Name: `http://host.docker.internal:5080/api/Packages/10/activities/8753584083949157635`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/Packages/2029816680461308690
  * Node Name: `http://host.docker.internal:5080/api/Packages/2029816680461308690`
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
* URL: http://host.docker.internal:5080/api/ai/933359508091087736
  * Node Name: `http://host.docker.internal:5080/api/ai/933359508091087736`
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
* URL: http://host.docker.internal:5080/api/ai/conversations/2681216612361278478
  * Node Name: `http://host.docker.internal:5080/api/ai/conversations/2681216612361278478`
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
* URL: http://host.docker.internal:5080/api/bookings/10/5879593217055660532
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/5879593217055660532`
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
* URL: http://host.docker.internal:5080/api/bookings/10/payments/10/1916769011240386612
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/10/1916769011240386612`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/10/payments/1604183072202614303
  * Node Name: `http://host.docker.internal:5080/api/bookings/10/payments/1604183072202614303`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/bookings/8956283230071332547
  * Node Name: `http://host.docker.internal:5080/api/bookings/8956283230071332547`
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
* URL: http://host.docker.internal:5080/api/hotels/10/7275202190700826286
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/7275202190700826286`
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
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/10/6017383934013325311
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/10/6017383934013325311`
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
* URL: http://host.docker.internal:5080/api/hotels/10/rooms/893330036744328209
  * Node Name: `http://host.docker.internal:5080/api/hotels/10/rooms/893330036744328209`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `405`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/hotels/1167247222113495301
  * Node Name: `http://host.docker.internal:5080/api/hotels/1167247222113495301`
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
* URL: http://host.docker.internal:5080/api/packages/10/4926693483839486494
  * Node Name: `http://host.docker.internal:5080/api/packages/10/4926693483839486494`
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
* URL: http://host.docker.internal:5080/api/packages/5420889947967290853
  * Node Name: `http://host.docker.internal:5080/api/packages/5420889947967290853`
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
* URL: http://host.docker.internal:5080/api/reports/154302546639893420
  * Node Name: `http://host.docker.internal:5080/api/reports/154302546639893420`
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
* URL: http://host.docker.internal:5080/api/reviews/10/5482986248759964853
  * Node Name: `http://host.docker.internal:5080/api/reviews/10/5482986248759964853`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: http://host.docker.internal:5080/api/reviews/5416463501941099329
  * Node Name: `http://host.docker.internal:5080/api/reviews/5416463501941099329`
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
* URL: http://host.docker.internal:5080/api/settings/5429446779584916010
  * Node Name: `http://host.docker.internal:5080/api/settings/5429446779584916010`
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
* URL: http://host.docker.internal:5080/api/transportation/3910861620311501804
  * Node Name: `http://host.docker.internal:5080/api/transportation/3910861620311501804`
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
* URL: http://host.docker.internal:5080/api/users/187958429646447735
  * Node Name: `http://host.docker.internal:5080/api/users/187958429646447735`
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
* URL: http://host.docker.internal:5080/api/users/id/7059988625254640924
  * Node Name: `http://host.docker.internal:5080/api/users/id/7059988625254640924`
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
* URL: http://host.docker.internal:5080/api/users/me/8193013202906655040
  * Node Name: `http://host.docker.internal:5080/api/users/me/8193013202906655040`
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
* URL: http://host.docker.internal:5080/swagger/1136966957994023255
  * Node Name: `http://host.docker.internal:5080/swagger/1136966957994023255`
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
* URL: http://host.docker.internal:5080/swagger/v1/4565821560767509904
  * Node Name: `http://host.docker.internal:5080/swagger/v1/4565821560767509904`
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

* URL: http://host.docker.internal:5080/api/Auth/me
  * Node Name: `http://host.docker.internal:5080/api/Auth/me`
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
* URL: http://host.docker.internal:5080/api/Bookings/10/status
  * Node Name: `http://host.docker.internal:5080/api/Bookings/10/status ()({status})`
  * Method: `PATCH`
  * Parameter: ``
  * Attack: ``
  * Evidence: `PATCH `
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

### [ Storable and Cacheable Content ](https://www.zaproxy.org/docs/alerts/10049/)



##### Informational (Medium)

### Description

The response contents are storable by caching components such as proxy servers, and may be retrieved directly from the cache, rather than from the origin server by the caching servers, in response to similar requests from other users. If the response data is sensitive, personal or user-specific, this may result in sensitive information being leaked. In some cases, this may even result in a user gaining complete control of the session of another user, depending on the configuration of the caching components in use in their environment. This is primarily an issue where "shared" caching servers such as "proxy" caches are configured on the local network. This configuration is typically found in corporate or educational environments, for instance.

* URL: http://host.docker.internal:5080/swagger/v1/swagger.json
  * Node Name: `http://host.docker.internal:5080/swagger/v1/swagger.json`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: `In the absence of an explicitly specified caching lifetime directive in the response, a liberal lifetime heuristic of 1 year was assumed. This is permitted by rfc7234.`


Instances: 1

### Solution

Validate that the response does not contain sensitive, personal or user-specific information. If it does, consider the use of the following HTTP response headers, to limit, or prevent the content being stored and retrieved from the cache by another user:
Cache-Control: no-cache, no-store, must-revalidate, private
Pragma: no-cache
Expires: 0
This configuration directs both HTTP 1.0 and HTTP 1.1 compliant caching servers to not store the response, and to not retrieve the response (without validation) from the cache, in response to a similar request.

### Reference


* [ https://datatracker.ietf.org/doc/html/rfc7234 ](https://datatracker.ietf.org/doc/html/rfc7234)
* [ https://datatracker.ietf.org/doc/html/rfc7231 ](https://datatracker.ietf.org/doc/html/rfc7231)
* [ https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html ](https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html)


#### CWE Id: [ 524 ](https://cwe.mitre.org/data/definitions/524.html)


#### WASC Id: 13

#### Source ID: 3


