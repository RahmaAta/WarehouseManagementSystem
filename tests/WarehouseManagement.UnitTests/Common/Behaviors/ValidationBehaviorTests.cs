using FluentValidation;
using FluentValidation.Results;
using MediatR;
using WarehouseManagement.Application.Common.Behaviors;
using ValidationException = WarehouseManagement.Application.Common.Exceptions.ValidationException;

namespace WarehouseManagement.UnitTests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public record TestCommand(string Name, int Quantity) : IRequest<string>;

    private class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        }
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldInvokeNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("Widget", 10);
        bool nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldInvokeNextAndReturnResponse()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("Valid Name", 5);
        bool nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("Executed");
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Executed", result);
    }

    [Fact]
    public async Task Handle_WithValidationFailures_ShouldThrowValidationExceptionAndNotCallNext()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("", 0); // Both invalid
        bool nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("Executed");
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(command, next, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey(nameof(TestCommand.Name)));
        Assert.True(ex.Errors.ContainsKey(nameof(TestCommand.Quantity)));
        Assert.Contains("Name is required.", ex.Errors[nameof(TestCommand.Name)]);
        Assert.Contains("Quantity must be greater than zero.", ex.Errors[nameof(TestCommand.Quantity)]);
    }
}
