using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using Moq;

using Nullinside.Api.Model.Ddl;
using Nullinside.Api.TwitchBot.Controllers;
using Nullinside.Api.TwitchBot.Model;

namespace Nullinside.Api.TwitchBot.Tests;

[TestFixture]
public class BotControllerTests : UnitTestBase {
  [SetUp]
  public override void Setup() {
    base.Setup();
    _configurationMock = new Mock<IConfiguration>();
    _controller = new BotController(_db, _configurationMock.Object);
  }

  private BotController _controller;
  private Mock<IConfiguration> _configurationMock;

  [Test]
  public async Task GetAllChatLogs_ReturnsPaginatedResults() {
    // Arrange
    for (int i = 1; i <= 15; i++) {
      await _db.TwitchUserChatLogs.AddAsync(new TwitchUserChatLogs {
        Id = i,
        TwitchId = "user1",
        Message = $"Message {i}",
        Timestamp = DateTime.UtcNow.AddMinutes(i)
      });
    }

    await _db.SaveChangesAsync();

    // Act
    ObjectResult result = await _controller.GetAllChatLogs(2, 5);

    // Assert
    Assert.IsInstanceOf<OkObjectResult>(result);
    var okResult = (OkObjectResult)result;
    Assert.IsInstanceOf<PagedResponse<TwitchChatLogResponse>>(okResult.Value);
    var response = (PagedResponse<TwitchChatLogResponse>)okResult.Value!;

    Assert.That(response.TotalCount!, Is.EqualTo(15));
    Assert.That(response.Page, Is.EqualTo(2));
    Assert.That(response.PageSize, Is.EqualTo(5));
    Assert.That(response.Data.Count(), Is.EqualTo(5));

    // Check ordering (descending by timestamp)
    List<TwitchChatLogResponse> logs = response.Data.ToList();
    Assert.That(logs[0].Message, Is.EqualTo("Message 10")); // 15, 14, 13, 12, 11 (page 1) | 10, 9, 8, 7, 6 (page 2)
    Assert.That(logs[4].Message, Is.EqualTo("Message 6"));
  }
}