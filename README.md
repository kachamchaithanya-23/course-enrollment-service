# Course Waitlist & Enrollment Service Slice (.NET)

A bounded, thread-safe service slice implementing course enrollment, dropping, and FIFO waitlist promotion in .NET 10 (C#) with comprehensive automated testing and complete assessment deliverables.

---

## Deliverables Index

1. **Source Code**:
   * Domain aggregate and core models: [`src/CourseEnrollment.Core/Models/`](src/CourseEnrollment.Core/Models)
   * Prerequisite & waitlist policies: [`src/CourseEnrollment.Core/Policies/`](src/CourseEnrollment.Core/Policies)
   * Enrollment service: [`src/CourseEnrollment.Core/Services/CourseEnrollmentService.cs`](src/CourseEnrollment.Core/Services/CourseEnrollmentService.cs)
2. **Automated Tests**:
   * 25 tests covering all acceptance criteria: [`tests/CourseEnrollment.Tests/`](tests/CourseEnrollment.Tests)
3. **Design Specification**:
   * Domain model, invariants, FIFO rules, and trade-offs: [`docs/DESIGN_SPEC.md`](docs/DESIGN_SPEC.md)
4. **AI Prompt & Use Record**:
   * Prompts, AI outputs, human reviews/modifications, and verification: [`docs/AI_PROMPT_RECORD.md`](docs/AI_PROMPT_RECORD.md)
5. **Setup & Run Instructions**:
   * Build and test execution commands: [`docs/SETUP_AND_RUN.md`](docs/SETUP_AND_RUN.md)
6. **Limitations & Follow-Up Risks**:
   * Scalability, concurrency boundaries, and domain trade-offs: [`docs/LIMITATIONS.md`](docs/LIMITATIONS.md)
7. **Scorecard**:
   * Rubric assessment: [`Course-Registration-Rubric-Score.md`](Course-Registration-Rubric-Score.md)

---

## Quick Start: Running Tests

```bash
# Run all 25 automated tests
dotnet test --logger "console;verbosity=normal"
```
