using AwesomeAssertions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations.Exceptions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Domain.UnitTests.Features.Leasing.OccupancyRegistrations;

public class OccupancyRegistrationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly ResidentId ResidentId = ResidentId.New();

    private static OccupancyRegistration Register() =>
        OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId, ResidentType.Occupant, "Jane Doe", "01700000000", "jane@example.com",
            "1234567890", new DateOnly(1990, 1, 1), "Female", "O+", "Islam", "Bangladeshi", "Robert Doe",
            "Mary Doe", "Married", "Engineer", "123 Example Road, Dhaka", "John Doe", "01711111111", null, Now);

    [Fact]
    public void Register_Starts_In_Draft()
    {
        OccupancyRegistration registration = Register();

        registration.Status.Should().Be(OccupancyRegistrationStatus.Draft);
    }

    [Fact]
    public void UpdateDraft_Throws_When_Not_Draft_Or_CorrectionsRequested()
    {
        OccupancyRegistration registration = Register();
        registration.Submit(Guid.NewGuid(), Now);

        Action act = () => registration.UpdateDraft(
            "New Name", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null);

        act.Should().Throw<OccupancyRegistrationInvalidTransitionException>();
    }

    [Fact]
    public void UpdateDraft_Preserves_NationalIdNumber_When_Not_Provided()
    {
        OccupancyRegistration registration = Register();

        registration.UpdateDraft(
            "Jane Doe", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null);

        registration.PrimaryNationalIdNumber.Should().Be("1234567890");
    }

    [Fact]
    public void UpdateDraft_Replaces_NationalIdNumber_When_Provided()
    {
        OccupancyRegistration registration = Register();

        registration.UpdateDraft(
            "Jane Doe", null, null, "9876543210", null, null, null, null, null, null, null, null, null, null, null,
            null, null);

        registration.PrimaryNationalIdNumber.Should().Be("9876543210");
    }

    [Fact]
    public void UpdateDraft_Replaces_FatherMotherName_And_MaritalStatus()
    {
        OccupancyRegistration registration = Register();

        registration.UpdateDraft(
            "Jane Doe", null, null, null, null, null, null, null, null, "Robert Doe", "Mary Doe", "Married", null,
            null, null, null, null);

        registration.PrimaryFatherName.Should().Be("Robert Doe");
        registration.PrimaryMotherName.Should().Be("Mary Doe");
        registration.PrimaryMaritalStatus.Should().Be("Married");
    }

    [Fact]
    public void Submit_Moves_Draft_To_Submitted()
    {
        OccupancyRegistration registration = Register();
        Guid submittedBy = Guid.NewGuid();

        registration.Submit(submittedBy, Now);

        registration.Status.Should().Be(OccupancyRegistrationStatus.Submitted);
        registration.SubmittedBy.Should().Be(submittedBy);
        registration.SubmittedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Submit_Throws_When_Already_Submitted()
    {
        OccupancyRegistration registration = Register();
        registration.Submit(Guid.NewGuid(), Now);

        Action act = () => registration.Submit(Guid.NewGuid(), Now);

        act.Should().Throw<OccupancyRegistrationInvalidTransitionException>();
    }

    [Fact]
    public void ApproveByOwner_Throws_When_Not_Submitted()
    {
        OccupancyRegistration registration = Register();

        Action act = () => registration.ApproveByOwner(Guid.NewGuid(), Now);

        act.Should().Throw<OccupancyRegistrationInvalidTransitionException>();
    }

    [Fact]
    public void Full_Happy_Path_Reaches_Active_Then_MovedOut()
    {
        OccupancyRegistration registration = Register();
        Guid owner = Guid.NewGuid();
        Guid management = Guid.NewGuid();

        registration.Submit(Guid.NewGuid(), Now);
        registration.ApproveByOwner(owner, Now);
        registration.Status.Should().Be(OccupancyRegistrationStatus.OwnerApproved);

        registration.VerifyByManagement(management, Now);
        registration.Status.Should().Be(OccupancyRegistrationStatus.ManagementVerified);

        registration.Activate(Now);
        registration.Status.Should().Be(OccupancyRegistrationStatus.Active);
        registration.ActivatedAtUtc.Should().Be(Now);

        registration.MoveOut("Relocated to another city", Now);
        registration.Status.Should().Be(OccupancyRegistrationStatus.MovedOut);
        registration.MoveOutReason.Should().Be("Relocated to another city");
    }

    [Fact]
    public void RequestCorrections_Works_From_Submitted_And_OwnerApproved()
    {
        OccupancyRegistration atSubmitted = Register();
        atSubmitted.Submit(Guid.NewGuid(), Now);
        atSubmitted.RequestCorrections("Missing NID copy", Now);
        atSubmitted.Status.Should().Be(OccupancyRegistrationStatus.CorrectionsRequested);
        atSubmitted.CorrectionsRequestedReason.Should().Be("Missing NID copy");

        OccupancyRegistration atOwnerApproved = Register();
        atOwnerApproved.Submit(Guid.NewGuid(), Now);
        atOwnerApproved.ApproveByOwner(Guid.NewGuid(), Now);
        atOwnerApproved.RequestCorrections("Address does not match lease", Now);
        atOwnerApproved.Status.Should().Be(OccupancyRegistrationStatus.CorrectionsRequested);
    }

    [Fact]
    public void CorrectionsRequested_Can_Be_Resubmitted()
    {
        OccupancyRegistration registration = Register();
        registration.Submit(Guid.NewGuid(), Now);
        registration.RequestCorrections("Missing NID copy", Now);

        registration.UpdateDraft(
            "Jane A. Doe", null, null, null, new DateOnly(1990, 1, 1), "Female", null, null, null, null, null, null,
            null, null, null, null, null);
        registration.Submit(Guid.NewGuid(), Now);

        registration.Status.Should().Be(OccupancyRegistrationStatus.Submitted);
        registration.PrimaryFullName.Should().Be("Jane A. Doe");
    }

    [Fact]
    public void Reject_Works_From_Submitted_And_OwnerApproved()
    {
        OccupancyRegistration atSubmitted = Register();
        atSubmitted.Submit(Guid.NewGuid(), Now);
        atSubmitted.Reject("Duplicate registration for this flat", Now);
        atSubmitted.Status.Should().Be(OccupancyRegistrationStatus.Rejected);

        OccupancyRegistration atOwnerApproved = Register();
        atOwnerApproved.Submit(Guid.NewGuid(), Now);
        atOwnerApproved.ApproveByOwner(Guid.NewGuid(), Now);
        atOwnerApproved.Reject("Failed background verification", Now);
        atOwnerApproved.Status.Should().Be(OccupancyRegistrationStatus.Rejected);
    }

    [Fact]
    public void Activate_Throws_When_Not_ManagementVerified()
    {
        OccupancyRegistration registration = Register();
        registration.Submit(Guid.NewGuid(), Now);
        registration.ApproveByOwner(Guid.NewGuid(), Now);

        Action act = () => registration.Activate(Now);

        act.Should().Throw<OccupancyRegistrationInvalidTransitionException>();
    }

    [Fact]
    public void MoveOut_Throws_When_Not_Active()
    {
        OccupancyRegistration registration = Register();

        Action act = () => registration.MoveOut("test", Now);

        act.Should().Throw<OccupancyRegistrationInvalidTransitionException>();
    }

    [Fact]
    public void Register_Throws_When_TenantId_Empty()
    {
        Action act = () => OccupancyRegistration.Register(
            Guid.Empty, FlatId, ResidentId, ResidentType.Occupant, "Jane Doe", null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Submit_Throws_When_NationalIdNumber_Is_Missing()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId, ResidentType.Occupant, "Jane Doe", null, null, null,
            new DateOnly(1990, 1, 1), "Female", null, null, null, null, null, null, null, null, null, null, null,
            Now);

        Action act = () => registration.Submit(Guid.NewGuid(), Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_Throws_When_Gender_Is_Missing()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId, ResidentType.Occupant, "Jane Doe", null, null, "1234567890",
            new DateOnly(1990, 1, 1), null, null, null, null, null, null, null, null, null, null, null, null, Now);

        Action act = () => registration.Submit(Guid.NewGuid(), Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_Throws_When_DateOfBirth_Is_Missing()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId, ResidentType.Occupant, "Jane Doe", null, null, "1234567890", null,
            "Female", null, null, null, null, null, null, null, null, null, null, null, Now);

        Action act = () => registration.Submit(Guid.NewGuid(), Now);

        act.Should().Throw<InvalidOperationException>();
    }
}
