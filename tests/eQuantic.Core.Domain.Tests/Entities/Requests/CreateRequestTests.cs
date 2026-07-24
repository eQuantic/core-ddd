using eQuantic.Core.Domain.Entities.Requests;
using FluentAssertions;

namespace eQuantic.Core.Domain.Tests.Entities.Requests;

public class CreateRequestTests
{
    [Test]
    public void CreateRequest_GetReferenceValue_returns_correct_value()
    {
        var referenceId = Guid.NewGuid();
        var request = new CreateRequest<ExampleRequest, Guid>(referenceId, new ExampleRequest());

        request.GetReferenceValue().Should().Be(referenceId);
    }
    
    [Test]
    public void CreateRequest_IsReferencedRequest_returns_true()
    {
        var referenceId = Guid.NewGuid();
        var request = new CreateRequest<ExampleRequest, Guid>(referenceId, new ExampleRequest());

        request.IsReferencedRequest().Should().BeTrue();
    }
    
    [Test]
    public void CreateRequest_GetReferenceType_returns_correct_type()
    {
        var referenceId = Guid.NewGuid();
        var request = new CreateRequest<ExampleRequest, Guid>(referenceId, new ExampleRequest());

        request.GetReferenceType().Should().Be(typeof(Guid));
    }
}