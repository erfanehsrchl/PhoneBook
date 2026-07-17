# API Examples

Successful responses use `ApiResponse<T>`. Handled errors use `ApiResponse`, while application validation errors use `ValidationApiResponse` with a field-keyed `errors` dictionary. The body's `statusCode` always matches the HTTP status. JSON properties use camel case.

## Create Contact

```http
POST /api/contacts
Content-Type: application/json

{
  "firstName": "Amir",
  "lastName": "Iranmanesh",
  "phoneNumber": "09121234567",
  "tag": "Friend"
}
```

```http
HTTP/1.1 201 Created
Location: /api/contacts/8c67a591-aef4-4a72-9cdd-3f94786ce0a5
Content-Type: application/json

{
  "data": {
    "id": "8c67a591-aef4-4a72-9cdd-3f94786ce0a5",
    "firstName": "Amir",
    "lastName": "Iranmanesh",
    "phoneNumber": "+989121234567",
    "tag": "Friend",
    "createdAtUtc": "2026-07-17T08:30:00Z",
    "updatedAtUtc": null
  },
  "statusCode": 201,
  "message": "Contact created successfully.",
  "errorCode": null
}
```

## Get Contact

```http
GET /api/contacts/8c67a591-aef4-4a72-9cdd-3f94786ce0a5
```

```json
{
  "data": {
    "id": "8c67a591-aef4-4a72-9cdd-3f94786ce0a5",
    "firstName": "Amir",
    "lastName": "Iranmanesh",
    "phoneNumber": "+989121234567",
    "tag": "Friend",
    "createdAtUtc": "2026-07-17T08:30:00Z",
    "updatedAtUtc": null
  },
  "statusCode": 200,
  "message": "Contact retrieved successfully.",
  "errorCode": null
}
```

## Get All Contacts

Both list endpoints accept `pageNumber` and `pageSize`. Defaults are 1 and 20; `pageNumber` must be at least 1 and `pageSize` must be between 1 and 100.

```http
GET /api/contacts?pageNumber=1&pageSize=20
```

```json
{
  "data": {
    "items": [
      {
        "id": "8c67a591-aef4-4a72-9cdd-3f94786ce0a5",
        "firstName": "Amir",
        "lastName": "Iranmanesh",
        "phoneNumber": "+989121234567",
        "tag": "Friend",
        "createdAtUtc": "2026-07-17T08:30:00Z",
        "updatedAtUtc": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "statusCode": 200,
  "message": "Contacts retrieved successfully.",
  "errorCode": null
}
```

An empty result is still successful and returns `items: []`, `totalCount: 0`, and `totalPages: 0`.

## Get Contacts by Tag

Tag matching is case-insensitive. Filtering uses a dedicated route and is applied before pagination.

```http
GET /api/contacts/by-tag/Friend?pageNumber=1&pageSize=20
```

The response uses the same `ApiResponse<PagedResponse<ContactResponse>>` shape as Get All and includes only matching contacts.

## Update Contact

```http
PUT /api/contacts/8c67a591-aef4-4a72-9cdd-3f94786ce0a5
Content-Type: application/json

{
  "firstName": "Amir",
  "lastName": "Iranmanesh",
  "phoneNumber": "09357654321",
  "tag": "Coworker"
}
```

```json
{
  "data": {
    "id": "8c67a591-aef4-4a72-9cdd-3f94786ce0a5",
    "firstName": "Amir",
    "lastName": "Iranmanesh",
    "phoneNumber": "+989357654321",
    "tag": "Coworker",
    "createdAtUtc": "2026-07-17T08:30:00Z",
    "updatedAtUtc": "2026-07-17T09:15:00Z"
  },
  "statusCode": 200,
  "message": "Contact updated successfully.",
  "errorCode": null
}
```

## Delete Contact

```http
DELETE /api/contacts/8c67a591-aef4-4a72-9cdd-3f94786ce0a5
```

```http
HTTP/1.1 204 No Content
```

Successful deletion has no response body.

## Validation Error

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "statusCode": 400,
  "message": "One or more validation errors occurred.",
  "errors": {
    "firstName": [
      "First name is required."
    ]
  },
  "errorCode": "Validation.Failed"
}
```

Malformed JSON and other model-binding failures use error code `Request.Invalid` and message `The request is invalid.`.

## Not Found Error

```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "statusCode": 404,
  "message": "Contact was not found.",
  "errorCode": "Contact.NotFound"
}
```

## Conflict Error

```http
HTTP/1.1 409 Conflict
Content-Type: application/json

{
  "statusCode": 409,
  "message": "A contact with this phone number already exists.",
  "errorCode": "Contact.PhoneNumberConflict"
}
```

## Unexpected Error

Unexpected exceptions are logged and return no internal exception details:

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "statusCode": 500,
  "message": "An unexpected error occurred.",
  "errorCode": "Server.UnexpectedError"
}
```
