using AwesomeAssertions;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Domain.UnitTests.Features.Residents.HouseholdMembers;

public class ResidentHouseholdMemberTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ResidentId = Guid.NewGuid();

    [Fact]
    public void Add_Starts_Active()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);

        member.IsActive.Should().BeTrue();
        member.FullName.Should().Be("Fatema Ahmed");
        member.RelationshipType.Should().Be(RelationshipType.Spouse);
    }

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);

        member.Deactivate();

        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Add_Throws_When_FullName_Empty()
    {
        Action act = () => ResidentHouseholdMember.Add(
            TenantId, ResidentId, "", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null, null, null,
            null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Throws_When_TenantId_Empty()
    {
        Action act = () => ResidentHouseholdMember.Add(
            Guid.Empty, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1),
            null, null, null, null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Throws_When_DateOfBirth_Is_In_The_Future()
    {
        Action act = () => ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female",
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(1), null, null, null, null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Throws_When_Child_Has_Neither_NationalId_Nor_BirthCertificate()
    {
        Action act = () => ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Rahim Ahmed", RelationshipType.Child, "Male", new DateOnly(2016, 3, 10), null,
            null, null, null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Allows_Child_With_BirthCertificate_And_No_NationalId()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Rahim Ahmed", RelationshipType.Child, "Male", new DateOnly(2016, 3, 10), null,
            "BC-98765", null, null, null, null, Now);

        member.BirthCertificateNumber.Should().Be("BC-98765");
    }

    [Fact]
    public void Add_Allows_Spouse_Without_NationalId()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);

        member.NationalIdNumber.Should().BeNull();
    }

    [Fact]
    public void Update_Changes_Fields()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);

        member.Update(
            "Fatema Ahmed Khan", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), "1234567890", null,
            "O+", "Islam", "Bangladeshi", "Doctor", Now.AddDays(1));

        member.FullName.Should().Be("Fatema Ahmed Khan");
        member.BloodGroup.Should().Be("O+");
        member.Occupation.Should().Be("Doctor");
    }

    [Fact]
    public void Update_Throws_When_Changing_To_Child_Without_Identity()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);

        Action act = () => member.Update(
            "Fatema Ahmed", RelationshipType.Child, "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null, Now.AddDays(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_Does_Not_Clear_NationalIdNumber_When_Blank_Value_Is_Submitted()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Rahim Ahmed", RelationshipType.Child, "Male", new DateOnly(2016, 3, 10),
            "1234567890", null, null, null, null, null, Now);

        member.Update(
            "Rahim Ahmed", RelationshipType.Child, "Male", new DateOnly(2016, 3, 10), null, null, null, null, null,
            null, Now.AddDays(1));

        member.NationalIdNumber.Should().Be(
            "1234567890", "an empty submission means 'not retyped', not 'clear it' — the field is masked on every read");
    }

    [Fact]
    public void Update_Allows_Child_To_Keep_Existing_Identity_Without_Retyping_It()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Rahim Ahmed", RelationshipType.Child, "Male", new DateOnly(2016, 3, 10),
            "1234567890", null, null, null, null, null, Now);

        Action act = () => member.Update(
            "Rahim Ahmed", RelationshipType.Child, "Male", new DateOnly(2016, 3, 10), null, null, null, null, null,
            null, Now.AddDays(1));

        act.Should().NotThrow("the existing National ID is preserved, so the Child-identity guard is still satisfied");
    }

    [Fact]
    public void SetPrimaryPhoto_Sets_AttachmentId()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);
        Guid attachmentId = Guid.NewGuid();

        member.SetPrimaryPhoto(attachmentId);

        member.PrimaryPhotoAttachmentId.Should().Be(attachmentId);
    }

    [Fact]
    public void SetPrimaryPhoto_Replaces_Existing_AttachmentId()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);
        member.SetPrimaryPhoto(Guid.NewGuid());
        Guid replacementAttachmentId = Guid.NewGuid();

        member.SetPrimaryPhoto(replacementAttachmentId);

        member.PrimaryPhotoAttachmentId.Should().Be(replacementAttachmentId);
    }

    [Fact]
    public void SetPrimaryPhoto_Null_Clears_AttachmentId()
    {
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            TenantId, ResidentId, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1), null,
            null, null, null, null, null, Now);
        member.SetPrimaryPhoto(Guid.NewGuid());

        member.SetPrimaryPhoto(null);

        member.PrimaryPhotoAttachmentId.Should().BeNull();
    }
}
