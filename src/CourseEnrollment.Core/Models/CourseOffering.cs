namespace CourseEnrollment.Core.Models;

using CourseEnrollment.Core.Policies;

/// <summary>
/// Domain aggregate root representing a single course offering with capacity and waitlist management.
/// Thread-safe for all state mutations and queries.
/// </summary>
public sealed class CourseOffering
{
    private readonly object _syncLock = new();
    private readonly Dictionary<string, Learner> _enrolled = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<WaitlistEntry> _waitlist = new();
    private readonly IPrerequisitePolicy _prerequisitePolicy;
    private readonly WaitlistIneligibilityPolicy _ineligibilityPolicy;

    public string CourseId { get; }
    public string Title { get; }
    public int Capacity { get; }
    public string? RequiredPrerequisite { get; }
    public WaitlistIneligibilityPolicy IneligibilityPolicy => _ineligibilityPolicy;

    public CourseOffering(
        string courseId,
        string title,
        int capacity,
        string? requiredPrerequisite = null,
        IPrerequisitePolicy? prerequisitePolicy = null,
        WaitlistIneligibilityPolicy ineligibilityPolicy = WaitlistIneligibilityPolicy.SkipAndRetain)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            throw new ArgumentException("Course ID cannot be null or empty.", nameof(courseId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Course Title cannot be null or empty.", nameof(title));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        CourseId = courseId.Trim();
        Title = title.Trim();
        Capacity = capacity;
        RequiredPrerequisite = string.IsNullOrWhiteSpace(requiredPrerequisite) ? null : requiredPrerequisite.Trim();
        _prerequisitePolicy = prerequisitePolicy ?? new StandardPrerequisitePolicy();
        _ineligibilityPolicy = ineligibilityPolicy;
    }

    /// <summary>
    /// Attempts to enroll a learner. If capacity is available and prerequisites are met,
    /// the learner is enrolled. If full and eligible, the learner joins the FIFO waitlist.
    /// Duplicate enrollments return safe, non-corrupting results.
    /// </summary>
    public EnrollmentResult Enroll(Learner learner)
    {
        ArgumentNullException.ThrowIfNull(learner);

        lock (_syncLock)
        {
            // Check for existing enrollment
            if (_enrolled.ContainsKey(learner.Id))
            {
                return EnrollmentResult.AlreadyEnrolled(CourseId, learner.Id);
            }

            // Check if already in waitlist
            int existingWaitlistPos = GetWaitlistPositionUnsafe(learner.Id);
            if (existingWaitlistPos > 0)
            {
                return EnrollmentResult.AlreadyWaitlisted(CourseId, learner.Id, existingWaitlistPos);
            }

            // Verify prerequisite eligibility
            if (!_prerequisitePolicy.IsEligible(this, learner, out string? failureReason))
            {
                return EnrollmentResult.PrerequisiteFailed(CourseId, learner.Id, failureReason ?? "Prerequisite criteria not met.");
            }

            // Seat available: Enroll
            if (_enrolled.Count < Capacity)
            {
                _enrolled[learner.Id] = learner;
                return EnrollmentResult.EnrolledSuccess(CourseId, learner.Id);
            }

            // Course is at capacity: Join FIFO waitlist
            _waitlist.AddLast(new WaitlistEntry(learner));
            int newPosition = _waitlist.Count;
            return EnrollmentResult.WaitlistedSuccess(CourseId, learner.Id, newPosition);
        }
    }

    /// <summary>
    /// Drops an enrolled learner from the course, opening a seat.
    /// If eligible learners are waiting, the earliest eligible learner is promoted to fill the seat.
    /// </summary>
    public DropResult Drop(string learnerId)
    {
        if (string.IsNullOrWhiteSpace(learnerId))
            return DropResult.Invalid(CourseId, learnerId ?? string.Empty, "Learner ID cannot be empty.");

        string trimmedLearnerId = learnerId.Trim();

        lock (_syncLock)
        {
            if (!_enrolled.Remove(trimmedLearnerId))
            {
                return DropResult.NotEnrolled(CourseId, trimmedLearnerId);
            }

            // A seat has opened. Attempt promotion from waitlist.
            Learner? promoted = PromoteEarliestEligibleUnsafe();

            if (promoted != null)
            {
                return DropResult.DroppedWithPromotion(CourseId, trimmedLearnerId, promoted.Id);
            }

            return DropResult.DroppedOnly(CourseId, trimmedLearnerId);
        }
    }

    /// <summary>
    /// Promotes the earliest eligible learner from the FIFO waitlist to enrolled.
    /// Ineligible learners are skipped or evicted based on the configured policy.
    /// Must be invoked within the lock.
    /// </summary>
    private Learner? PromoteEarliestEligibleUnsafe()
    {
        if (_enrolled.Count >= Capacity || _waitlist.Count == 0)
            return null;

        LinkedListNode<WaitlistEntry>? currentNode = _waitlist.First;

        while (currentNode != null)
        {
            LinkedListNode<WaitlistEntry>? nextNode = currentNode.Next;
            WaitlistEntry entry = currentNode.Value;

            if (_prerequisitePolicy.IsEligible(this, entry.Learner, out _))
            {
                // Found earliest eligible candidate!
                _waitlist.Remove(currentNode);
                _enrolled[entry.Learner.Id] = entry.Learner;
                return entry.Learner;
            }

            // Current candidate is ineligible at time of promotion
            if (_ineligibilityPolicy == WaitlistIneligibilityPolicy.SkipAndEvict)
            {
                _waitlist.Remove(currentNode);
            }
            // If SkipAndRetain, we leave currentNode in the waitlist and inspect nextNode

            currentNode = nextNode;
        }

        return null;
    }

    /// <summary>
    /// Returns the current 1-based position of a learner in the waitlist, or -1 if not found.
    /// Must be invoked within the lock.
    /// </summary>
    private int GetWaitlistPositionUnsafe(string learnerId)
    {
        int index = 1;
        foreach (WaitlistEntry entry in _waitlist)
        {
            if (string.Equals(entry.Learner.Id, learnerId, StringComparison.OrdinalIgnoreCase))
                return index;
            index++;
        }
        return -1;
    }

    /// <summary>
    /// Returns a thread-safe snapshot of the offering's state.
    /// </summary>
    public CourseOfferingStatus GetStatus()
    {
        lock (_syncLock)
        {
            var enrolledIds = _enrolled.Keys.ToList();
            var waitlistIds = _waitlist.Select(e => e.Learner.Id).ToList();

            return new CourseOfferingStatus(
                CourseId,
                Title,
                Capacity,
                _enrolled.Count,
                _waitlist.Count,
                enrolledIds,
                waitlistIds);
        }
    }
}

