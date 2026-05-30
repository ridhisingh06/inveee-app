# Personnel Module - Quick API Reference

## Base URL
```
https://localhost:5000/api/personnel
```

## Authentication
All endpoints require JWT token with `ADMIN` role in the Authorization header:
```
Authorization: Bearer {jwt_token}
```

---

## ENDPOINTS

### 1. CREATE PERSONNEL
```
POST /api/personnel
Content-Type: multipart/form-data
Authorization: Bearer {token}

Body (form-data):
  name*                 : string       (required, max 100)
  email*                : string       (required, email format, max 150)
  icNumber              : string       (max 20)
  birthDate             : date         (YYYY-MM-DD)
  residentialAddress    : text
  residentialPhone      : string       (max 20)
  officePhone           : string       (max 20)
  designation           : string       (max 100)
  jobDescription        : text
  department            : string       (max 100)
  isStoresIncharge      : boolean      (default: false)
  building              : string       (max 100)
  reportingOfficer      : string       (max 100)
  idCardNumber          : string       (max 30)
  idCardExpiryDate      : date         (YYYY-MM-DD)
  photo                 : file         (JPG/JPEG, max 2MB)

Response (201 Created):
{
  "message": "Personnel record created successfully.",
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    ...
    "photoUrl": "https://localhost:5000/uploads/personnel/guid.jpg",
    "createdAt": "2026-05-21T10:00:00Z"
  }
}
```

---

### 2. LIST PERSONNEL (Paginated)
```
GET /api/personnel?page=1&pageSize=20
Authorization: Bearer {token}

Query Parameters:
  page     : int        (default: 1, min: 1)
  pageSize : int        (default: 20, min: 1, max: 100)

Response (200 OK):
{
  "data": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com",
      ...
    },
    {
      "id": 2,
      "name": "Jane Doe",
      ...
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

### 3. GET SINGLE PERSONNEL
```
GET /api/personnel/{id}
Authorization: Bearer {token}

Path Parameters:
  id : int              (personnel ID)

Response (200 OK):
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "icNumber": "123456-78-9012",
  "birthDate": "1990-01-15",
  "residentialAddress": "123 Main St, City",
  "residentialPhone": "555-0001",
  "officePhone": "555-1000",
  "designation": "Senior Manager",
  "jobDescription": "Manages team and projects",
  "department": "IT",
  "isStoresIncharge": false,
  "building": "Building A",
  "reportingOfficer": "CEO",
  "idCardNumber": "ID-001",
  "idCardExpiryDate": "2030-12-31",
  "photoUrl": "https://localhost:5000/uploads/personnel/guid.jpg",
  "createdAt": "2026-05-21T10:00:00Z",
  "updatedAt": null
}

Error (404 Not Found):
{
  "message": "Personnel with id 999 not found."
}
```

---

### 4. UPDATE PERSONNEL
```
PUT /api/personnel/{id}
Content-Type: multipart/form-data
Authorization: Bearer {token}

Path Parameters:
  id : int              (personnel ID)

Body (form-data):
  name*                 : string       (required, max 100)
  email*                : string       (required, email format, max 150)
  icNumber              : string       (max 20)
  birthDate             : date         (YYYY-MM-DD)
  residentialAddress    : text
  residentialPhone      : string       (max 20)
  officePhone           : string       (max 20)
  designation           : string       (max 100)
  jobDescription        : text
  department            : string       (max 100)
  isStoresIncharge      : boolean      (default: false)
  building              : string       (max 100)
  reportingOfficer      : string       (max 100)
  idCardNumber          : string       (max 30)
  idCardExpiryDate      : date         (YYYY-MM-DD)
  photo                 : file         (JPG/JPEG, max 2MB, optional)

Response (200 OK):
{
  "message": "Personnel record updated successfully.",
  "data": {
    "id": 1,
    "name": "Jane Doe",
    ...
    "updatedAt": "2026-05-21T12:00:00Z"
  }
}

Errors:
  404 Not Found:
    {
      "message": "Personnel with id 999 not found."
    }
  
  409 Conflict (duplicate email):
    {
      "message": "Email 'john@example.com' is already in use by another record."
    }
```

---

### 5. DELETE PERSONNEL
```
DELETE /api/personnel/{id}
Authorization: Bearer {token}

Path Parameters:
  id : int              (personnel ID)

Response (200 OK):
{
  "message": "Personnel record deleted successfully."
}

Error (404 Not Found):
{
  "message": "Personnel with id 999 not found."
}
```

---

## ERROR RESPONSES

### 400 Bad Request
```json
{
  "message": "Validation failed.",
  "errors": {
    "Name": ["The Name field is required."],
    "Email": ["The Email field is required."]
  }
}
```

### 401 Unauthorized
```json
{
  "message": "Unauthorized"
}
```
(When token is missing or invalid)

### 403 Forbidden
```json
{
  "message": "Forbidden"
}
```
(When token doesn't have ADMIN role)

### 404 Not Found
```json
{
  "message": "Personnel with id 999 not found."
}
```

### 409 Conflict
```json
{
  "message": "A personnel record with email 'duplicate@example.com' already exists."
}
```

### 500 Internal Server Error
```json
{
  "message": "Failed to create personnel record."
}
```

---

## VALIDATION RULES

### Field Validations
- **name** (Required): Max 100 characters
- **email** (Required): Valid email format, Max 150 characters, Must be unique
- **icNumber**: Max 20 characters
- **birthDate**: Valid date format (YYYY-MM-DD)
- **residentialAddress**: Text (no limit specified)
- **residentialPhone**: Max 20 characters
- **officePhone**: Max 20 characters
- **designation**: Max 100 characters
- **jobDescription**: Text (no limit specified)
- **department**: Max 100 characters
- **isStoresIncharge**: Boolean (true/false)
- **building**: Max 100 characters
- **reportingOfficer**: Max 100 characters
- **idCardNumber**: Max 30 characters
- **idCardExpiryDate**: Valid date format (YYYY-MM-DD)
- **photo**: JPG/JPEG file, Max 2 MB

### Business Rules
1. Email must be unique (case-insensitive)
2. Name and Email are required
3. Photo must be JPG/JPEG format
4. Photo file size must not exceed 2 MB
5. Only ADMIN users can perform operations

---

## EXAMPLE CURL REQUESTS

### Create Personnel (with photo)
```bash
curl -X POST https://localhost:5000/api/personnel \
  -H "Authorization: Bearer {token}" \
  -F "name=John Doe" \
  -F "email=john@example.com" \
  -F "designation=Manager" \
  -F "department=IT" \
  -F "photo=@photo.jpg"
```

### List Personnel
```bash
curl -X GET "https://localhost:5000/api/personnel?page=1&pageSize=20" \
  -H "Authorization: Bearer {token}"
```

### Get Single Personnel
```bash
curl -X GET https://localhost:5000/api/personnel/1 \
  -H "Authorization: Bearer {token}"
```

### Update Personnel
```bash
curl -X PUT https://localhost:5000/api/personnel/1 \
  -H "Authorization: Bearer {token}" \
  -F "name=Jane Doe" \
  -F "email=jane@example.com" \
  -F "designation=Senior Manager"
```

### Delete Personnel
```bash
curl -X DELETE https://localhost:5000/api/personnel/1 \
  -H "Authorization: Bearer {token}"
```

---

## POSTMAN SETUP

1. **Set up Environment Variables:**
   - `base_url`: `https://localhost:5000`
   - `token`: Your JWT token (obtained from login)

2. **Create Personnel (POST)**
   - URL: `{{base_url}}/api/personnel`
   - Headers: `Authorization: Bearer {{token}}`
   - Body: form-data with all fields
   - Response: 201 Created

3. **List Personnel (GET)**
   - URL: `{{base_url}}/api/personnel?page=1&pageSize=20`
   - Headers: `Authorization: Bearer {{token}}`
   - Response: 200 OK

4. **Get by ID (GET)**
   - URL: `{{base_url}}/api/personnel/1`
   - Headers: `Authorization: Bearer {{token}}`
   - Response: 200 OK

5. **Update Personnel (PUT)**
   - URL: `{{base_url}}/api/personnel/1`
   - Headers: `Authorization: Bearer {{token}}`
   - Body: form-data with updated fields
   - Response: 200 OK

6. **Delete Personnel (DELETE)**
   - URL: `{{base_url}}/api/personnel/1`
   - Headers: `Authorization: Bearer {{token}}`
   - Response: 200 OK

---

## NOTES

- All timestamps are in UTC format
- Pagination defaults: page=1, pageSize=20
- Maximum pageSize: 100
- Photo paths are stored relative to wwwroot
- Photos are automatically deleted when personnel record is deleted
- Email comparison is case-insensitive for uniqueness check
- UpdatedAt is null until the first update

