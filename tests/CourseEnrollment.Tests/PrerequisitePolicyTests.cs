namespace CourseEnrollment.Tests;

using CourseEnrollment.Core.Models;
using CourseEnrollment.Core.Policies;
using CourseEnrollment.Core.Services;

public class PrerequisitePolicyTests
{
    private readonly CourseEnrollmentService _service;
    private const string CourseId = "CS-201";
    private const string PrerequisiteId = "CS-101";

    public PrerequisitePolicyTests()
    {
        _service = new CourseEnrollmentService();
        var offering = new CourseOffering(
            CourseId,
            "Data Structures",
            capacity: 1,
            requiredPrerequisite: PrerequisiteId,
            ineligibilityPolicy: WaitlistIneligibilityPolicy.SkipAndRetain);
        _service.RegisterOffering(offering);
    }

    [Fact]
    public void Enroll_WithoutRequiredPrerequisite_FailsPrerequisiteCheck()
    {
        // Learner has NOT completed CS-101
        var ineligibleLearner = new Learner("L-100", "Ineligible Dan", completedPrerequisites: new[] { "MATH-101" });

        var result = _service.Enroll(CourseId, ineligibleLearner);

        Assert.Equal(EnrollmentStatus.PrerequisiteNotMet, result.Status);
        Assert.False(result.IsSuccess);

        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(0, status.EnrolledCount);
        Assert.Equal(0, status.WaitlistCount);
    }

    [Fact]
    public void Enroll_WithRequiredPrerequisite_Succeeds()
    {
        // Learner HAS completed CS-101
        var eligibleLearner = new Learner("L-101", "Eligible Eve", completedPrerequisites: new[] { PrerequisiteId });

        var result = _service.Enroll(CourseId, eligibleLearner);

        Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
        Assert.True(result.IsSuccess);

        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(1, status.EnrolledCount);
    }

    [Fact]
    public void Drop_WhenWaitlistHeadIsIneligible_SkipAndRetainPromotesNextEligibleLearner()
    {
        // Arrange:
        // Dynamic policy: we can toggle eligibility to simulate a learner becoming ineligible while on waitlist
        var dynamicPolicy = new TestableDynamicPrerequisitePolicy(PrerequisiteId);
        var offering = new CourseOffering(
            "CS-301",
            "Advanced Algorithms",
            capacity: 1,
            requiredPrerequisite: PrerequisiteId,
            prerequisitePolicy: dynamicPolicy,
            ineligibilityPolicy: WaitlistIneligibilityPolicy.SkipAndRetain);

        _service.RegisterOffering(offering);

        var enrolled = new Learner("L-1", "Enrolled Student", new[] { PrerequisiteId });
        var waitlist1 = new Learner("L-2", "Waitlisted Ineligible", new[] { PrerequisiteId });
        var waitlist2 = new Learner("L-3", "Waitlisted Eligible", new[] { PrerequisiteId });

        _service.Enroll("CS-301", enrolled);
        _service.Enroll("CS-301", waitlist1);
        _service.Enroll("CS-301", waitlist2);

        var preStatus = _service.GetStatus("CS-301")!;
        Assert.Equal(1, preStatus.EnrolledCount);
        Assert.Equal(2, preStatus.WaitlistCount);
        Assert.Equal(new[] { "L-2", "L-3" }, preStatus.WaitlistLearnerIds);

        // Simulate waitlist1 (L-2) losing eligibility (e.g. academic standing hold or prerequisite revocation)
        dynamicPolicy.RevokeEligibility("L-2");

        // Act: Enrolled student drops
        var dropResult = _service.Drop("CS-301", "L-1");

        // Assert:
        // L-1 dropped, L-2 was skipped (ineligible), L-3 was promoted!
        Assert.Equal(DropStatus.DroppedAndPromoted, dropResult.Status);
        Assert.Equal("L-3", dropResult.PromotedLearnerId);

        var postStatus = _service.GetStatus("CS-301")!;
        Assert.Equal(1, postStatus.EnrolledCount);
        Assert.Contains("L-3", postStatus.EnrolledLearnerIds);

        // L-2 was retained in the waitlist at position 1
        Assert.Equal(1, postStatus.WaitlistCount);
        Assert.Equal(new[] { "L-2" }, postStatus.WaitlistLearnerIds);
    }

    [Fact]
    public void Drop_WhenWaitlistHeadIsIneligible_SkipAndEvictRemovesIneligibleLearner()
    {
        // Arrange with SkipAndEvict policy
        var dynamicPolicy = new TestableDynamicPrerequisitePolicy(PrerequisiteId);
        var offering = new CourseOffering(
            "CS-302",
            "Database Systems",
            capacity: 1,
            requiredPrerequisite: PrerequisiteId,
            prerequisitePolicy: dynamicPolicy,
            ineligibilityPolicy: WaitlistIneligibilityPolicy.SkipAndEvict);

        _service.RegisterOffering(offering);

        var enrolled = new Learner("L-1", "Enrolled", new[] { PrerequisiteId });
        var waitlist1 = new Learner("L-2", "Waitlisted Ineligible", new[] { PrerequisiteId });
        var waitlist2 = new Learner("L-3", "Waitlisted Eligible", new[] { PrerequisiteId });

        _service.Enroll("CS-302", enrolled);
        _service.Enroll("CS-302", waitlist1);
        _service.Enroll("CS-302", waitlist2);

        // Revoke L-2's eligibility
        dynamicPolicy.RevokeEligibility("L-2");

        // Act: L-1 drops
        var dropResult = _service.Drop("CS-302", "L-1");

        // Assert: L-2 is evicted, L-3 is promoted, waitlist is now empty
        Assert.Equal(DropStatus.DroppedAndPromoted, dropResult.Status);
        Assert.Equal("L-3", dropResult.PromotedLearnerId);

        var postStatus = _service.GetStatus("CS-302")!;
        Assert.Equal(1, postStatus.EnrolledCount);
        Assert.Contains("L-3", postStatus.EnrolledLearnerIds);
        Assert.Equal(0, postStatus.WaitlistCount);
    }

    private sealed class TestableDynamicPrerequisitePolicy : IPrerequisitePolicy
    {
        private readonly string _requiredPrereq;
        private readonly HashSet<string> _revokedLearnerIds = new(StringComparer.OrdinalIgnoreCase);

        public TestableDynamicPrerequisitePolicy(string requiredPrereq)
        {
            _requiredPrereq = requiredPrereq;
        }

        public void RevokeEligibility(string learnerId) => _revokedLearnerIds.Add(learnerId);

        public bool IsEligible(CourseOffering offering, Learner learner, out string? failureReason)
        {
            if (_revokedLearnerIds.Contains(learner.Id))
            {
                failureReason = $"Learner '{learner.Id}' has an eligibility hold.";
                return false;
            }

            if (!learner.CompletedPrerequisites.Contains(_requiredPrereq))
            {
                failureReason = $"Learner missing prerequisite '{_requiredPrereq}'.";
                return false;
            }

            failureReason = null;
            return true;
        }
    }
}

