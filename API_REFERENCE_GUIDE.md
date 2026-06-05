# API Reference Guide - Inventory Management System

## Table of Contents
1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Response Format](#response-format)
4. [Status Codes](#status-codes)
5. [Auth Endpoints](#auth-endpoints)
6. [Inventory Endpoints](#inventory-endpoints)
7. [Request Endpoints](#request-endpoints)
8. [Personnel Endpoints](#personnel-endpoints)
9. [Bills Endpoints](#bills-endpoints)
10. [Admin Endpoints](#admin-endpoints)
11. [Error Examples](#error-examples)

---

## Overview

**Base URL (Production):**
```
https://api.invmgmt.com/api
```

**Base URL (Development):**
```
http://localhost:5000/api
```

**API Documentation (Swagger):**
```
http://localhost:5000/swagger
```

**Health Check:**
```
GET http://localhost:5000/health
```

---

## Authentication

### JWT Token
All protected endpoints require a JWT token in the Authorization header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Structure
```
Header:
{
  "alg": "HS256",
  "typ": "JWT"
}

Payload:
{
  "sub": "user@example.com",
  "email": "user@example.com",
  "role": "USER",
  "userId": 1,
  "exp": 1717593600,
  "iat": 1717507200
}
```

### Token Expiration
- **Duration:** 24 hours
- **Refresh:** Re-login required
- **Header Format:** `Authorization: Bearer <token>`

---

## Response Format

### Success Response
```json
{
  "status": 200,
  "message": "Operation completed successfully",
  "data": { /* response data */ },
  "timestamp": "2026-06-05T10:30:00Z",
  "traceId": "0HMVJM52U0BHS:00000001"
}
```

### Paginated Response
```json
{
  "items": [ /* array of items */ ],
  "totalItems": 100,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

### Error Response
```json
{
  "message": "Error description",
  "traceId": "0HMVJM52U0BHS:00000001",
  "timestamp": "2026-06-05T10:30:00Z",
  "path": "/api/endpoint"
}
```

---

## Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request succeeded |
| 201 | Created | Resource created successfully |
| 400 | Bad Request | Invalid input or validation error |
| 401 | Unauthorized | Missing or invalid token |
| 403 | Forbidden | Insufficient permissions (approval pending) |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Duplicate item/conflict error |
| 500 | Server Error | Internal server error |
| 503 | Service Unavailable | Database connection failed |

---

## Auth Endpoints

### 1. Login
**Endpoint:** `POST /auth/login`

**Access:** Public (No authentication required)

**Request:**
```json
{
  "email": "user@gmail.com",
  "password": "password123"
}
```

**Response (Success - 200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "message": "Login successful"
}
```

**Response (Pending Approval - 403):**
```json
{
  "message": "Your registration is pending admin approval"
}
```

**Response (Invalid Credentials - 401):**
```json
{
  "message": "Invalid email or password"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@gmail.com","password":"password123"}'
```

---

### 2. Register
**Endpoint:** `POST /auth/register`

**Access:** Public (No authentication required)

**Request:**
```json
{
  "username": "John Doe",
  "email": "john@gmail.com",
  "password": "SecurePass123!",
  "departmentId": 2,
  "designation": "System Administrator"
}
```

**Response (Success - 200):**
```json
{
  "message": "Registration request submitted successfully. Please wait for admin approval."
}
```

**Response (Duplicate Email - 400):**
```json
{
  "message": "Email already exists"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username":"John Doe",
    "email":"john@gmail.com",
    "password":"SecurePass123!",
    "departmentId":2,
    "designation":"Admin"
  }'
```

---

## Inventory Endpoints

### 1. Get All Items
**Endpoint:** `GET /inventory`

**Access:** Authorized (USER, ADMIN, ISSUER)

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| categoryId | int | No | Filter by category |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |

**Response (200):**
```json
[
  {
    "id": 1,
    "name": "Laptop",
    "categoryId": 2,
    "category": "IT Related",
    "availableQuantity": 5,
    "totalQuantity": 10,
    "description": "Dell Laptop",
    "createdDate": "2026-05-01T10:00:00Z"
  },
  {
    "id": 2,
    "name": "Pen",
    "categoryId": 1,
    "category": "Stationary",
    "availableQuantity": 100,
    "totalQuantity": 200,
    "description": "Ballpoint pen",
    "createdDate": "2026-05-01T10:00:00Z"
  }
]
```

**cURL Example:**
```bash
curl -X GET http://localhost:5000/api/inventory \
  -H "Authorization: Bearer <token>"
```

---

### 2. Get Item by ID
**Endpoint:** `GET /inventory/{id}`

**Access:** Authorized (USER, ADMIN, ISSUER)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "id": 1,
  "name": "Laptop",
  "categoryId": 2,
  "category": "IT Related",
  "availableQuantity": 5,
  "totalQuantity": 10,
  "description": "Dell Laptop",
  "createdAt": "2026-05-01T10:00:00Z"
}
```

**cURL Example:**
```bash
curl -X GET http://localhost:5000/api/inventory/1 \
  -H "Authorization: Bearer <token>"
```

---

### 3. Add Item
**Endpoint:** `POST /inventory`

**Access:** Authorized (ADMIN, ISSUER)

**Request:**
```json
{
  "name": "Monitor",
  "categoryId": 2,
  "description": "22 inch LED Monitor",
  "totalQuantity": 15
}
```

**Response (201):**
```json
{
  "message": "Item Added Successfully"
}
```

**Response (Duplicate - 409):**
```json
{
  "message": "An item with the name \"Monitor\" already exists. Please use a different name."
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/inventory \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Monitor",
    "categoryId":2,
    "description":"22 inch LED Monitor",
    "totalQuantity":15
  }'
```

---

### 4. Update Item
**Endpoint:** `PUT /inventory/{id}`

**Access:** Authorized (ADMIN, ISSUER)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Request:**
```json
{
  "name": "Monitor 22 inch",
  "categoryId": 2,
  "description": "Updated description",
  "totalQuantity": 20
}
```

**Response (200):**
```json
{
  "message": "Item Updated Successfully"
}
```

**cURL Example:**
```bash
curl -X PUT http://localhost:5000/api/inventory/1 \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Monitor 22 inch",
    "categoryId":2,
    "description":"Updated description",
    "totalQuantity":20
  }'
```

---

### 5. Increase Stock
**Endpoint:** `PATCH /inventory/{id}/increase-stock`

**Access:** Authorized (ADMIN, ISSUER)

**Request:**
```json
{
  "quantity": 10
}
```

**Response (200):**
```json
{
  "message": "Stock increased successfully",
  "id": 1,
  "name": "Monitor",
  "categoryId": 2,
  "category": "IT Related",
  "availableQuantity": 15,
  "totalQuantity": 25
}
```

**cURL Example:**
```bash
curl -X PATCH http://localhost:5000/api/inventory/1/increase-stock \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"quantity":10}'
```

---

### 6. Decrease Stock
**Endpoint:** `PATCH /inventory/{id}/decrease-stock`

**Access:** Authorized (ADMIN, ISSUER)

**Request:**
```json
{
  "quantity": 5
}
```

**Response (200):**
```json
{
  "message": "Stock decreased successfully",
  "id": 1,
  "name": "Monitor",
  "categoryId": 2,
  "category": "IT Related",
  "availableQuantity": 5,
  "totalQuantity": 15
}
```

**Response (Insufficient Stock - 400):**
```json
{
  "message": "Insufficient stock. Available: 3, Requested: 5"
}
```

**cURL Example:**
```bash
curl -X PATCH http://localhost:5000/api/inventory/1/decrease-stock \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"quantity":5}'
```

---

### 7. Delete Item
**Endpoint:** `DELETE /inventory/{id}`

**Access:** Authorized (ADMIN, ISSUER)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "message": "Item Deleted"
}
```

**cURL Example:**
```bash
curl -X DELETE http://localhost:5000/api/inventory/1 \
  -H "Authorization: Bearer <token>"
```

---

## Request Endpoints

### 1. Create Request
**Endpoint:** `POST /request`

**Access:** Authorized (USER)

**Request:**
```json
{
  "categoryId": null,
  "items": [
    {
      "itemId": 1,
      "quantity": 2
    },
    {
      "itemId": 3,
      "quantity": 5
    }
  ]
}
```

**Response (201):**
```json
{
  "id": 42,
  "message": "Request created successfully"
}
```

**Response (Exceeds Limit - 400):**
```json
{
  "message": "Request quantity exceeds your role limit for item 'Monitor'"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/request \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "categoryId":null,
    "items":[
      {"itemId":1,"quantity":2},
      {"itemId":3,"quantity":5}
    ]
  }'
```

---

### 2. Get My Requests
**Endpoint:** `GET /request/my`

**Access:** Authorized (USER)

**Query Parameters:**
| Parameter | Type | Required | Default |
|-----------|------|----------|---------|
| pageNumber | int | No | 1 |
| pageSize | int | No | 10 |

**Response (200):**
```json
{
  "items": [
    {
      "id": 42,
      "status": "Pending",
      "createdAt": "2026-06-05T09:15:00Z",
      "updatedAt": "2026-06-05T09:15:00Z",
      "items": [
        {
          "id": 1,
          "itemId": 1,
          "itemName": "Monitor",
          "quantityRequested": 2,
          "quantityApproved": 0,
          "quantityIssued": 0,
          "status": "Pending"
        }
      ]
    }
  ],
  "totalItems": 5,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

**cURL Example:**
```bash
curl -X GET "http://localhost:5000/api/request/my?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer <token>"
```

---

### 3. Get Request Details
**Endpoint:** `GET /request/{id}`

**Access:** Authorized (USER, ADMIN, ISSUER)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "id": 42,
  "userId": 5,
  "status": "Approved",
  "createdAt": "2026-06-05T09:15:00Z",
  "updatedAt": "2026-06-05T10:20:00Z",
  "items": [
    {
      "id": 1,
      "itemId": 1,
      "itemName": "Monitor",
      "quantityRequested": 2,
      "quantityApproved": 2,
      "quantityIssued": 0,
      "status": "Approved"
    }
  ]
}
```

**cURL Example:**
```bash
curl -X GET http://localhost:5000/api/request/42 \
  -H "Authorization: Bearer <token>"
```

---

### 4. Approve Request (Admin)
**Endpoint:** `POST /request/approve/{id}`

**Access:** Authorized (ADMIN)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "message": "Request approved successfully"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/request/approve/42 \
  -H "Authorization: Bearer <token>"
```

---

### 5. Reject Request (Admin)
**Endpoint:** `POST /request/reject/{id}`

**Access:** Authorized (ADMIN)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "message": "Request rejected successfully"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/request/reject/42 \
  -H "Authorization: Bearer <token>"
```

---

### 6. Confirm Received
**Endpoint:** `POST /request/{id}/confirm-received`

**Access:** Authorized (USER)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "message": "Items marked as received successfully"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/request/42/confirm-received \
  -H "Authorization: Bearer <token>"
```

---

### 7. Get Pending Requests (Admin)
**Endpoint:** `GET /request/pending`

**Access:** Authorized (ADMIN)

**Query Parameters:**
| Parameter | Type | Required | Default |
|-----------|------|----------|---------|
| pageNumber | int | No | 1 |
| pageSize | int | No | 10 |

**Response (200):**
```json
{
  "items": [
    {
      "id": 42,
      "userId": 5,
      "status": "Pending",
      "createdAt": "2026-06-05T09:15:00Z",
      "items": [
        {
          "id": 1,
          "itemId": 1,
          "itemName": "Monitor",
          "quantityRequested": 2,
          "quantityApproved": 0,
          "quantityIssued": 0,
          "status": "Pending"
        }
      ]
    }
  ],
  "totalItems": 3,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

### 8. Delete Request (User)
**Endpoint:** `DELETE /request/{id}`

**Access:** Authorized (USER)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "message": "Request deleted successfully"
}
```

**Response (Not Allowed - 400):**
```json
{
  "message": "Cannot delete request in this status"
}
```

**cURL Example:**
```bash
curl -X DELETE http://localhost:5000/api/request/42 \
  -H "Authorization: Bearer <token>"
```

---

## Personnel Endpoints

### 1. Get All Personnel
**Endpoint:** `GET /personnel`

**Access:** Authorized (ADMIN)

**Query Parameters:**
| Parameter | Type | Required | Default |
|-----------|------|----------|---------|
| pageNumber | int | No | 1 |
| pageSize | int | No | 10 |
| searchTerm | string | No | null |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "name": "Rajesh Kumar",
      "email": "rajesh@company.com",
      "phone": "+91-9876543210",
      "designation": "IT Manager",
      "department": "IT",
      "photoUrl": "https://api.invmgmt.com/uploads/personnel/1.jpg",
      "dateOfBirth": "1990-05-15",
      "address": "123 Main Street, City",
      "createdAt": "2026-05-01T10:00:00Z"
    }
  ],
  "totalItems": 25,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

**cURL Example:**
```bash
curl -X GET "http://localhost:5000/api/personnel?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer <token>"
```

---

### 2. Add Personnel
**Endpoint:** `POST /personnel`

**Access:** Authorized (ADMIN)

**Request:**
```json
{
  "name": "Priya Singh",
  "email": "priya@company.com",
  "phone": "+91-9876543211",
  "designation": "HR Officer",
  "department": "HR",
  "dateOfBirth": "1992-08-20",
  "address": "456 Oak Avenue, City"
}
```

**Response (201):**
```json
{
  "id": 26,
  "name": "Priya Singh",
  "email": "priya@company.com",
  "phone": "+91-9876543211",
  "designation": "HR Officer",
  "department": "HR",
  "dateOfBirth": "1992-08-20",
  "address": "456 Oak Avenue, City",
  "createdAt": "2026-06-05T10:30:00Z"
}
```

**Response (Duplicate Email - 409):**
```json
{
  "message": "Email already exists"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/personnel \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Priya Singh",
    "email":"priya@company.com",
    "phone":"+91-9876543211",
    "designation":"HR Officer",
    "department":"HR",
    "dateOfBirth":"1992-08-20",
    "address":"456 Oak Avenue, City"
  }'
```

---

### 3. Update Personnel
**Endpoint:** `PUT /personnel/{id}`

**Access:** Authorized (ADMIN)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Request:**
```json
{
  "name": "Priya Singh",
  "phone": "+91-9876543212",
  "designation": "Senior HR Officer",
  "address": "789 Elm Street, City"
}
```

**Response (200):**
```json
{
  "message": "Personnel updated successfully"
}
```

---

### 4. Delete Personnel
**Endpoint:** `DELETE /personnel/{id}`

**Access:** Authorized (ADMIN)

**Parameters:**
| Parameter | Type | Location | Required |
|-----------|------|----------|----------|
| id | int | URL path | Yes |

**Response (200):**
```json
{
  "message": "Personnel deleted successfully"
}
```

---

## Bills Endpoints

### 1. Get All Bills
**Endpoint:** `GET /bills`

**Access:** Authorized (ADMIN, ISSUER)

**Query Parameters:**
| Parameter | Type | Required | Default |
|-----------|------|----------|---------|
| pageNumber | int | No | 1 |
| pageSize | int | No | 10 |
| fromDate | datetime | No | null |
| toDate | datetime | No | null |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "billNo": "BILL-2026-001",
      "createdByUserId": 3,
      "createdByName": "Admin User",
      "billDate": "2026-06-05",
      "totalAmount": 5000.00,
      "itemCount": 3,
      "createdAt": "2026-06-05T10:30:00Z"
    }
  ],
  "totalItems": 15,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 2
}
```

---

### 2. Get Bill Details
**Endpoint:** `GET /bills/{id}`

**Access:** Authorized (ADMIN, ISSUER)

**Response (200):**
```json
{
  "id": 1,
  "billNo": "BILL-2026-001",
  "createdByUserId": 3,
  "createdByName": "Admin User",
  "billDate": "2026-06-05",
  "totalAmount": 5000.00,
  "createdAt": "2026-06-05T10:30:00Z",
  "items": [
    {
      "id": 1,
      "itemId": 1,
      "itemName": "Monitor",
      "quantity": 2,
      "unitPrice": 1500.00,
      "subtotal": 3000.00
    }
  ]
}
```

---

### 3. Create Bill
**Endpoint:** `POST /bills`

**Access:** Authorized (ISSUER)

**Request:**
```json
{
  "billNo": "BILL-2026-002",
  "billDate": "2026-06-05",
  "items": [
    {
      "itemId": 1,
      "quantity": 2,
      "unitPrice": 1500.00
    }
  ]
}
```

**Response (201):**
```json
{
  "id": 2,
  "message": "Bill created successfully"
}
```

---

## Admin Endpoints

### 1. Get Pending Registrations
**Endpoint:** `GET /admin/registrations`

**Access:** Authorized (ADMIN)

**Query Parameters:**
| Parameter | Type | Required | Default |
|-----------|------|----------|---------|
| pageNumber | int | No | 1 |
| pageSize | int | No | 10 |

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "username": "John Doe",
      "email": "john@gmail.com",
      "departmentId": 2,
      "department": "IT",
      "designation": "System Administrator",
      "status": "Pending",
      "createdAt": "2026-06-04T15:30:00Z"
    }
  ],
  "totalItems": 3,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

### 2. Approve Registration
**Endpoint:** `PUT /admin/registrations/{id}/approve`

**Access:** Authorized (ADMIN)

**Response (200):**
```json
{
  "message": "Registration approved successfully"
}
```

---

### 3. Reject Registration
**Endpoint:** `PUT /admin/registrations/{id}/reject`

**Access:** Authorized (ADMIN)

**Response (200):**
```json
{
  "message": "Registration rejected successfully"
}
```

---

### 4. Get All Users
**Endpoint:** `GET /admin/users`

**Access:** Authorized (ADMIN)

**Response (200):**
```json
{
  "items": [
    {
      "id": 1,
      "username": "System Admin",
      "email": "admin@gmail.com",
      "department": "Admin",
      "role": "ADMIN",
      "isActive": true,
      "isApproved": true,
      "createdAt": "2026-05-01T10:00:00Z"
    }
  ],
  "totalItems": 25
}
```

---

## Error Examples

### 401 Unauthorized - Missing Token
```json
{
  "message": "Unauthorized"
}
```

### 401 Unauthorized - Invalid Token
```json
{
  "message": "Invalid token"
}
```

### 403 Forbidden - Insufficient Permissions
```json
{
  "message": "You do not have permission to access this resource"
}
```

### 404 Not Found
```json
{
  "message": "Request not found or access denied"
}
```

### 400 Bad Request - Validation Error
```json
{
  "message": "Validation failed",
  "errors": [
    "Email field is required",
    "Password must be at least 8 characters"
  ]
}
```

### 409 Conflict - Duplicate
```json
{
  "message": "An item with the name \"Monitor\" already exists. Please use a different name."
}
```

### 500 Internal Server Error
```json
{
  "message": "An internal server error occurred.",
  "traceId": "0HMVJM52U0BHS:00000001",
  "timestamp": "2026-06-05T10:30:00Z",
  "path": "/api/request"
}
```

---

## Rate Limiting

Currently, no rate limiting is implemented. However, it's recommended for production:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1717593600
```

---

## Pagination Standards

All list endpoints support pagination with the following parameters:

```
?pageNumber=1&pageSize=10
```

**Response:**
```json
{
  "items": [ /* array */ ],
  "totalItems": 100,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

---

## Testing with Postman

### Import Collection
1. Open Postman
2. Click "Import"
3. Use the Swagger JSON: `http://localhost:5000/swagger/v1/swagger.json`

### Set Authorization
1. Create a collection
2. Go to "Authorization" tab
3. Type: Bearer Token
4. Token: `{{token}}` (set in environment variables)

### Environment Variables
```json
{
  "base_url": "http://localhost:5000/api",
  "token": "eyJhbGc..."
}
```

---

## Common Issues & Solutions

### Issue: 403 Forbidden on Login
**Cause:** User registration is pending approval
**Solution:** Wait for admin approval or ask admin to approve registration

### Issue: 401 Unauthorized
**Cause:** Missing or expired token
**Solution:** Re-login to get a new token

### Issue: 409 Conflict - Duplicate Item
**Cause:** Item with same name already exists
**Solution:** Use a different name or modify the existing item

### Issue: 400 Bad Request - Exceeds Limit
**Cause:** Request quantity exceeds role-based item limit
**Solution:** Reduce quantity or contact admin to increase limit

---

## Support

For API issues or documentation updates, contact:
- **Developer:** development@invmgmt.com
- **Support:** support@invmgmt.com
- **Issues:** Report on internal issue tracker

Last Updated: **June 5, 2026**
