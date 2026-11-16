namespace CourseEnrollment.Tests;

using CourseEnrollment.Core.Models;
using CourseEnrollment.Core.Services;

public class DropAndPromotionTests
{
    private readonly CourseEnrollmentService _service;
    private const string CourseId = "CS-101";

    public DropAndPromotionTests()
    {
        _service = new CourseEnrollmentService();
        var offering = new CourseOffering(CourseId, "Introduction to Computer Science", capacity: 2);
        _service.RegisterOffering(offering);
    }

    [Fact]
    public void Drop_WhenWaitlistIsEmpty_OpensSeatWithoutPromotion()
    {
        // Arrange
        _service.Enroll(CourseId, new Learner("L-1", "Alice"));
        _service.Enroll(CourseId, new Learner("L-2", "Bob"));

        // Act
        var result = _service.Drop(CourseId, "L-1");

        // Assert
        Assert.Equal(DropStatus.DroppedWithoutPromotion, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Null(result.PromotedLearnerId);

        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(1, status.EnrolledCount);
        Assert.Equal(1, status.AvailableSeats);
        Assert.DoesNotContain("L-1", status.EnrolledLearnerIds);
        Assert.Contains("L-2", status.EnrolledLearnerIds);
    }

    [Fact]
    public void Drop_WhenWaitlistHasLearners_PromotesEarliestLearnerInFifoOrder()
    {
        // Arrange: Fill course (capacity: 2) and add 2 to waitlist
        _service.Enroll(CourseId, new Learner("L-1", "Alice"));
        _service.Enroll(CourseId, new Learner("L-2", "Bob"));
        _service.Enroll(CourseId, new Learner("L-3", "Charlie")); // Waitlist 1
        _service.Enroll(CourseId, new Learner("L-4", "Diana"));   // Waitlist 2

        var preStatus = _service.GetStatus(CourseId)!;
        Assert.Equal(2, preStatus.WaitlistCount);
        Assert.Equal(new[] { "L-3", "L-4" }, preStatus.WaitlistLearnerIds);

        // Act: L-1 drops
        var dropResult = _service.Drop(CourseId, "L-1");

        // Assert
        Assert.Equal(DropStatus.DroppedAndPromoted, dropResult.Status);
        Assert.Equal("L-3", dropResult.PromotedLearnerId);

        var postStatus = _service.GetStatus(CourseId)!;
        Assert.Equal(2, postStatus.EnrolledCount);
        Assert.Contains("L-3", postStatus.EnrolledLearnerIds);
        Assert.DoesNotContain("L-1", postStatus.EnrolledLearnerIds);

        // L-4 is now head of the waitlist
        Assert.Equal(1, postStatus.WaitlistCount);
        Assert.Equal(new[] { "L-4" }, postStatus.WaitlistLearnerIds);
    }

    [Fact]
    public void Drop_SequentialDrops_PromotesWaitlistInStrictFifoOrder()
    {
        // Arrange: Fill 2 seats and 3 waitlist
        _service.Enroll(CourseId, new Learner("L-1", "Alice"));
        _service.Enroll(CourseId, new Learner("L-2", "Bob"));
        _service.Enroll(CourseId, new Learner("L-3", "Charlie")); // W1
        _service.Enroll(CourseId, new Learner("L-4", "Diana"));   // W2
        _service.Enroll(CourseId, new Learner("L-5", "Evan"));    // W3

        // Act 1: Drop L-2 -> promotes L-3
        var res1 = _service.Drop(CourseId, "L-2");
        Assert.Equal("L-3", res1.PromotedLearnerId);

        // Act 2: Drop L-1 -> promotes L-4
        var res2 = _service.Drop(CourseId, "L-1");
        Assert.Equal("L-4", res2.PromotedLearnerId);

        // Act 3: Drop L-3 (who was previously promoted!) -> promotes L-5
        var res3 = _service.Drop(CourseId, "L-3");
        Assert.Equal("L-5", res3.PromotedLearnerId);

        // Assert final state: enrolled are L-4 and L-5, waitlist is empty
        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(2, status.EnrolledCount);
        Assert.Equal(0, status.WaitlistCount);
        Assert.Equal(new[] { "L-4", "L-5" }, status.EnrolledLearnerIds);
    }

    [Fact]
    public void Drop_LearnerNotEnrolled_ReturnsLearnerNotEnrolledWithoutCorruptingState()
    {
        // Arrange
        _service.Enroll(CourseId, new Learner("L-1", "Alice"));
        var initialStatus = _service.GetStatus(CourseId)!;

        // Act: Try to drop learner who was never enrolled
        var result = _service.Drop(CourseId, "L-999");

        // Assert
        Assert.Equal(DropStatus.LearnerNotEnrolled, result.Status);
        Assert.False(result.IsSuccess);

        var currentStatus = _service.GetStatus(CourseId)!;
        Assert.Equal(initialStatus.EnrolledCount, currentStatus.EnrolledCount);
        Assert.Equal(initialStatus.AvailableSeats, currentStatus.AvailableSeats);
        Assert.Equal(initialStatus.WaitlistCount, currentStatus.WaitlistCount);
    }

    [Fact]
    public void Drop_DuplicateDropCalls_DoesNotCorruptStateOrTriggerDuplicatePromotions()
    {
        // Arrange: 2 enrolled, 1 waitlisted
        _service.Enroll(CourseId, new Learner("L-1", "Alice"));
        _service.Enroll(CourseId, new Learner("L-2", "Bob"));
        _service.Enroll(CourseId, new Learner("L-3", "Charlie"));

        // Act 1: First drop of L-1 -> promotes L-3
        var drop1 = _service.Drop(CourseId, "L-1");
        Assert.Equal(DropStatus.DroppedAndPromoted, drop1.Status);
        Assert.Equal("L-3", drop1.PromotedLearnerId);

        // Act 2: Second drop of same L-1
        var drop2 = _service.Drop(CourseId, "L-1");
        Assert.Equal(DropStatus.LearnerNotEnrolled, drop2.Status);
        Assert.False(drop2.IsSuccess);

        // Assert: Course state remains intact (capacity: 2, enrolled: 2 [L-2, L-3], waitlist: 0)
        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(2, status.EnrolledCount);
        Assert.Equal(0, status.WaitlistCount);
        Assert.Contains("L-2", status.EnrolledLearnerIds);
        Assert.Contains("L-3", status.EnrolledLearnerIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Drop_InvalidLearnerId_ReturnsInvalidRequest(string? invalidLearnerId)
    {
        var result = _service.Drop(CourseId, invalidLearnerId!);
        Assert.Equal(DropStatus.InvalidRequest, result.Status);
    }

    [Fact]
    public void Drop_NonExistentCourse_ReturnsCourseNotFound()
    {
        var result = _service.Drop("UNKNOWN-COURSE", "L-1");
        Assert.Equal(DropStatus.CourseNotFound, result.Status);
    }
}

