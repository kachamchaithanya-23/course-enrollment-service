# Limitations and Follow-Up Risks

This document provides a concise summary of known limitations and potential follow-up risks within the bounded scope of this course enrollment and waitlist service slice.

---

## 1. Architectural & Scoping Limitations

1. **In-Memory Volatility**:
   * **Limitation**: State is held in-memory within `CourseOffering` and `CourseEnrollmentService`. Restarting the host process resets enrollment and waitlist queues.
   * **Follow-up**: For production systems, integrate an abstraction over durable storage (e.g., PostgreSQL with optimistic/pessimistic locking or event sourcing with Marten/Kafka).

2. **Single-Instance Locking**:
   * **Limitation**: Concurrency control uses in-process synchronization (`lock (_syncLock)`). This ensures thread-safety within a single application instance, but does not extend to multi-node horizontal scaling.
   * **Follow-up**: In a distributed multi-node deployment, replace in-process locks with distributed locks (e.g., Redis Redlock) or database row-level locking (`SELECT ... FOR UPDATE` or SQL transactions with Serializable isolation level).

3. **Single Offering Scope**:
   * **Limitation**: By requirement, the service models a single course offering and does not enforce cross-course schedule conflicts, maximum student credit limits, or co-requisites across offerings.
   * **Follow-up**: Introduce a `StudentAcademicProfile` aggregate that validates aggregate semester credit caps and timetables before dispatching enrollment.

---

## 2. Domain & Policy Trade-offs

1. **`SkipAndRetain` Waitlist Invalidation Window**:
   * **Observation**: Under the `SkipAndRetain` policy, an ineligible learner is bypassed when a seat opens, but remains at the head of the line. If that learner never satisfies their prerequisite, they will be checked and skipped on every subsequent drop.
   * **Risk**: For long waitlists with frequent turnover, repeatedly evaluating ineligible students at the head adds marginal compute overhead.
   * **Mitigation**: Introduce a timeout or expiration mechanism (e.g., "Must satisfy prerequisite within 48 hours or be evicted") or periodically execute a waitlist sanitation sweep.

2. **Direct Promotion vs. Offer-and-Accept Flow**:
   * **Observation**: The current implementation automatically promotes waitlisted students immediately upon seat vacancy.
   * **Risk**: A student who joined the waitlist days or weeks prior may no longer want or be able to attend the course, yet they are automatically enrolled without confirmation.
   * **Mitigation**: In a complete enterprise system, promotion could transition to an `Offered` state with a time-to-live (e.g., 24-hour claim window) before rolling to the next waitlisted learner.

