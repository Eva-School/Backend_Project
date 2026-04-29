# Vice Module Endpoints - Manual Test Checklist (11-23)

## 0) Setup
1. Start the API (Swagger should be available).
2. Make sure seeding is enabled (`RUN_SEED` is not `false`).
3. In Swagger, use `Authorize` with a Bearer token.
4. Important: test flow should discover real IDs first, then use them in quarter/final endpoints.

## 1) Login (JWT)

### StudentAffairs token (for Vice endpoints)
`POST /api/auth/login`
```json
{
  "username": "staff",
  "password": "Staff@123"
}
```
Use `accessToken` as `Authorization: Bearer <token>` in Swagger.

### Admin token (for approve endpoint)
`POST /api/auth/login`
```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

## 2) GET /api/vice/dashboard/cards (11)
### Auth
- None (`AllowAnonymous`)
### Expect
- `200` with 3 cards:
  - Teacher -> `/vice/teachers`
  - Student -> `/vice/students`
  - Grades -> `/vice/grades`

## 3) GET /api/vice/students (12)
### Auth
- `Student Affairs` or `StudentAffairs` token works
### Request (query)
- `year` = `junior|wheeler|senior`
- `department` = `OM|SD`
- `classId` optional

Example:
`GET /api/vice/students?year=senior&department=OM`

### Expect
- `200` array of students with:
  - `id`, `classId`, `studentCode`, `name`, `department`, `className`, `year`

Use this response as source of truth:
- Save one `studentId` and one `classId` from this list.
- If this call returns empty, do not continue to quarter/final endpoints before fixing filters.

## 4) POST /api/vice/students (13)
### Auth
- `StudentAffairs`
### Body
```json
{
  "firstName": "Ahmed",
  "middleName": "M",
  "lastName": "Ali",
  "studentCode": "2025123",
  "email": "student@example.com",
  "phone": "01000000000",
  "department": "OM",
  "year": "senior",
  "classId": 1
}
```
### Expect
- `200` created student object.

> Note: `classId` must belong to the requested `department` (service checks it).

## 5) PUT /api/vice/students/{studentId} (14)
### Auth
- `StudentAffairs`
### Body
Use the same schema as (13).

Example:
`PUT /api/vice/students/{studentId}`

### Expect
- `200` updated student object.

## 6) DELETE /api/vice/students/{studentId} (15)
### Auth
- `StudentAffairs`
### Expect
- `200` `{ message: "Student deleted successfully" }`

## 7) GET /api/Subjects (16)
### Auth
- Project controller uses `[Authorize]` (JWT required)
### Optional filter
- `year` = `junior|wheeler|senior`

Example:
`GET /api/Subjects?year=senior`

### Expect
- `200` list of subjects, filtered by `Stage` if `year` is provided.

## 8) PUT /api/vice/grades/quarter/subjects/{subjectId}/max-grades (16.1)
### Auth
- `StudentAffairs`
### Request
- `subjectId` from `GET /api/Subjects`
### Body
```json
{
  "maxQuarterGrades": { "q1": 25, "q2": 25, "q3": 25, "q4": 25 }
}
```
### Expect
- `200` with updated max grades object.

## 9) GET /api/vice/grades/quarter/students (17)
### Auth
- `StudentAffairs`
### Query
- `level` = `junior|wheeler|senior`
- `subjectId`
- `department` = `OM|SD`
- `classId` optional

Discovery order (to avoid empty response):
1. `GET /api/vice/students?year=senior&department=OM` -> pick `classId`.
2. `GET /api/Subjects?year=senior` -> pick `subjectId`.
3. Call quarter endpoint using those exact values.

Example:
`GET /api/vice/grades/quarter/students?level=senior&subjectId=<from-subjects>&department=OM&classId=<from-vice-students>`

### Expect
```json
{
  "status": "draft|locked",
  "maxQuarterGrades": { "q1": 12, "q2": 13, "q3": 12, "q4": 13 },
  "students": [
    { "studentId": "1", "studentName": "Ahmed Ali", "q1": 12, "q2": 13, "q3": 12, "q4": 13 }
  ]
}
```

> Tip: Seeding creates at least one `locked` sheet for testing. Pick the class/department that returns `status=locked`.

## 10) PUT /api/vice/grades/quarter/students (18)
### Auth
- `StudentAffairs`
### Body
```json
{
  "level": "senior",
  "subjectId": 1,
  "department": "OM",
  "classId": 1,
  "students": [
    { "studentId": "1", "q1": 10, "q2": 11, "q3": 9, "q4": 10 }
  ]
}
```
### Expect
- If sheet is **not locked**: `200` with `{ message, updatedCount }`
- If sheet is **locked**: service returns `updatedCount = 0` (controller still returns `200` with that value).

## 11) GET /api/vice/grades/final/students (19)
### Auth
- `StudentAffairs`
### Query
- `level` = `junior|wheeler|senior`
- `semester` = `1|2`
- `department` = `OM|SD`
- `classId` optional

Discovery order (to avoid empty response):
1. `GET /api/vice/students?year=senior&department=OM` -> pick `classId`.
2. Use the same `level`/`department`, then call final endpoint.

Example:
`GET /api/vice/grades/final/students?level=senior&semester=1&department=OM&classId=<from-vice-students>`

### Expect
```json
{
  "status": "draft|submitted|approved",
  "students": [
    { "studentId": "1", "studentName": "Ahmed Ali", "score": 85 }
  ]
}
```

## 12) PUT /api/vice/grades/final/students (20)
### Auth
- `StudentAffairs`
### Body
```json
{
  "level": "senior",
  "semester": 1,
  "department": "OM",
  "classId": 1,
  "grades": [
    { "studentId": "1", "score": 78 }
  ]
}
```
### Expect
- `200` on success
- `400` if grades are already approved/locked in that filter (service returns `updatedCount=0` and controller rejects).

## 13) POST /api/vice/grades/final/submit (21)
### Auth
- `StudentAffairs`
### Body
```json
{
  "level": "senior",
  "semester": 1,
  "department": "OM"
}
```
(`classId` is optional)

### Expect
- `200` `{ "message": "Final grades submitted for approval" }`

If you previously got:
`Cannot insert the value NULL into column 'Notes' ... ResultApprovals`
that was fixed in service code by setting default `Notes = ""` when creating approvals.

## 14) POST /api/admin/grades/final/approve (19.1)
### Auth
- `Admin`
### Body
```json
{
  "level": "senior",
  "semester": 1,
  "department": "OM",
  "classId": "1"
}
```
(`classId` optional)

### Expect
- `200` `{ "message": "Grades locked successfully" }`

## 15) GET /api/vice/grades/final/history (22)
### Auth
- `StudentAffairs`
### Query
- `studentId` (use `id` from student list)
- `subjectId` (use `GET /api/Subjects` subject id)

Example:
`GET /api/vice/grades/final/history?studentId=1&subjectId=1`

### Expect
- `200` list of history items (sorted desc by timestamp).

## 16) GET /api/vice/grades/dashboard (23)
### Auth
- `StudentAffairs`
### Expect
- `200` with:
  - `totalStudents`, `totalSubjects`
  - `quarterGradesPending`, `finalGradesPending`
  - `recentActivity` (10-20 entries typically)

## Quick troubleshooting for empty GETs
- Empty quarter/final tables usually mean wrong `classId` or wrong `subjectId` for selected `level/department`.
- Never hardcode `classId=1` unless it actually exists in `GET /api/vice/students` for that same filter.
- Always re-fetch IDs after reseeding.

