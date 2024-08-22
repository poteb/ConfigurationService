using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace pote.Config.UnitTests;

[TestFixture]
public class DateTimeParserTests
{
    [Test]
    public async Task TestWinterDateTimeUtc()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"MyDateTime\":\"2022-12-07T09:25:13.585Z\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var obj = JsonConvert.DeserializeObject<DateTimeContainerClass>(response);
        Assert.AreEqual(new DateTime(2022, 12, 7, 9, 25, 13, 585, DateTimeKind.Utc), obj?.MyDateTime);
        var timeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        var local = TimeZoneInfo.ConvertTimeFromUtc(obj.MyDateTime, timeZoneId);
        Assert.AreEqual(new DateTime(2022, 12, 7, 10, 25, 13, 585, DateTimeKind.Local), local);
    }
    
    [Test]
    public async Task TestSummerDateTimeUtc()
    {
        var dataProvider = new TestDataProvider();
        var parser = new Parser.Parser(dataProvider);
        var response = await parser.Parse("{\"MyDateTime\":\"2023-08-22T11:56:11.151Z\"}", "unittest", "test", _ => { }, CancellationToken.None, "", false);
        var obj = JsonConvert.DeserializeObject<DateTimeContainerClass>(response);
        Assert.AreEqual(new DateTime(2023, 8, 22, 11, 56, 11, 151, DateTimeKind.Utc), obj?.MyDateTime);
        var timeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        var local = TimeZoneInfo.ConvertTimeFromUtc(obj.MyDateTime, timeZoneId);
        Assert.AreEqual(new DateTime(2023, 8, 22, 13, 56, 11, 151, DateTimeKind.Local), local);
    }
}

public class DateTimeContainerClass
{
    public DateTime MyDateTime { get; set; }
}