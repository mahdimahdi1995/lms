# Domain Reference — Enterprise LMS

## Roles

| Role | Capabilities |
|------|-------------|
| **Learner** | Browse published courses, request enrollment, view approved course content, complete lessons, take quizzes, view own progress |
| **Trainer** | Create and manage courses (draft/published), add lessons and quizzes, view enrollment lists for own courses |
| **Manager** | View their assigned team of Learners, approve or reject enrollment requests from their team, view team progress |
| **Admin** | Full access — manage all users, all courses, all enrollments; assign roles; access all data |

A Manager has a team: other users whose `ManagerId` field points to them. This self-referential
relationship in the `User` table determines which enrollment requests the Manager can approve
and which Learners appear in their team view.

---

## Entities

### User

The central entity. Every person in the system is a User with exactly one Role. Users are
never hard-deleted — when an Admin deletes a user, `IsDeleted` is set to `true` and PII
fields are anonymised (Email, FirstName, LastName set to null). The record stays in the
database to preserve the integrity of enrollment history and audit data.

**Business rules:**
- Email must be unique across the system
- A user may only have one active role at a time
- A Manager can only see and act on users whose `ManagerId` points to them
- Admin can see all users, including soft-deleted ones (via `IgnoreQueryFilters()`)

---

### Course

A learning unit created by a Trainer. Has two statuses:
- **Draft** — only visible to the creating Trainer and Admins; Learners and Managers cannot see it
- **Published** — visible in the course catalog to all authenticated users

Courses are tagged with zero or more Skills. When a Learner completes all lessons in a
published course, they automatically acquire all Skills associated with that course.

**Business rules:**
- Only the owning Trainer or an Admin can edit or delete a course
- Soft-deleted courses disappear from all queries automatically via EF Core global query filter
- A course must be Published before Learners can request enrollment

---

### Module

An organisational container that groups Lessons within a Course — think of it as a chapter
or section header ("Part 1: Foundations", "Part 2: Advanced Topics"). Modules are ordered
within their Course.

Modules have no business rules of their own. Progress is not tracked at the module level;
it is tracked at the Lesson level. For simple courses, a single Module containing all lessons
is perfectly valid.

---

### Lesson

The smallest content unit. Has a markdown body (rendered in the frontend), an optional video
URL, and an order position within its Module.

Access to lesson content is restricted: only Learners with an Approved enrollment, Trainers
who own the course, and Admins can view lesson content.

Completing a lesson creates a `Progress` record for that user and lesson. Course completion
percentage is calculated from these records at read time.

**Business rules:**
- A Lesson belongs to exactly one Module
- Lessons are ordered within their Module via the `Order` field
- A Lesson can have at most one Quiz

---

### Enrollment

A Learner's request to access a Course. Goes through an explicit approval workflow:

```
Learner requests → Pending
                       → Manager approves → Approved  (access granted to course content)
                       → Manager rejects  → Rejected  (access denied)
```

The Manager who can approve or reject is determined by the Learner's `ManagerId` field.
Only that Manager can act on a given Learner's request. Cross-team actions are rejected
with a 403 Forbidden response.

**Business rules:**
- Only Learners can create enrollment requests
- A Learner cannot have two active enrollments for the same course
- Only the Learner's assigned Manager can approve or reject their requests
- An Approved enrollment is required to access lesson content and submit quiz attempts
- Enrollment status moves forward only: Pending → Approved or Rejected (no reversals in MVP)

---

### Progress

Records which lessons a Learner has completed. One record per user per lesson — enforced by a
unique constraint. Marking a lesson complete is idempotent: calling it twice succeeds but
creates no duplicate (upsert pattern).

Course completion percentage is derived on read:

```
progressPercent = completedLessonCount / totalLessonCount × 100
```

When `progressPercent` reaches 100%:
1. The course is considered complete
2. A `UserSkill` record is created for each Skill tagged on that course, with `AcquiredAt = now`

**Business rules:**
- Only Learners with an Approved enrollment can mark lessons complete
- Progress records are visible to the Learner, their Manager, and Admins

---

### Quiz

A per-lesson multiple-choice assessment. A Lesson can have at most one Quiz. A Quiz contains
one or more Questions; each Question has multiple Options with exactly one marked as correct.

Score is calculated as a percentage: `correctAnswers / totalQuestions × 100`. Multiple
attempts are allowed; each attempt is its own record with its own score and timestamp.

**Business rules:**
- Only Learners with an Approved enrollment can submit attempts
- Score is stored as an integer (0–100)
- Completing a quiz attempt (any score) counts toward lesson completion for progress tracking

---

### Skill

A tag representing a competency area (e.g., "Project Management", "SQL", "Leadership").
Skills are assigned to Courses by Trainers. When a Learner completes a Course, they
automatically acquire all Skills on that Course — creating a `UserSkill` record with
an `AcquiredAt` timestamp.

Skills form the foundation of the Skill Profile feature (stretch): a Learner's profile
shows all acquired Skills and when each was earned.

---

### Learning Path *(stretch feature)*

An ordered sequence of Courses that form a structured training program. Courses in a
Learning Path can have prerequisites — a Learner must complete Course A before Course B
becomes accessible.

When a Learner is assigned to a Learning Path, the system creates individual Enrollment
requests for each Course in the path. The Course enrollment remains the access-control
primitive; the Learning Path is a convenience assignment layer on top.

**Business rules:**
- Courses are ordered within a path via `LearningPathCourse.Order`
- `PrerequisiteCourseId` optionally requires completion of another course in the same path before this one unlocks
- Assigned to Learners by Managers or Admins
