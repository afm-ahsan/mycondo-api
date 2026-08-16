using AwesomeAssertions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.UnitTests.Features.Leasing.HouseholdMembers;

public class HouseholdMemberTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly OccupancyRegistrationId RegistrationId = OccupancyRegistrationId.New();

    [Fact]
    public void Add_Starts_Active()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "John Doe", "Spouse", new DateOnly(1992, 5, 1), "01711111111", null, "Male",
            null, null, null, null, null, Now);

        member.IsActive.Should().BeTrue();
        member.FullName.Should().Be("John Doe");
        member.RelationshipToPrimary.Should().Be("Spouse");
    }

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null,
            Now);

        member.Deactivate();

        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Add_Throws_When_FullName_Empty()
    {
        Action act = () => HouseholdMember.Add(
            TenantId, RegistrationId, "", "Spouse", null, null, null, "Male", null, null, null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Throws_When_TenantId_Empty()
    {
        Action act = () => HouseholdMember.Add(
            Guid.Empty, RegistrationId, "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Throws_When_Child_Has_Neither_NationalId_Nor_BirthCertificate()
    {
        Action act = () => HouseholdMember.Add(
            TenantId, RegistrationId, "Baby Doe", "Child", new DateOnly(2020, 1, 1), null, null, "Female", null,
            null, null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Allows_Child_With_BirthCertificate_And_No_NationalId()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "Baby Doe", "Child", new DateOnly(2020, 1, 1), null, null, "Female",
            "BC-12345", null, null, null, null, Now);

        member.BirthCertificateNumber.Should().Be("BC-12345");
    }

    [Fact]
    public void Update_Changes_Fields()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null,
            Now);

        member.Update(
            "Jane Doe", "Spouse", new DateOnly(1990, 1, 1), "01711111111", "1234567890", "Female", null, "O+",
            "Islam", "Bangladeshi", "Engineer", Now);

        member.FullName.Should().Be("Jane Doe");
        member.Gender.Should().Be("Female");
        member.BloodGroup.Should().Be("O+");
        member.Occupation.Should().Be("Engineer");
    }

    [Fact]
    public void Update_Does_Not_Clear_NationalIdNumber_When_Blank_Value_Is_Submitted()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "Baby Doe", "Child", new DateOnly(2020, 1, 1), null, "1234567890", "Female",
            null, null, null, null, null, Now);

        member.Update(
            "Baby Doe", "Child", new DateOnly(2020, 1, 1), null, null, "Female", null, null, null, null, null,
            Now.AddDays(1));

        member.NationalIdNumber.Should().Be(
            "1234567890", "an empty submission means 'not retyped', not 'clear it' — the field is masked on every read");
    }

    [Fact]
    public void SetPrimaryPhoto_Sets_AttachmentId()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null,
            Now);
        Guid attachmentId = Guid.NewGuid();

        member.SetPrimaryPhoto(attachmentId);

        member.PrimaryPhotoAttachmentId.Should().Be(attachmentId);
    }

    [Fact]
    public void SetPrimaryPhoto_Replaces_Existing_AttachmentId()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null,
            Now);
        member.SetPrimaryPhoto(Guid.NewGuid());
        Guid replacementAttachmentId = Guid.NewGuid();

        member.SetPrimaryPhoto(replacementAttachmentId);

        member.PrimaryPhotoAttachmentId.Should().Be(replacementAttachmentId);
    }

    [Fact]
    public void SetPrimaryPhoto_Null_Clears_AttachmentId()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null,
            Now);
        member.SetPrimaryPhoto(Guid.NewGuid());

        member.SetPrimaryPhoto(null);

        member.PrimaryPhotoAttachmentId.Should().BeNull();
    }
}
