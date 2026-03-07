# Online Course Management System

A .NET 8 microservices-based Online Course Management System implementing Clean Architecture, JWT authentication, role-based authorization, rate limiting, CORS, security headers, API versioning, and full CRUD operations with Entity Framework Core.

---

## Architecture Overview

```
OnlineCourseManagement/
├── src/
│   ├── UserService/                     # Handles auth, registration, JWT issuance
│   │   ├── UserService.Domain/          # Entities, Enums (no dependencies)
│   │   ├── UserService.Application/     # Business logic, Interfaces, DTOs
│   │   ├── UserService.Infrastructure/  # EF Core, Repositories, TokenService
│   │   └── UserService.API/             # Controllers, Middleware, Program.cs
│   └── CourseService/                   # Manages courses and enrollments
│       ├── CourseService.Domain/        # Entities (Course, Enrollment)
│       ├── CourseService.Application/   # Business logic, Interfaces, DTOs
│       ├── CourseService.Infrastructure/# EF Core, Repositories, UserServiceClient
│       └── CourseService.API/           # Controllers, Middleware, Program.cs
├── tests/
│   ├── UserService.Tests/               # xUnit tests: Auth, User, Token
│   └── CourseService.Tests/             # xUnit tests: Course, Enrollment, Search
├── docker-compose.yml
└── OnlineCourseManagement.sln
```

### Design Patterns & Principles
- **Clean Architecture** — strict dependency rule (Domain <- Application <- Infrastructure <- API)
- **Repository Pattern** — abstract data access behind interfaces
- **Service Pattern** — business logic isolated in Application layer services
- **Result Pattern** — `Result<T>` replaces exceptions for predictable flow control
- **Options Pattern** — strongly-typed configuration via `JwtSettings`, `UserServiceSettings`
- **Delegating Handler** — JWT token forwarded automatically from CourseService -> UserService
- **Async/Await** — all data access and controller actions are fully async

---

## Services

| Service       | Port  | Swagger UI                     |
|---------------|-------|-------------------------------|
| UserService   | 5001  | http://localhost:5001          |
| CourseService | 5002  | http://localhost:5002          |

> Swagger UI is disabled in Production environments.

---

## API Endpoints

All endpoints are versioned under `/api/v1/`. The version can be omitted and defaults to `v1`.

### UserService (`http://localhost:5001`)

| Method | Endpoint                        | Auth     | Rate Limit | Description                       |
|--------|---------------------------------|----------|------------|-----------------------------------|
| POST   | `/api/v1/auth/register`         | None     | 10/min     | Register as Student or Instructor |
| POST   | `/api/v1/auth/login`            | None     | 10/min     | Login and receive JWT token       |
| GET    | `/api/v1/users/{id}`            | JWT      | 100/min    | Get user by ID                    |
| GET    | `/api/v1/users/me`              | JWT      | 100/min    | Get current user profile          |

### CourseService (`http://localhost:5002`)

| Method | Endpoint                              | Role               | Description                                           |
|--------|---------------------------------------|--------------------|-------------------------------------------------------|
| GET    | `/api/v1/courses/search`              | Any (JWT)          | Search all active courses with filters (paginated)    |
| GET    | `/api/v1/courses/{id}`                | Any (JWT)          | Get any course by ID                                  |
| POST   | `/api/v1/courses`                     | Instructor         | Create a new course                                   |
| PUT    | `/api/v1/courses/{id}`                | Instructor         | Update own course                                     |
| DELETE | `/api/v1/courses/{id}`                | Instructor         | Soft-delete own course (no enrolled students)         |
| GET    | `/api/v1/enrollments/my-courses`      | Student            | Get enrolled courses (paginated)                      |
| POST   | `/api/v1/enrollments`                 | Student            | Self-enroll in a course                               |
| POST   | `/api/v1/enrollments`                 | Instructor         | Enroll any student in a course                        |
| POST   | `/api/v1/enrollments/bulk`            | Student            | Self-enroll in multiple courses at once                |
| POST   | `/api/v1/enrollments/bulk`            | Instructor         | Enroll a student in multiple courses at once           |

> **Note:** Course deletion is a soft delete (sets `IsActive = false`). Courses with enrolled students cannot be deleted. Students cannot unenroll once enrolled.

### Role-Based Access Summary

| Action                      | Instructor       | Student    |
|-----------------------------|:----------------:|:----------:|
| Create course               | Yes              | No         |
| Update own course           | Yes              | No         |
| Delete course (soft)        | Own, no enrollments | No       |
| Search all active courses   | Yes              | Yes        |
| Get course by ID            | Yes              | Yes        |
| View enrolled courses       | No               | Yes        |
| Enroll a student            | Any student      | Self only  |
| Bulk enroll                 | Any student      | Self only  |
| Unenroll                    | No               | No         |

### Course Search Query Parameters

`GET /api/v1/courses/search` accepts optional query parameters (available to all authenticated users):

| Parameter        | Type     | Validation                                    | Description                          |
|------------------|----------|-----------------------------------------------|--------------------------------------|
| `courseName`     | string   | Max 200 chars, letters/numbers/spaces/punctuation | Filter by course name (partial)    |
| `instructorName` | string   | Max 100 chars, letters/spaces/dots/hyphens only | Filter by instructor name (partial) |
| `startDate`      | datetime | Optional                                      | Filter courses starting after date   |
| `endDate`        | datetime | Optional                                      | Filter courses ending before date    |
| `pageNumber`     | int      | Min: 1                                        | Page number (default: 1)             |
| `pageSize`       | int      | 1–50                                          | Page size (default: 10)              |

Omit all filter parameters to return all active courses.

---

## Input Validation

### Registration (`POST /api/v1/auth/register`)

| Field      | Validations                                                                                       |
|------------|---------------------------------------------------------------------------------------------------|
| `fullName` | Required, 2–100 characters, letters/spaces/dots/apostrophes/hyphens only                         |
| `email`    | Required, valid email format, max 256 characters                                                  |
| `password` | Required, 8–100 characters, must include uppercase + lowercase + digit + special char (`@$!%*?&#`) |
| `role`     | Required (`1` = Student, `2` = Instructor)                                                        |

### Login (`POST /api/v1/auth/login`)

| Field      | Validations                                      |
|------------|--------------------------------------------------|
| `email`    | Required, valid email format, max 256 characters  |
| `password` | Required, max 100 characters                      |

### Create Course (`POST /api/v1/courses`)

| Field           | Validations                                                              |
|-----------------|--------------------------------------------------------------------------|
| `title`         | Required, 3–200 characters, letters/numbers/spaces/basic punctuation only |
| `description`   | Required, 10–2000 characters                                             |
| `instructorId`  | Required, valid GUID                                                      |
| `startDate`     | Required, cannot be in the past, must be before `endDate`                 |
| `endDate`       | Required, must be after `startDate`                                       |

### Update Course (`PUT /api/v1/courses/{id}`)

| Field         | Validations                                                              |
|---------------|--------------------------------------------------------------------------|
| `title`       | Required, 3–200 characters, letters/numbers/spaces/basic punctuation only |
| `description` | Required, 10–2000 characters                                             |
| `startDate`   | Required, must be before `endDate`                                        |
| `endDate`     | Required, must be after `startDate`                                       |

### Enroll Student (`POST /api/v1/enrollments`)

| Field       | Validations                                                                 |
|-------------|-----------------------------------------------------------------------------|
| `courseId`  | Required, valid GUID                                                         |
| `studentId` | Required for Instructors (valid GUID). Ignored for Students (taken from JWT) |

### Bulk Enroll (`POST /api/v1/enrollments/bulk`)

| Field       | Validations                                                                 |
|-------------|-----------------------------------------------------------------------------|
| `courseIds` | Required, 1–20 GUIDs                                                        |
| `studentId` | Required for Instructors (valid GUID). Ignored for Students (taken from JWT) |

---

## Running Locally (Without Docker)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Steps

**1. Start UserService:**
```bash
cd src/UserService/UserService.API
dotnet run
# Swagger: http://localhost:5001
```

**2. Start CourseService** (in a new terminal):
```bash
cd src/CourseService/CourseService.API
dotnet run
# Swagger: http://localhost:5002
```

**3. Run all tests:**
```bash
dotnet test OnlineCourseManagement.sln
```

---

## Running with Docker

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Steps
```bash
# Build and start both services
docker-compose up --build

# Stop services
docker-compose down
```

Services will be available at:
- UserService:  http://localhost:5001
- CourseService: http://localhost:5002

---

## Quick Start Guide (Testing with Swagger)

### 1. Register an Instructor
```
POST http://localhost:5001/api/v1/auth/register
{
  "fullName": "Dr. Jane Smith",
  "email": "jane@example.com",
  "password": "Password123!",
  "role": 2
}
```
> **Role values:** `1` = Student, `2` = Instructor

### 2. Register a Student
```
POST http://localhost:5001/api/v1/auth/register
{
  "fullName": "Alice Johnson",
  "email": "alice@example.com",
  "password": "Password123!",
  "role": 1
}
```

### 3. Login as Instructor to get JWT token
```
POST http://localhost:5001/api/v1/auth/login
{
  "email": "jane@example.com",
  "password": "Password123!"
}
```
Copy the `token` from the response.

### 4. Authorize in Swagger
Click the **Authorize** button in Swagger UI and paste the token.

### 5. Create a Course (as Instructor)
```
POST http://localhost:5002/api/v1/courses
{
  "title": "Introduction to C#",
  "description": "A comprehensive course on C# programming for beginners",
  "instructorId": "<instructor-guid-from-step-1>",
  "startDate": "2026-04-01T00:00:00Z",
  "endDate": "2026-06-30T00:00:00Z"
}
```

### 6. Enroll a Student

**Option A — Instructor enrolls a student:**
```
POST http://localhost:5002/api/v1/enrollments
{
  "courseId": "<course-guid>",
  "studentId": "<student-guid-from-step-2>"
}
```

**Option B — Student self-enrolls (login as Student first, then):**
```
POST http://localhost:5002/api/v1/enrollments
{
  "courseId": "<course-guid>"
}
```
> `studentId` is not needed for Students — it's automatically taken from the JWT token.

### 7. Bulk Enroll (multiple courses at once)

**As Student** — self-enroll in multiple courses:
```
POST http://localhost:5002/api/v1/enrollments/bulk
{
  "courseIds": ["<course-guid-1>", "<course-guid-2>"]
}
```

**As Instructor** — enroll a student in multiple courses:
```
POST http://localhost:5002/api/v1/enrollments/bulk
{
  "studentId": "<student-guid>",
  "courseIds": ["<course-guid-1>", "<course-guid-2>"]
}
```

Response includes per-course success/failure:
```json
{
  "enrolled": [{ "id": "...", "courseId": "...", "studentId": "...", "enrolledAt": "..." }],
  "failed": [{ "courseId": "...", "reason": "Already enrolled in this course." }]
}
```

### 8. Browse Courses

All authenticated users can search active courses:
```
GET http://localhost:5002/api/v1/courses/search?pageNumber=1&pageSize=10

# Filtered by instructor and date range
GET http://localhost:5002/api/v1/courses/search?instructorName=Jane&startDate=2026-01-01&pageSize=10
```

---

## Authentication & Authorization

- JWT tokens are issued by **UserService** and validated by **CourseService** using the shared secret.
- Tokens expire after **60 minutes**.
- Role claims (`Instructor` / `Student`) are embedded in the JWT and enforced via `[Authorize(Roles = "...")]`.
- CourseService automatically forwards the JWT to UserService when validating instructors/students via the `AuthorizationDelegatingHandler`.

---

## Security Features

### Rate Limiting
- **Auth endpoints** (`/auth/register`, `/auth/login`): 10 requests per 60 seconds — prevents brute force attacks
- **All other endpoints**: 100 requests per 60 seconds — general abuse prevention
- Returns `429 Too Many Requests` with JSON error body on limit exceeded

### CORS
- Configured per-environment via `appsettings.json` (`Cors:AllowedOrigins`)
- Default allowed origins: `http://localhost:3000`, `http://localhost:4200`
- Supports credentials, any header, any method

### Security Headers (applied to every response)
| Header | Value | Purpose |
|--------|-------|---------|
| `X-Frame-Options` | `DENY` | Prevents clickjacking |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME sniffing |
| `X-XSS-Protection` | `1; mode=block` | Legacy XSS protection |
| `Referrer-Policy` | `no-referrer` | Controls referrer leakage |
| `Permissions-Policy` | `camera=(), microphone=(), ...` | Disables browser features |
| `Content-Security-Policy` | `default-src 'none'` (API) / relaxed for Swagger | Blocks external resources on API endpoints |
| `Server` | *(removed)* | Hides server identity |

### Password Security
- Passwords are hashed using **BCrypt** with automatic salting — never stored in plain text
- BCrypt is a one-way hash; passwords cannot be decrypted or retrieved
- Minimum 8 characters with uppercase, lowercase, digit, and special character required

### Error Handling
All error responses follow a consistent JSON structure:
```json
{
  "statusCode": 401,
  "error": "Unauthorized",
  "message": "Your session has expired. Please log in again."
}
```

Status code mapping:
| HTTP Status | Error | Trigger |
|-------------|-------|---------|
| 400 | Bad Request | Validation errors, malformed input |
| 401 | Unauthorized | Missing/expired/invalid JWT |
| 403 | Forbidden | Insufficient role permissions |
| 404 | Not Found | Resource does not exist |
| 409 | Conflict | Duplicate resource (e.g., email already registered, already enrolled) |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unhandled server exceptions |

---

## API Versioning

- URL segment versioning: `/api/v{version}/[controller]`
- Current version: `v1`
- Default version is `v1` when omitted
- Version reported in response headers (`api-supported-versions`)
- Adding new versions does not break existing clients

---

## Key Technical Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Architecture | Clean Architecture | Clear separation of concerns, testable, dependency-inversion |
| Database | EF Core InMemory | Zero setup, fast for development/testing |
| Password hashing | BCrypt | Industry-standard, includes salt automatically |
| Error handling | Result<T> + Global Middleware | Consistent API responses, no exception-based control flow |
| Inter-service auth | Delegating Handler | Transparent JWT forwarding without polluting business logic |
| Pagination | PagedResult<T> | Reusable across both services |
| Logging | Serilog | Structured logging to console + rolling file |
| Rate limiting | ASP.NET Core FixedWindowLimiter | Built-in, no external dependencies |
| API versioning | Asp.Versioning URL segment | Clean URLs, easy to evolve API |

---

## Unit Tests Summary

| Test Project | Tests | Coverage |
|---|---|---|
| UserService.Tests | 22 | Auth register/login/inactive (positive+negative), User repository (positive+negative), JWT generation, signature validation, expiry, uniqueness |
| CourseService.Tests | 25 | Course CRUD by owner/non-owner/not-found, Enrollment conflict/inactive/wrong-role/not-found, Search by instructor/date/combined, Pagination, Empty results, Inactive exclusion, GetById positive+negative |
| **Total** | **47** | All passing |
