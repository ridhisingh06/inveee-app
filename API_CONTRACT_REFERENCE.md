# API Contract Reference - Login & Approval Flow

## Registration API

### Register New User
```
POST /api/auth/register
Content-Type: application/json

Request Body:
{
  "username": "string (required)",
  "email": "string (required, valid email)",
  "password": "string (required, min 8 chars)",
  "designation": "string (required)",
  "departmentId": number (required),
  "roleId": number (required)
}

Response (200 OK):
{
  "message": "User registration successful. Awaiting admin approval."
}

Possible Status Codes:
- 200 OK: Registration successful
- 400 Bad Request: Missing fields or validation failed
- 500 Internal Server Error: Unexpected server error
```

---

## Login API

### Login
```
POST /api/auth/login
Content-Type: application/json

Request Body:
{
  "email": "string (required)",
  "password": "string (required)"
}

Response (200 OK):
{
  "token": "JWT token string",
  "message": "Login successful"
}

Response (403 Forbidden) - Pending Approval:
{
  "message": "Your account is pending admin approval."
}

Response (401 Unauthorized) - Invalid Credentials:
{
  "message": "Invalid credentials."
}

Response (400 Bad Request) - Account Issue:
{
  "message": "Your account is not approved yet"
}

Response (500 Internal Server Error):
{
  "message": "An internal server error occurred.",
  "developerMessage": "Exception details",
  "stackTrace": "Stack trace"
}

Possible Status Codes:
- 200 OK: Login successful, token provided
- 400 Bad Request: Validation failed or account not approved
- 401 Unauthorized: Invalid email/password
- 403 Forbidden: Account pending approval
- 500 Internal Server Error: Unexpected server error

JWT Token Structure:
Header: {
  "alg": "HS256",
  "typ": "JWT"
}

Payload: {
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "userId",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "user@example.com",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "username",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin|User|Issuer",
  "exp": 1234567890,
  "iss": "YourIssuer",
  "aud": "YourAudience"
}
```

---

## Admin Approval API

### Get Pending Users
```
GET /api/admin/pending-users?page=1&limit=50
Authorization: Bearer {adminToken}

Response (200 OK):
{
  "totalRecords": 100,
  "totalPages": 2,
  "currentPage": 1,
  "data": [
    {
      "id": 123,
      "username": "john_doe",
      "email": "john@example.com",
      "role": "User",
      "status": "Pending",
      "roleId": 2,
      "departmentId": 1,
      "departmentName": "Operations",
      "designation": "Manager",
      "createdAt": "2026-05-16T10:30:00Z"
    }
  ]
}

Possible Status Codes:
- 200 OK: Successfully retrieved pending users
- 401 Unauthorized: Invalid or missing token
- 403 Forbidden: User is not Admin
- 500 Internal Server Error: Unexpected server error
```

### Approve User Registration
```
PUT /api/admin/approve/{registrationRequestId}
Authorization: Bearer {adminToken}
Content-Type: application/json

Request Body:
{
  "roleId": number (required),
  "departmentId": number (required)
}

Response (200 OK):
{
  "message": "User approved successfully",
  "userId": 456,
  "email": "john@example.com",
  "isApproved": true,
  "isActive": true
}

Response (400 Bad Request) - Already Approved:
{
  "message": "User already approved",
  "isAlreadyApproved": true
}

Response (400 Bad Request) - Invalid Role/Department:
{
  "message": "Invalid RoleId provided."
}

Response (400 Bad Request) - Already Rejected:
{
  "message": "This request was rejected and cannot be approved"
}

Response (404 Not Found):
{
  "message": "Pending request not found"
}

Response (500 Internal Server Error):
{
  "message": "An internal server error occurred during approval.",
  "error": "Exception details"
}

Possible Status Codes:
- 200 OK: User approved successfully
- 400 Bad Request: Invalid request data or validation failed
- 401 Unauthorized: Invalid or missing token
- 403 Forbidden: User is not Admin
- 404 Not Found: Registration request not found
- 500 Internal Server Error: Unexpected server error

Side Effects:
1. Creates User record if doesn't exist
2. Sets User.IsActive = true
3. Creates/Updates UserRole mapping
4. Sets RegistrationRequest.Status = Approved
5. Records ApprovedAt timestamp
6. Records ApprovedBy user ID
```

### Reject User Registration
```
PUT /api/admin/reject/{registrationRequestId}
Authorization: Bearer {adminToken}
Content-Type: application/json

Request Body: {} (empty)

Response (200 OK):
{
  "message": "User rejected successfully",
  "isRejected": true
}

Response (404 Not Found):
{
  "message": "Pending request not found"
}

Response (500 Internal Server Error):
{
  "message": "An internal server error occurred during rejection.",
  "error": "Exception details"
}

Possible Status Codes:
- 200 OK: User rejected successfully
- 401 Unauthorized: Invalid or missing token
- 403 Forbidden: User is not Admin
- 404 Not Found: Registration request not found
- 500 Internal Server Error: Unexpected server error

Side Effects:
1. Sets RegistrationRequest.Status = Rejected
2. Sets RegistrationRequest.IsActive = false
3. Records ApprovedAt timestamp
4. Does NOT delete User record if it exists
```

---

## User Status Flow Diagram

```
Registration
    ↓
RegistrationRequest.Status = Pending
RegistrationRequest.IsActive = false
User.IsActive = false (if user created)
    ↓
[APPROVAL PATH]
    ↓
Admin calls /api/admin/approve/{id}
    ↓
RegistrationRequest.Status = Approved ✓
RegistrationRequest.IsActive = true ✓
User.IsActive = true ✓
    ↓
User can now Login → Receives JWT Token → Access Dashboard

[OR REJECTION PATH]
    ↓
Admin calls /api/admin/reject/{id}
    ↓
RegistrationRequest.Status = Rejected ✓
RegistrationRequest.IsActive = false
    ↓
User cannot Login → "Not approved yet" error → Cannot access system
```

---

## Error Codes Reference

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Proceed with login or use response data |
| 400 | Bad Request | Check request parameters and validation |
| 401 | Unauthorized | Check email/password or token validity |
| 403 | Forbidden | Check user approval status or admin role |
| 404 | Not Found | Verify resource ID exists |
| 500 | Server Error | Check backend logs for details |

---

## Field Validation Rules

### Registration/Login Email
- Must be valid email format: `user@domain.com`
- Must be unique (not already registered)
- Case-insensitive comparison

### Password
- Minimum 8 characters
- Stored as BCrypt hash
- Never returned in responses

### Username
- Required
- Case-sensitive
- Must be unique among approved users

### Department ID
- Must reference existing Department record
- Required during registration

### Role ID
- Must reference existing Role record
- Required during registration
- Can be changed during approval

---

## Authentication Flow

```
1. User registers
   ↓
2. Admin approves
   ↓
3. User logs in with email + password
   ↓
4. Server verifies:
   - User exists
   - IsActive = true
   - Password hash matches
   ↓
5. Server generates JWT with claims:
   - UserId
   - Email
   - Username
   - Roles (Admin, User, Issuer)
   ↓
6. JWT returned to client
   ↓
7. Client stores in localStorage
   ↓
8. Client includes in Authorization header for future requests
   ↓
9. Server validates JWT on protected endpoints
```

---

## Database State After Each Operation

### After Registration (Pending)
```
RegistrationRequests:
- Id: 123
- Email: user@example.com
- Status: Pending (0)
- IsActive: false
- ApprovedAt: null
- ApprovedBy: null

Users:
- (May or may not exist depending on implementation)
```

### After Approval
```
RegistrationRequests:
- Id: 123
- Email: user@example.com
- Status: Approved (1) ← CHANGED
- IsActive: true ← CHANGED
- ApprovedAt: 2026-05-16T10:30:00Z ← SET
- ApprovedBy: 1 (admin user id) ← SET

Users:
- Id: 456
- Email: user@example.com
- IsActive: true ← CHANGED TO TRUE
- CreatedAt: 2026-05-16T10:30:00Z

UserRoles:
- UserId: 456
- RoleId: 2 (User role)
```

### After Rejection
```
RegistrationRequests:
- Id: 123
- Email: user@example.com
- Status: Rejected (2) ← CHANGED
- IsActive: false ← CHANGED
- ApprovedAt: 2026-05-16T10:30:00Z ← SET

Users:
- (Unchanged or deleted depending on implementation)
```

---

## Rate Limiting Considerations

- No rate limiting currently implemented
- Consider adding rate limiting for:
  - Login attempts (prevent brute force)
  - Registration (prevent spam)
  - Approval operations (prevent abuse)

---

## Security Considerations

✓ Passwords hashed with BCrypt  
✓ JWT tokens signed with secret key  
✓ Admin operations require authorization  
✓ Email addresses masked in logs (PII protection)  
✓ Sensitive fields excluded from response (no password hashes)  
✓ CORS configured (currently allows any origin - review for production)  

---

## Future API Enhancements

1. Add pagination for login history
2. Add user suspension/deactivation endpoints
3. Add password reset flow
4. Add email verification
5. Add multi-factor authentication
6. Add rate limiting
7. Add API versioning
8. Add webhook for approval notifications
9. Add bulk approval endpoint
10. Add audit logging endpoint

---

## Item Requests API (Item Request Approval Workflow)

Status flow:
`REQUESTED (User) -> ISSUED (Issuer) -> APPROVED (Admin) -> RECEIVED (User)`

Rules:
- User cannot create a new request if any existing request is `REQUESTED`, `ISSUED`, or `APPROVED`.
- User can request again only after `REJECTED` or `RECEIVED`.

Endpoints:
- `POST /api/requests` (USER) Create request (status=`REQUESTED`, inserts `RequestItems`).
- `GET /api/requests` (role-based list)
  - USER: own requests
  - ISSUER: `REQUESTED` requests
  - ADMIN: `ISSUED` requests
- `PATCH /api/requests/{id}/issue` (ISSUER) `REQUESTED -> ISSUED` (+ stock reduction)
- `PATCH /api/requests/{id}/approve` (ADMIN) `ISSUED -> APPROVED`
- `PATCH /api/requests/{id}/reject` (ISSUER/ADMIN) Issuer rejects `REQUESTED`, Admin rejects `ISSUED`
- `PATCH /api/requests/{id}/receive` (USER) `APPROVED -> RECEIVED`
- `GET /api/requests/can-request` (USER) Returns `{ canRequest, message }`

---

**Last Updated**: 2026-05-16  
**Version**: 1.0  
**Status**: Current Implementation
