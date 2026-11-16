# AI Prompt and Use Record

In accordance with Acceptance Criteria 7 of the assessment, this record details all engineering activities conducted with AI assistance, documenting prompts, AI outputs, human reviews/modifications/rejections, and empirical verification.

---

## Activity 1: Problem Decomposition & Tech Stack Alignment

* **Prompt / Task**:
  > "Analyse and understand the problem statement and all details in the provided PDF. Explain clearly." followed by "For this, use .NET as the tech stack."
* **AI Output Summary**:
  * Decomposed the PDF into 6 functional pillars: Objective, Operations (Enroll, Drop, Promote), Acceptance Criteria (AC 1-7), Anti-goals (no frontend/unnecessary infra), Deliverables, and Ineligibility Policy options (`SkipAndRetain` vs. `SkipAndEvict`).
  * Proposed a clean .NET 10 solution structure with domain models, thread-safe services, comprehensive xUnit test suites, and documentation.
* **Evaluation & Decision**:
  * **Accepted**: Clean separation of `CourseOffering` aggregate, `IPrerequisitePolicy`, and `CourseEnrollmentService`.
  * **Accepted**: Selecting .NET 10 (matching the installed local runtime/SDK environment).
* **Verification**:
  * Verified local .NET SDK installation (`10.0.302`) via `dotnet --version`.

---

## Activity 2: Domain Model & Aggregate Design

* **Prompt / Task**:
  > Implement domain models for `Learner`, `CourseOffering`, `WaitlistEntry`, `EnrollmentResult`, `DropResult`, and prerequisite policies in .NET.
* **AI Output Summary**:
  * Created immutable value objects and aggregate root with lock-based concurrency control.
  * Formulated `WaitlistIneligibilityPolicy` supporting both `SkipAndRetain` (default) and `SkipAndEvict`.
* **Evaluation & Modifications**:
  * **Edited**: Initially, `CourseOffering` used a simple `List<Learner>` for waitlist. This was modified to use `LinkedList<WaitlistEntry>` to enable $O(1)$ removal of arbitrary nodes during promotion without shifting elements, while retaining exact arrival timestamps for FIFO ordering.
  * **Edited**: Standardized result objects (`EnrollmentResult`, `DropResult`) to use typed enums and factory methods instead of throwing exceptions for valid domain outcomes (e.g., duplicate drops or courses at capacity).
* **Verification**:
  * Compiled via `dotnet build --no-restore` resulting in 0 errors and 0 warnings.

---

## Activity 3: Test Suite Generation & Concurrency Testing

* **Prompt / Task**:
  > Implement xUnit test suites covering all criteria in AC 6: available capacity, full capacity, duplicate requests, FIFO promotion, prerequisite failures, invalid/repeated drops, and a focused concurrency test.
* **AI Output Summary**:
  * Generated four focused test fixtures:
    1. `EnrollmentTests.cs` (7 test cases)
    2. `PrerequisitePolicyTests.cs` (5 test cases)
    3. `DropAndPromotionTests.cs` (7 test cases)
    4. `ConcurrencyTests.cs` (3 test cases including 60 concurrent enrollments and simultaneous drop/enroll tasks).
* **Evaluation & Modifications**:
  * **Rejected / Edited**: The AI initially wrote `Assert.Single(results.Where(r => r.Status == EnrollmentStatus.Enrolled));` in `ConcurrencyTests.cs`.
  * **Fix Applied**: Modified to `Assert.Single(results, r => r.Status == EnrollmentStatus.Enrolled);` adhering strictly to xUnit Analyzer rule `xUnit2031`.
  * **Added**: Enhanced `Drop_SequentialDrops_PromotesWaitlistInStrictFifoOrder` to verify that previously promoted learners can also be dropped and will trigger subsequent promotions down the line.
* **Verification**:
  * Executed `dotnet test --no-build` via command line.
  * **Result**: All 25 unit, integration, and concurrency tests passed successfully in 422 ms.

---

## Activity 4: Ineligibility Policy Verification

* **Prompt / Task**:
  > Verify that ineligible waitlisted learners are skipped consistently and never promoted.
* **AI Output Summary**:
  * Implemented `TestableDynamicPrerequisitePolicy` to simulate a learner who met prerequisites at waitlist join time but had their eligibility revoked before a seat opened.
* **Evaluation & Decision**:
  * **Accepted**: Verified that `CS-301` skipped the revoked learner, promoted the next eligible student, and retained the ineligible student at the head of the waitlist (`SkipAndRetain`).
  * **Accepted**: Verified that `CS-302` evicted the ineligible learner when configured with `SkipAndEvict`.
* **Verification**:
  * Passed both dynamic policy tests in `PrerequisitePolicyTests.cs`.

