using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.UnitTests.Common;

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "1";
    public string? Username { get; set; } = "test_manager";
    public string? Role { get; set; } = "WarehouseManager";
    public bool IsAuthenticated => true;
}
