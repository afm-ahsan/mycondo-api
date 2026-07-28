---
name: feature-folder-convention
description: The mandatory RajMango-style feature-first folder layout for MyCondo.Application and MyCondo.Domain. Use whenever adding a new command, query, or entity, or when restructuring existing code into Features/.
---

# Feature Folder Convention (Backend)

## The rule

```
MyCondo.Application/Features/<Feature>/
├── Commands/<UseCaseName>/
│   ├── <UseCaseName>Command.cs
│   ├── <UseCaseName>CommandHandler.cs
│   ├── <UseCaseName>CommandValidator.cs
│   └── <UseCaseName>Result.cs        # only if the result isn't a trivial primitive/DTO
├── Queries/<UseCaseName>/
│   ├── <UseCaseName>Query.cs
│   ├── <UseCaseName>QueryHandler.cs
│   └── <ResultName>Dto.cs
├── DTOs/          # only for DTOs shared by 2+ use cases within this feature
├── Validators/
├── Services/
├── Specifications/
├── Mappings/
├── Models/
└── Events/
```

Only create the subfolders a feature actually needs — don't scaffold empty `Specifications/`,
`Events/`, etc. "just in case."

## Never do this

- Never a shared root-level `Commands/` or `Queries/` folder outside a feature.
- Never a DTO in a feature-wide `DTOs/` folder if only one use case uses it — put it next to that use
  case instead. Promote to `DTOs/` only when a second use case starts needing it.
- Never mix two distinct features in one folder because they happen to be related. Example: MyCondo's
  current `Users/` folder mixes authentication mechanics (Login/Register/RefreshToken/Logout) with
  what should eventually be user/account management. When splitting it, `Login` etc. go to
  `Features/Auth/`, and `Features/Users/` is reserved for actual profile/account CRUD.

## History (fixed 2026-07-28, Wave 0.5)

`src/MyCondo.Application/Users/...` and the corresponding flat `src/MyCondo.Domain/{Users,Roles,...}`
folders used to be missing the `Features/` parent entirely. This was restructured in Wave 0.5:
Application's `Users` split into `Features/Auth` (Login/Register/RefreshToken/Logout/ChangePassword/
GetMyProfile), and Domain's flat identity folders moved under `Features/Identity/`. Two architecture
tests in `MyCondo.ArchitectureTests` (`FeatureFolderConventionTests.cs`) now enforce this going
forward — a new Command/Query placed outside `Features/<Feature>/...` fails the build's test suite.
`Features/Users/` is reserved for actual profile/account CRUD once that use case exists.

## Domain layer

Same idea, one layer down: `MyCondo.Domain/Features/<Feature>/<Entity>.cs` etc. Lower urgency than
Application (existing Domain folders don't currently mix features), but new domain code should still
go under `Features/` from day one to avoid compounding the debt.
