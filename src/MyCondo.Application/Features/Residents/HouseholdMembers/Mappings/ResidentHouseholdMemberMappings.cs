using MyCondo.Application.Common;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Mappings;

public static class ResidentHouseholdMemberMappings
{
    public static ResidentHouseholdMemberDto ToDto(this ResidentHouseholdMember member) => new(
        member.Id.Value, member.ResidentId, member.FullName, member.RelationshipType.ToString(), member.Gender,
        member.DateOfBirth, IdentityMasking.Mask(member.NationalIdNumber),
        IdentityMasking.Mask(member.BirthCertificateNumber), member.BloodGroup, member.Religion, member.Nationality,
        member.Occupation, member.IsActive);
}
