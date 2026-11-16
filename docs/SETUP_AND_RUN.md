# Setup and Run Instructions

This guide provides instructions to build, run, and execute the automated test suite for the **Course Waitlist & Enrollment Service**.

---

## Prerequisites
* **.NET SDK**: .NET 10.0 (or .NET 8.0+)
  Verify with:
  ```bash
  dotnet --version
  ```

---

## Project Structure
* `src/CourseEnrollment.Core/` — Core domain model, policies, and service slice.
* `tests/CourseEnrollment.Tests/` — Comprehensive xUnit test suite (unit, integration, concurrency).
* `CourseEnrollmentService.sln` — Visual Studio / .NET Solution.

---

## Build & Test Commands

### 1. Restore Dependencies
```bash
dotnet restore
```

### 2. Build Solution
```bash
dotnet build --no-restore
```

### 3. Run Automated Tests
To run all 25 tests with detailed test execution output:
```bash
dotnet test --no-build --logger "console;verbosity=normal"
```

Or standard execution:
```bash
dotnet test
```

### 4. Run Focused Test Fixtures
To run a specific fixture (e.g., concurrency or drop/promotion tests):
```bash
# Concurrency tests only
dotnet test --filter "FullyQualifiedName~ConcurrencyTests"

# Drop & Promotion tests only
dotnet test --filter "FullyQualifiedName~DropAndPromotionTests"

# Prerequisite policy tests only
dotnet test --filter "FullyQualifiedName~PrerequisitePolicyTests"

# Enrollment tests only
dotnet test --filter "FullyQualifiedName~EnrollmentTests"
```

