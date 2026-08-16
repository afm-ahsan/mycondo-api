using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Domain.UnitTests.Features.Residents;

public class ResidentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();

    [Fact]
    public void Register_Trims_Fields()
    {
        Resident resident = Resident.Register(
            TenantId, FlatId, "  Jane Doe  ", " 01700000000 ", " jane@example.com ", ResidentType.Owner, Now);

        resident.FullName.Should().Be("Jane Doe");
        resident.Phone.Should().Be("+8801700000000");
        resident.Email.Should().Be("jane@example.com");
        resident.ResidentType.Should().Be(ResidentType.Owner);
        resident.Version.Should().Be(1);
    }

    [Fact]
    public void Register_Allows_Null_Phone_And_Email()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Occupant, Now);

        resident.Phone.Should().BeNull();
        resident.Email.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Throws_When_FullName_Is_Blank(string fullName)
    {
        Action act = () => Resident.Register(TenantId, FlatId, fullName, null, null, ResidentType.Owner, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateContactDetails_Increments_Version()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);

        resident.UpdateContactDetails("01711111111", "jane2@example.com");

        resident.Phone.Should().Be("+8801711111111");
        resident.Email.Should().Be("jane2@example.com");
        resident.Version.Should().Be(2);
    }

    [Fact]
    public void Deactivate_Sets_DeletedAtUtc()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);

        resident.Deactivate(Now.AddDays(1), Guid.NewGuid());

        resident.DeletedAtUtc.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void New_Resident_Has_No_UserId_Until_Explicitly_Linked()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);

        resident.UserId.Should().BeNull();
    }

    [Fact]
    public void LinkToUser_Sets_UserId_And_Increments_Version()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);
        Guid userId = Guid.NewGuid();

        resident.LinkToUser(userId);

        resident.UserId.Should().Be(userId);
        resident.Version.Should().Be(2);
    }

    [Fact]
    public void LinkToUser_Is_A_NoOp_When_Already_Linked_To_The_Same_User()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);
        Guid userId = Guid.NewGuid();
        resident.LinkToUser(userId);

        resident.LinkToUser(userId);

        resident.Version.Should().Be(2, "re-linking to the same User must not bump the version");
    }

    [Fact]
    public void LinkToUser_Throws_When_UserId_Is_Empty()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);

        Action act = () => resident.LinkToUser(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateOwnerDetails_Sets_Extended_Profile_Fields_And_Increments_Version()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);
        DateOnly dob = new(1990, 1, 1);

        resident.UpdateOwnerDetails(
            "01799999999", "1234567890123", "P1234567", dob, "Female", "123 Present St", "456 Permanent Rd",
            "John Doe Sr.", "Mary Doe", "Married", "Engineer", "Acme Corp", "789 Office Ave", "Emergency Contact",
            "01788888888", "O+", "Islam", "Bangladeshi", Now.AddDays(1));

        resident.AlternatePhone.Should().Be("+8801799999999");
        resident.NationalIdNumber.Should().Be("1234567890123");
        resident.PassportNumber.Should().Be("P1234567");
        resident.DateOfBirth.Should().Be(dob);
        resident.Gender.Should().Be("Female");
        resident.PresentAddress.Should().Be("123 Present St");
        resident.PermanentAddress.Should().Be("456 Permanent Rd");
        resident.FatherName.Should().Be("John Doe Sr.");
        resident.MotherName.Should().Be("Mary Doe");
        resident.MaritalStatus.Should().Be("Married");
        resident.Profession.Should().Be("Engineer");
        resident.Employer.Should().Be("Acme Corp");
        resident.OfficeAddress.Should().Be("789 Office Ave");
        resident.EmergencyContactName.Should().Be("Emergency Contact");
        resident.EmergencyContactPhone.Should().Be("+8801788888888");
        resident.BloodGroup.Should().Be("O+");
        resident.Religion.Should().Be("Islam");
        resident.Nationality.Should().Be("Bangladeshi");
        resident.Version.Should().Be(2);
    }

    [Fact]
    public void UpdateOwnerDetails_Does_Not_Clear_NationalIdNumber_When_Blank_Value_Is_Submitted()
    {
        Resident resident = Resident.Register(TenantId, FlatId, "Jane Doe", null, null, ResidentType.Owner, Now);
        resident.UpdateOwnerDetails(
            null, "1234567890123", "P1234567", null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, Now);

        resident.UpdateOwnerDetails(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, Now.AddDays(1));

        resident.NationalIdNumber.Should().Be(
            "1234567890123", "an empty submission means 'not retyped', not 'clear it' — matching OccupancyRegistration's masking convention");
        resident.PassportNumber.Should().Be("P1234567");
    }
}
