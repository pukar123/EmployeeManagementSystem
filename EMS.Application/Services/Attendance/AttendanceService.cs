using EMS.Application.DTOs.Attendance;
using EMS.Application.Mapping;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Pukar.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace EMS.Application.Services.Attendance;

public sealed class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceBreakRepository _breakRepository;
    private readonly IBaseRepository<Employee> _employeeRepository;
    private readonly IBaseRepository<AttendancePolicy> _policyRepository;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IAttendanceBreakRepository breakRepository,
        IBaseRepository<Employee> employeeRepository,
        IBaseRepository<AttendancePolicy> policyRepository)
    {
        _attendanceRepository = attendanceRepository;
        _breakRepository = breakRepository;
        _employeeRepository = employeeRepository;
        _policyRepository = policyRepository;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<AttendanceRecordResponseModel> CheckInAsync(CheckInRequestModel request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");
        var at = request.CheckInAtUtc ?? DateTime.UtcNow;

        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken);
        if (open is not null)
            throw new BusinessRuleException("Employee already has an active attendance session.");

        var entity = new AttendanceRecord
        {
            OrganizationId = employee.OrganizationId,
            EmployeeId = employee.Id,
            WorkDate = at.Date,
            CheckInAtUtc = at,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            Source = AttendanceSource.Web,
            Status = AttendanceStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await _attendanceRepository.AddAsync(entity, cancellationToken);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(entity);
    }

    public async Task<AttendanceRecordResponseModel> CheckOutAsync(CheckOutRequestModel request, CancellationToken cancellationToken = default)
    {
        var at = request.CheckOutAtUtc ?? DateTime.UtcNow;
        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("No active attendance session found.");

        var openBreak = await _breakRepository.GetOpenForAttendanceAsync(open.Id, cancellationToken);
        if (openBreak is not null)
            throw new BusinessRuleException("End the active break before check-out.");

        if (at < open.CheckInAtUtc)
            throw new BusinessRuleException("Check-out time cannot be earlier than check-in time.");

        open.CheckOutAtUtc = at;
        open.CheckOutLatitude = request.Latitude;
        open.CheckOutLongitude = request.Longitude;
        open.Status = AttendanceStatus.Completed;
        open.UpdatedAtUtc = DateTime.UtcNow;

        _attendanceRepository.Update(open);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        await LoadBreaksAsync(open, cancellationToken);
        return AttendanceMapper.ToResponse(open);
    }

    public async Task<AttendanceBreakResponseModel> StartBreakAsync(StartBreakRequestModel request, CancellationToken cancellationToken = default)
    {
        var at = request.StartAtUtc ?? DateTime.UtcNow;
        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("No active attendance session found.");

        var currentBreak = await _breakRepository.GetOpenForAttendanceAsync(open.Id, cancellationToken);
        if (currentBreak is not null)
            throw new BusinessRuleException("An active break already exists.");

        if (at < open.CheckInAtUtc)
            throw new BusinessRuleException("Break start cannot be earlier than check-in.");

        var entity = new AttendanceBreak
        {
            AttendanceRecordId = open.Id,
            StartAtUtc = at,
        };

        await _breakRepository.AddAsync(entity, cancellationToken);
        await _breakRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(entity);
    }

    public async Task<AttendanceBreakResponseModel> EndBreakAsync(EndBreakRequestModel request, CancellationToken cancellationToken = default)
    {
        var at = request.EndAtUtc ?? DateTime.UtcNow;
        var open = await _attendanceRepository.GetOpenForEmployeeAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("No active attendance session found.");

        var currentBreak = await _breakRepository.GetOpenForAttendanceAsync(open.Id, cancellationToken)
            ?? throw new BusinessRuleException("No active break found.");

        if (at < currentBreak.StartAtUtc)
            throw new BusinessRuleException("Break end cannot be earlier than break start.");

        currentBreak.EndAtUtc = at;
        currentBreak.DurationMinutes = Math.Max(0, (int)Math.Round((at - currentBreak.StartAtUtc).TotalMinutes));
        _breakRepository.Update(currentBreak);
        await _breakRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(currentBreak);
    }

    public async Task<AttendanceRecordResponseModel> CreateManualEntryAsync(ManualAttendanceEntryRequestModel request, CancellationToken cancellationToken = default)
    {
        if (request.CheckOutAtUtc <= request.CheckInAtUtc)
            throw new BusinessRuleException("Check-out must be later than check-in.");

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new BusinessRuleException("Employee was not found.");

        if (employee.OrganizationId != request.OrganizationId)
            throw new BusinessRuleException("Employee must belong to the same organization.");

        var entity = new AttendanceRecord
        {
            OrganizationId = request.OrganizationId,
            EmployeeId = request.EmployeeId,
            WorkDate = request.WorkDate.Date,
            CheckInAtUtc = request.CheckInAtUtc,
            CheckOutAtUtc = request.CheckOutAtUtc,
            Source = AttendanceSource.Manual,
            Status = AttendanceStatus.AutoApproved,
            ManualReason = StringHelper.NormalizeOptional(request.ManualReason),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await _attendanceRepository.AddAsync(entity, cancellationToken);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
        return AttendanceMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<AttendanceRecordResponseModel>> GetEmployeeAttendanceAsync(
        int employeeId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = _attendanceRepository.GetQueryable()
            .Include(x => x.Breaks)
            .Where(x => x.EmployeeId == employeeId);

        if (fromDate.HasValue)
            query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(x => x.WorkDate <= toDate.Value.Date);

        var rows = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CheckInAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(AttendanceMapper.ToResponse).ToList();
    }

    public async Task<AttendanceSummaryResponseModel> GetSummaryAsync(
        int employeeId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await _attendanceRepository.GetQueryable()
            .Include(x => x.Breaks)
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.WorkDate >= fromDate.Date &&
                x.WorkDate <= toDate.Date)
            .ToListAsync(cancellationToken);

        var mapped = rows.Select(AttendanceMapper.ToResponse).ToList();

        return new AttendanceSummaryResponseModel
        {
            EmployeeId = employeeId,
            FromDate = fromDate.Date,
            ToDate = toDate.Date,
            TotalRecords = mapped.Count,
            TotalBreakMinutes = mapped.Sum(x => x.BreakMinutes),
            TotalWorkedMinutes = mapped.Sum(x => x.WorkedMinutes),
        };
    }

    public async Task<IReadOnlyList<AttendanceDailySummaryResponseModel>> GetDailySummariesAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ValidateReportRange(request.FromDate, request.ToDate);

        var rows = await _attendanceRepository.GetRecordsForReportAsync(
            request.OrganizationId,
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            request.DepartmentId,
            cancellationToken);

        return rows
            .GroupBy(x => x.WorkDate.Date)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var mapped = group.Select(AttendanceMapper.ToResponse).ToList();
                return new AttendanceDailySummaryResponseModel
                {
                    WorkDate = group.Key,
                    OrganizationId = request.OrganizationId,
                    EmployeeId = request.EmployeeId,
                    DepartmentId = request.DepartmentId,
                    TotalRecords = mapped.Count,
                    PresentEmployees = group.Select(x => x.EmployeeId).Distinct().Count(),
                    TotalWorkedMinutes = mapped.Sum(x => x.WorkedMinutes),
                    TotalBreakMinutes = mapped.Sum(x => x.BreakMinutes),
                    AverageWorkedMinutes = mapped.Count == 0 ? 0 : decimal.Round((decimal)mapped.Sum(x => x.WorkedMinutes) / mapped.Count, 2),
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AttendancePeriodSummaryResponseModel>> GetPeriodSummariesAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ValidateReportRange(request.FromDate, request.ToDate);
        var mode = NormalizeGrouping(request.GroupBy);

        var rows = await _attendanceRepository.GetRecordsForReportAsync(
            request.OrganizationId,
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            request.DepartmentId,
            cancellationToken);

        return rows
            .GroupBy(x => GetPeriodStartDate(x.WorkDate.Date, mode))
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var mapped = group.Select(AttendanceMapper.ToResponse).ToList();
                var periodStart = group.Key;
                var periodEnd = mode == "month"
                    ? new DateTime(periodStart.Year, periodStart.Month, DateTime.DaysInMonth(periodStart.Year, periodStart.Month))
                    : periodStart.AddDays(6);

                return new AttendancePeriodSummaryResponseModel
                {
                    PeriodLabel = mode == "month"
                        ? periodStart.ToString("yyyy-MM", CultureInfo.InvariantCulture)
                        : $"{periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}",
                    PeriodStartDate = periodStart,
                    PeriodEndDate = periodEnd,
                    OrganizationId = request.OrganizationId,
                    EmployeeId = request.EmployeeId,
                    DepartmentId = request.DepartmentId,
                    TotalRecords = mapped.Count,
                    PresentEmployees = group.Select(x => x.EmployeeId).Distinct().Count(),
                    TotalWorkedMinutes = mapped.Sum(x => x.WorkedMinutes),
                    TotalBreakMinutes = mapped.Sum(x => x.BreakMinutes),
                    AverageWorkedMinutes = mapped.Count == 0 ? 0 : decimal.Round((decimal)mapped.Sum(x => x.WorkedMinutes) / mapped.Count, 2),
                };
            })
            .ToList();
    }

    public async Task<AttendancePunctualityResponseModel> GetPunctualityAnalyticsAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ValidateReportRange(request.FromDate, request.ToDate);

        var policy = await GetPolicyAsync(request.OrganizationId, cancellationToken);
        var rows = await _attendanceRepository.GetRecordsForReportAsync(
            request.OrganizationId,
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            request.DepartmentId,
            cancellationToken);
        var mapped = rows.Select(AttendanceMapper.ToResponse).ToList();

        const int graceMinutes = 15;
        var lateArrivals = mapped.Count(x => x.CheckInAtUtc > x.WorkDate.Date.AddHours(9).AddMinutes(graceMinutes));
        var earlyDepartures = mapped.Count(x => x.WorkedMinutes < policy.StandardDailyMinutes);

        return new AttendancePunctualityResponseModel
        {
            OrganizationId = request.OrganizationId,
            FromDate = request.FromDate.Date,
            ToDate = request.ToDate.Date,
            EmployeeId = request.EmployeeId,
            DepartmentId = request.DepartmentId,
            TotalRecords = mapped.Count,
            LateArrivals = lateArrivals,
            EarlyDepartures = earlyDepartures,
            LateArrivalRate = mapped.Count == 0 ? 0 : decimal.Round((decimal)lateArrivals * 100 / mapped.Count, 2),
            EarlyDepartureRate = mapped.Count == 0 ? 0 : decimal.Round((decimal)earlyDepartures * 100 / mapped.Count, 2),
        };
    }

    public async Task<AttendanceAbsenteeismResponseModel> GetAbsenteeismAnalyticsAsync(
        AttendanceReportFilterRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ValidateReportRange(request.FromDate, request.ToDate);
        var rows = await _attendanceRepository.GetRecordsForReportAsync(
            request.OrganizationId,
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            request.DepartmentId,
            cancellationToken);

        var workingDays = GetWeekdayCount(request.FromDate.Date, request.ToDate.Date);
        var headCount = await GetHeadCountAsync(request, cancellationToken);
        var expectedWorkDays = workingDays * headCount;
        var presentDays = rows.Select(x => (x.EmployeeId, x.WorkDate.Date)).Distinct().Count();
        var absentDays = Math.Max(0, expectedWorkDays - presentDays);

        return new AttendanceAbsenteeismResponseModel
        {
            OrganizationId = request.OrganizationId,
            FromDate = request.FromDate.Date,
            ToDate = request.ToDate.Date,
            EmployeeId = request.EmployeeId,
            DepartmentId = request.DepartmentId,
            ExpectedWorkDays = expectedWorkDays,
            PresentDays = presentDays,
            AbsentDays = absentDays,
            AbsenceRate = expectedWorkDays == 0 ? 0 : decimal.Round((decimal)absentDays * 100 / expectedWorkDays, 2),
        };
    }

    public async Task<AttendanceExportFileResponseModel> ExportReportAsync(
        AttendanceExportRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ValidateReportRange(request.FromDate, request.ToDate);
        var records = await _attendanceRepository.GetRecordsForReportAsync(
            request.OrganizationId,
            request.FromDate,
            request.ToDate,
            request.EmployeeId,
            request.DepartmentId,
            cancellationToken);
        var rows = records.Select(AttendanceMapper.ToResponse).ToList();
        var format = request.Format.Trim().ToLowerInvariant();

        return format switch
        {
            "csv" => BuildCsvExport(rows, request),
            "xlsx" => BuildExcelExport(rows, request),
            "pdf" => BuildPdfExport(rows, request),
            _ => throw new BusinessRuleException("Invalid export format. Allowed values are csv, xlsx, and pdf."),
        };
    }

    private async Task LoadBreaksAsync(AttendanceRecord record, CancellationToken cancellationToken)
    {
        record.Breaks = await _breakRepository.GetQueryable()
            .Where(x => x.AttendanceRecordId == record.Id)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync(cancellationToken);
    }

    private static void ValidateReportRange(DateTime fromDate, DateTime toDate)
    {
        if (toDate.Date < fromDate.Date)
            throw new BusinessRuleException("toDate cannot be earlier than fromDate.");
    }

    private static string NormalizeGrouping(string groupBy)
    {
        return groupBy.Trim().ToLowerInvariant() switch
        {
            "week" => "week",
            "month" => "month",
            _ => throw new BusinessRuleException("groupBy must be either 'week' or 'month'."),
        };
    }

    private static DateTime GetPeriodStartDate(DateTime date, string mode)
    {
        if (mode == "month")
            return new DateTime(date.Year, date.Month, 1);

        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset).Date;
    }

    private async Task<AttendancePolicy> GetPolicyAsync(int organizationId, CancellationToken cancellationToken)
    {
        var policy = await _policyRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, cancellationToken);
        if (policy is not null)
            return policy;

        return new AttendancePolicy
        {
            OrganizationId = organizationId,
            StandardDailyMinutes = 480,
            StandardBreakMinutes = 60,
        };
    }

    private async Task<int> GetHeadCountAsync(AttendanceReportFilterRequestModel request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId.HasValue)
            return 1;

        var query = _employeeRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId && !x.IsArchived);

        if (request.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == request.DepartmentId.Value);

        return await query.CountAsync(cancellationToken);
    }

    private static int GetWeekdayCount(DateTime fromDate, DateTime toDate)
    {
        var count = 0;
        for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            count++;
        }
        return count;
    }

    private static AttendanceExportFileResponseModel BuildCsvExport(
        IReadOnlyList<AttendanceRecordResponseModel> rows,
        AttendanceExportRequestModel request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WorkDate,EmployeeId,CheckInAtUtc,CheckOutAtUtc,WorkedMinutes,BreakMinutes,Status,Source");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                row.WorkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.EmployeeId,
                row.CheckInAtUtc.ToString("O", CultureInfo.InvariantCulture),
                row.CheckOutAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                row.WorkedMinutes,
                row.BreakMinutes,
                row.Status,
                row.Source));
        }

        return new AttendanceExportFileResponseModel
        {
            FileName = BuildFileName("csv", request),
            ContentType = "text/csv",
            FileBytes = Encoding.UTF8.GetBytes(sb.ToString()),
        };
    }

    private static AttendanceExportFileResponseModel BuildExcelExport(
        IReadOnlyList<AttendanceRecordResponseModel> rows,
        AttendanceExportRequestModel request)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Attendance");
        worksheet.Cell(1, 1).Value = "Work Date";
        worksheet.Cell(1, 2).Value = "Employee Id";
        worksheet.Cell(1, 3).Value = "Check In (UTC)";
        worksheet.Cell(1, 4).Value = "Check Out (UTC)";
        worksheet.Cell(1, 5).Value = "Worked Minutes";
        worksheet.Cell(1, 6).Value = "Break Minutes";
        worksheet.Cell(1, 7).Value = "Status";
        worksheet.Cell(1, 8).Value = "Source";

        var rowIndex = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = row.WorkDate.Date;
            worksheet.Cell(rowIndex, 2).Value = row.EmployeeId;
            worksheet.Cell(rowIndex, 3).Value = row.CheckInAtUtc;
            worksheet.Cell(rowIndex, 4).Value = row.CheckOutAtUtc;
            worksheet.Cell(rowIndex, 5).Value = row.WorkedMinutes;
            worksheet.Cell(rowIndex, 6).Value = row.BreakMinutes;
            worksheet.Cell(rowIndex, 7).Value = row.Status.ToString();
            worksheet.Cell(rowIndex, 8).Value = row.Source.ToString();
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new AttendanceExportFileResponseModel
        {
            FileName = BuildFileName("xlsx", request),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileBytes = stream.ToArray(),
        };
    }

    private static AttendanceExportFileResponseModel BuildPdfExport(
        IReadOnlyList<AttendanceRecordResponseModel> rows,
        AttendanceExportRequestModel request)
    {
        var bytes = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text("Attendance Report").Bold().FontSize(16);
                    col.Item().Text($"Range: {request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}");
                    col.Item().Text($"Rows: {rows.Count}");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Employee").Bold();
                            header.Cell().Text("Check In").Bold();
                            header.Cell().Text("Worked").Bold();
                            header.Cell().Text("Break").Bold();
                        });

                        foreach (var row in rows.Take(500))
                        {
                            table.Cell().Text(row.WorkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                            table.Cell().Text(row.EmployeeId.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Text(row.CheckInAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                            table.Cell().Text(row.WorkedMinutes.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Text(row.BreakMinutes.ToString(CultureInfo.InvariantCulture));
                        }
                    });
                });
            });
        }).GeneratePdf();

        return new AttendanceExportFileResponseModel
        {
            FileName = BuildFileName("pdf", request),
            ContentType = "application/pdf",
            FileBytes = bytes,
        };
    }

    private static string BuildFileName(string extension, AttendanceExportRequestModel request)
    {
        return $"attendance-report-{request.FromDate:yyyyMMdd}-{request.ToDate:yyyyMMdd}.{extension}";
    }
}
