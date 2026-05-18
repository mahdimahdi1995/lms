# Database Schema — ER Diagram

```mermaid
erDiagram
    User {
        uuid    Id                  PK
        string  Email               UK
        string  PasswordHash
        string  FirstName
        string  LastName
        string  Role                "Learner | Trainer | Manager | Admin"
        uuid    ManagerId           FK "nullable — self-ref"
        bool    IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    Course {
        uuid    Id                  PK
        string  Title
        string  Description
        string  CoverUrl            "nullable"
        string  Status              "Draft | Published"
        uuid    TrainerId           FK
        bool    IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    Module {
        uuid    Id                  PK
        uuid    CourseId            FK
        string  Title
        int     Order
    }

    Lesson {
        uuid    Id                  PK
        uuid    ModuleId            FK
        string  Title
        text    Body                "Markdown"
        string  VideoUrl            "nullable"
        int     Order
    }

    Skill {
        uuid    Id                  PK
        string  Name                UK
        string  Description
    }

    CourseSkill {
        uuid    CourseId            FK
        uuid    SkillId             FK
    }

    UserSkill {
        uuid    UserId              FK
        uuid    SkillId             FK
        datetime AcquiredAt
    }

    LearningPath {
        uuid    Id                  PK
        string  Title
        string  Description
        datetime CreatedAt
    }

    LearningPathCourse {
        uuid    LearningPathId          FK
        uuid    CourseId                FK
        int     Order
        uuid    PrerequisiteCourseId    FK "nullable — must complete before this unlocks"
    }

    Enrollment {
        uuid    Id                  PK
        uuid    UserId              FK "the Learner"
        uuid    CourseId            FK
        string  Status              "Pending | Approved | Rejected"
        datetime RequestedAt
        datetime ReviewedAt         "nullable"
        uuid    ReviewedByUserId    FK "nullable — the Manager"
    }

    Progress {
        uuid    Id                  PK
        uuid    UserId              FK
        uuid    LessonId            FK
        datetime CompletedAt
    }

    Quiz {
        uuid    Id                  PK
        uuid    LessonId            FK  "1-to-1"
        string  Title
    }

    QuizQuestion {
        uuid    Id                  PK
        uuid    QuizId              FK
        string  Text
        int     Order
    }

    QuizOption {
        uuid    Id                  PK
        uuid    QuizQuestionId      FK
        string  Text
        bool    IsCorrect
    }

    QuizAttempt {
        uuid    Id                  PK
        uuid    UserId              FK
        uuid    QuizId              FK
        int     Score               "0-100 percentage"
        datetime AttemptedAt
    }

    User            ||--o{ Course              : "creates (Trainer)"
    User            ||--o{ Enrollment          : "requests (Learner)"
    User            ||--o{ Enrollment          : "reviews (Manager)"
    User            ||--o{ Progress            : "tracks"
    User            ||--o{ QuizAttempt         : "attempts"
    User            ||--o{ UserSkill           : "acquires"
    User            ||--o| User                : "managed by (ManagerId)"
    Course          ||--o{ Module              : "contains"
    Module          ||--o{ Lesson              : "contains"
    Lesson          ||--o| Quiz                : "has (optional)"
    Quiz            ||--o{ QuizQuestion        : "contains"
    QuizQuestion    ||--o{ QuizOption          : "has"
    QuizAttempt     }o--|| Quiz                : "for"
    Course          ||--o{ CourseSkill         : "tagged with"
    Skill           ||--o{ CourseSkill         : "used in"
    Skill           ||--o{ UserSkill           : "acquired as"
    LearningPath    ||--o{ LearningPathCourse  : "orders"
    Course          ||--o{ LearningPathCourse  : "part of"
    Enrollment      }o--|| Course              : "for"
    Progress        }o--|| Lesson              : "completes"
```

## Key Design Decisions

**UUID primary keys** — no auto-increment integers. Avoids enumeration attacks, works naturally with distributed systems, and simplifies multi-tenant isolation if added later.

**Soft delete on User and Course** — `IsDeleted` flag; EF Core global query filter hides them automatically from all queries. Right-to-erasure (GDPR) is handled by anonymising PII fields (null out Email, FirstName, LastName) rather than physical deletion, which would break enrollment history and audit trails.

**Progress is per-lesson, not per-course** — course progress % is derived on read: `completedLessons / totalLessons`. We don't save a single percentage number for the whole course. Instead, we save a completion record for every lesson a user finishes. When the dashboard loads, the app counts those records to calculate progress. This ensures the percentage is always accurate, even if a trainer adds or removes lessons after some learners have already started.

**Enrollment is per-course** — when a learner joins a Learning Path, the system creates individual enrollments for each course in the path. The course is still the access-control primitive: if you are not enrolled in a specific course, you cannot see its lessons, regardless of path membership. Benefits of this design:

- **Security**: the access check is a single query — "is the user enrolled in this course?"
- **Flexibility**: a course can be assigned standalone without belonging to a path
- **Independence**: deleting a Learning Path does not remove a learner's course progress
- **Simpler code**: no multi-level checks (Path → Course → Lesson); the logic stays flat
- **Clearer reporting**: managers can see exactly which courses each team member has completed

**QuizAttempt stores score as 0–100** — percentage is cleaner to display than raw counts. Multiple attempts are allowed; each attempt has its own record so history is preserved.

**Module is organisational only** — a Module is a label that groups lessons into sections (e.g., "Part 1: Foundations"). It has no business rules of its own. Learners do not enroll in modules and progress is not tracked at the module level. The system only tracks individual lessons, which keeps the data model flat and the queries simple.
