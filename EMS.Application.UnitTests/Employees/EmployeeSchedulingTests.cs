using EMS.Application.DTOs.Employee;
using EMS.Application.Services.Authorization;
using EMS.Application.Services.Employees;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Moq;
using NUnit.Framework;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Employees;

public class EmployeeBusinessDateHelperTests
{
    [Test]
    public void EnsureNotFutureForImmediateAction_Throws_ForTomorrow()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new EMS.Application.Options.EmployeeSchedulingOptions
        {
            BusinessTimeZoneId = "Australia/Sydney",
        });
        var sut = new EmployeeBusinessDateHelper(options);
        var tomorrow = DateTime.UtcNow.AddDays(2);

        Assert.Throws<BusinessRuleException>(() =>
            sut.EnsureNotFutureForImmediateAction(tomorrow, "termination"));
    }
}
