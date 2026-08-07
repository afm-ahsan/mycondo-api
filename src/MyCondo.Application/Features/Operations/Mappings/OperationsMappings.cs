using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;
using MyCondo.Domain.Features.Operations.GeneratorSessions;

namespace MyCondo.Application.Features.Operations.Mappings;

internal static class OperationsMappings
{
    public static GeneratorDto ToDto(this Generator generator) => new(
        generator.Id.Value, generator.BuildingId.Value, generator.Name, generator.Model, generator.CapacityKva,
        generator.Location, generator.CurrentHourMeterReading, generator.IsActive);

    public static GeneratorSessionDto ToDto(this GeneratorSession session) => new(
        session.Id.Value, session.GeneratorId.Value, session.StartAtUtc, session.StopAtUtc, session.OperatorId,
        session.OpeningFuelLevel, session.ClosingFuelLevel, session.OutageReason, session.RuntimeMinutes,
        session.Status.ToString());

    public static GeneratorFuelReceiptDto ToDto(this GeneratorFuelReceipt receipt) => new(
        receipt.Id.Value, receipt.GeneratorId.Value, receipt.ReceivedAtUtc, receipt.Quantity, receipt.Cost,
        receipt.Supplier, receipt.Remarks);

    public static GeneratorMaintenanceScheduleDto ToDto(this GeneratorMaintenanceSchedule schedule) => new(
        schedule.Id.Value, schedule.GeneratorId.Value, schedule.NextDueDate, schedule.NextDueHourMeterReading,
        schedule.IsActive);

    public static GeneratorServiceRecordDto ToDto(this GeneratorServiceRecord record) => new(
        record.Id.Value, record.GeneratorId.Value, record.PerformedAtUtc, record.Description, record.Cost,
        record.PerformedBy);

    public static GeneratorBreakdownRecordDto ToDto(this GeneratorBreakdownRecord record) => new(
        record.Id.Value, record.GeneratorId.Value, record.ReportedAtUtc, record.Description, record.DowntimeStartUtc,
        record.DowntimeEndUtc, record.Resolution, record.Cost);

    public static GasCylinderSupplierDto ToDto(this GasCylinderSupplier supplier) => new(
        supplier.Id.Value, supplier.Name, supplier.ContactPhone, supplier.ContactEmail, supplier.Address, supplier.IsActive);

    public static CylinderPurchaseDto ToDto(this CylinderPurchase purchase) => new(
        purchase.Id.Value, purchase.SupplierId.Value, purchase.InvoiceNumber, purchase.PurchaseDate, purchase.CylinderType,
        purchase.Quantity, purchase.CylinderWeightKg, purchase.RatePerCylinder, purchase.DeliveryOrOtherCost,
        purchase.Remarks, purchase.PaymentStatus.ToString(), purchase.ApprovalStatus.ToString(), purchase.ApprovedBy,
        purchase.ApprovedAtUtc, purchase.RejectedReason, purchase.TotalKg, purchase.LineAmount, purchase.UnitPricePerKg,
        purchase.GrandTotal);

    public static CylinderStockMovementDto ToDto(this CylinderStockMovement movement) => new(
        movement.Id.Value, movement.CylinderType, movement.MovementType.ToString(), movement.Quantity, movement.OccurredAtUtc,
        movement.Reason, movement.RecordedBy, movement.CylinderPurchaseId?.Value);

    public static MonthlyCylinderReconciliationDto ToDto(this MonthlyCylinderReconciliation reconciliation) => new(
        reconciliation.Id.Value, reconciliation.CylinderType, reconciliation.PeriodMonth, reconciliation.OpeningStock,
        reconciliation.TotalReceived, reconciliation.TotalIssued, reconciliation.TotalEmptyReturned,
        reconciliation.ClosingStock, reconciliation.VarianceQuantity, reconciliation.Remarks, reconciliation.ReconciledBy,
        reconciliation.ReconciledAtUtc);
}
