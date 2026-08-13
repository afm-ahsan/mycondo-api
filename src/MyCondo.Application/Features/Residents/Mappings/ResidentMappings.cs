using MyCondo.Application.Common;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Mappings;

public static class ResidentMappings
{
    public static ResidentDto ToDto(this Resident resident) => new(
        resident.Id.Value, resident.FlatId.Value, resident.FullName, resident.Phone, resident.Email,
        resident.ResidentType.ToString(), resident.AlternatePhone, IdentityMasking.Mask(resident.NationalIdNumber),
        IdentityMasking.Mask(resident.PassportNumber), resident.DateOfBirth, resident.Gender,
        resident.PresentAddress, resident.PermanentAddress, resident.FatherName, resident.MotherName,
        resident.MaritalStatus, resident.Profession, resident.Employer, resident.OfficeAddress,
        resident.EmergencyContactName, resident.EmergencyContactPhone);
}
