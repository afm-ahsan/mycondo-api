using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;

public class UpdateFlatOwnerProfileCommandValidatorTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);

    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();

    private UpdateFlatOwnerProfileCommandValidator CreateValidator() => new(_residents);

    private static UpdateFlatOwnerProfileCommand BuildCommand(Guid residentId, string? nationalIdNumber = "9876543210") => new(
        residentId, "Updated Name", "01700000000", "updated@example.com", "01711111111", nationalIdNumber, "P7654321",
        new DateOnly(1985, 5, 5), "Male", "New Present Addr", "New Permanent Addr", "New Father", "New Mother",
        "Single", "Doctor", "New Employer", "New Office Addr", "New Emergency Contact", "01799999999", "A+",
        "Christianity", "Bangladeshi");

    private static Resident CreateResident(Guid tenantId, bool withNationalId)
    {
        Flat flat = Flat.Create(tenantId, BuildingId.New(), "A-101", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(tenantId, flat.Id, "Original Name", null, null, ResidentType.Owner, NowUtc);
        if (withNationalId)
        {
            resident.UpdateOwnerDetails(
                null, "1234567890", null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, NowUtc);
        }
        return resident;
    }

    [Fact]
    public async Task Valid_Command_Passes()
    {
        Resident resident = CreateResident(Guid.NewGuid(), withNationalId: false);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        ValidationResult result = await CreateValidator().ValidateAsync(BuildCommand(resident.Id.Value));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Blank_NationalIdNumber_Fails_When_Resident_Has_None_On_File()
    {
        Resident resident = CreateResident(Guid.NewGuid(), withNationalId: false);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        ValidationResult result = await CreateValidator().ValidateAsync(BuildCommand(resident.Id.Value, nationalIdNumber: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFlatOwnerProfileCommand.NationalIdNumber));
    }

    [Fact]
    public async Task Blank_NationalIdNumber_Passes_When_Resident_Already_Has_One_On_File()
    {
        Resident resident = CreateResident(Guid.NewGuid(), withNationalId: true);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        ValidationResult result = await CreateValidator().ValidateAsync(BuildCommand(resident.Id.Value, nationalIdNumber: null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Blank_NationalIdNumber_Fails_When_Resident_Not_Found()
    {
        _residents.GetByIdAsync(Arg.Any<ResidentId>(), Arg.Any<CancellationToken>()).Returns((Resident?)null);

        ValidationResult result = await CreateValidator().ValidateAsync(BuildCommand(Guid.NewGuid(), nationalIdNumber: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFlatOwnerProfileCommand.NationalIdNumber));
    }
}
