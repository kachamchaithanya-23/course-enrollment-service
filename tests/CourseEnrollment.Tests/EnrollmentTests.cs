namespace CourseEnrollment.Tests;

using CourseEnrollment.Core.Models;
using CourseEnrollment.Core.Services;

public class EnrollmentTests
{
    private readonly CourseEnrollmentService _service;
    private const string CourseId = "CS-101";

    public EnrollmentTests()
    {
        _service = new CourseEnrollmentService();
        var offering = new CourseOffering(CourseId, "Introduction to Computer Science", capacity: 2);
        _service.RegisterOffering(offering);
    }

    [Fact]
    public void Enroll_WhenCapacityAvailable_SucceedsWithEnrolledStatus()
    {
        // Arrange
        var learner = new Learner("L-001", "Alice");

        // Act
        var result = _service.Enroll(CourseId, learner);

        // Assert
        Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Null(result.WaitlistPosition);

        var status = _service.GetStatus(CourseId);
        Assert.NotNull(status);
        Assert.Equal(1, status.EnrolledCount);
        Assert.Equal(1, status.AvailableSeats);
        Assert.Contains("L-001", status.EnrolledLearnerIds);
        Assert.Empty(status.WaitlistLearnerIds);
    }

    [Fact]
    public void Enroll_UpToCapacity_FillsCourseCompletely()
    {
        // Arrange & Act
        var res1 = _service.Enroll(CourseId, new Learner("L-001", "Alice"));
        var res2 = _service.Enroll(CourseId, new Learner("L-002", "Bob"));

        // Assert
        Assert.Equal(EnrollmentStatus.Enrolled, res1.Status);
        Assert.Equal(EnrollmentStatus.Enrolled, res2.Status);

        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(2, status.EnrolledCount);
        Assert.Equal(0, status.AvailableSeats);
        Assert.True(status.IsFull);
        Assert.Equal(0, status.WaitlistCount);
    }

    [Fact]
    public void Enroll_WhenCourseFull_PlacesEligibleLearnerOnWaitlist()
    {
        // Arrange: Fill course to capacity (2)
        _service.Enroll(CourseId, new Learner("L-001", "Alice"));
        _service.Enroll(CourseId, new Learner("L-002", "Bob"));

        // Act: Third learner enrolls
        var res3 = _service.Enroll(CourseId, new Learner("L-003", "Charlie"));
        var res4 = _service.Enroll(CourseId, new Learner("L-004", "Diana"));

        // Assert
        Assert.Equal(EnrollmentStatus.Waitlisted, res3.Status);
        Assert.Equal(1, res3.WaitlistPosition);

        Assert.Equal(EnrollmentStatus.Waitlisted, res4.Status);
        Assert.Equal(2, res4.WaitlistPosition);

        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(2, status.EnrolledCount);
        Assert.Equal(2, status.WaitlistCount);
        Assert.Equal(new[] { "L-003", "L-004" }, status.WaitlistLearnerIds);
    }

    [Fact]
    public void Enroll_DuplicateRequestForEnrolledLearner_ReturnsAlreadyEnrolledWithoutDuplicating()
    {
        // Arrange
        var learner = new Learner("L-001", "Alice");
        _service.Enroll(CourseId, learner);

        // Act: Duplicate enrollment attempt
        var duplicateResult = _service.Enroll(CourseId, learner);

        // Assert
        Assert.Equal(EnrollmentStatus.AlreadyEnrolled, duplicateResult.Status);
        Assert.False(duplicateResult.IsSuccess);

        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(1, status.EnrolledCount);
        Assert.Single(status.EnrolledLearnerIds);
    }

    [Fact]
    public void Enroll_DuplicateRequestForWaitlistedLearner_ReturnsAlreadyWaitlistedWithoutDuplicating()
    {
        // Arrange: Fill capacity
        _service.Enroll(CourseId, new Learner("L-001", "Alice"));
        _service.Enroll(CourseId, new Learner("L-002", "Bob"));

        var waitlistedLearner = new Learner("L-003", "Charlie");
        var firstResult = _service.Enroll(CourseId, waitlistedLearner);
        Assert.Equal(EnrollmentStatus.Waitlisted, firstResult.Status);
        Assert.Equal(1, firstResult.WaitlistPosition);

        // Act: Duplicate waitlist enrollment attempt
        var duplicateResult = _service.Enroll(CourseId, waitlistedLearner);

        // Assert
        Assert.Equal(EnrollmentStatus.AlreadyWaitlisted, duplicateResult.Status);
        Assert.Equal(1, duplicateResult.WaitlistPosition);

        var status = _service.GetStatus(CourseId)!;
        Assert.Equal(1, status.WaitlistCount);
        Assert.Single(status.WaitlistLearnerIds);
    }

    [Fact]
    public void Enroll_NonExistentCourse_ReturnsCourseNotFound()
    {
        var result = _service.Enroll("NON-EXISTENT", new Learner("L-001", "Alice"));
        Assert.Equal(EnrollmentStatus.CourseNotFound, result.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Enroll_InvalidCourseId_ReturnsInvalidRequest(string? invalidCourseId)
    {
        var result = _service.Enroll(invalidCourseId!, new Learner("L-001", "Alice"));
        Assert.Equal(EnrollmentStatus.InvalidRequest, result.Status);
    }
}

