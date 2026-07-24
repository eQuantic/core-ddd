using eQuantic.Core.Domain.Entities;
using FluentAssertions;

namespace eQuantic.Core.Domain.Tests.Entities;

public class FilteringCollectionTests
{
    [Test]
    public void TryParse_builds_a_typed_expression_model()
    {
        var parsed = FilteringCollection<ExampleEntity>.TryParse("total:gt(100)", null, out var collection);

        parsed.Should().BeTrue();
        collection.Should().ContainSingle();
    }

    [Test]
    public void TryParse_rejects_invalid_syntax()
    {
        var parsed = FilteringCollection<ExampleEntity>.TryParse("total:nope(100)", null, out var collection);

        parsed.Should().BeFalse();
        collection.Should().BeNull();
    }

    [Test]
    public void ToPredicate_combines_items_with_and()
    {
        FilteringCollection<ExampleEntity>.TryParse("total:gt(100)", null, out var collection);
        FilteringCollection<ExampleEntity>.TryParse("name:ct(a)", null, out var second);
        collection!.AddRange(second!);

        var predicate = collection.ToPredicate()!.Compile();

        predicate(new ExampleEntity { Total = 150, Name = "Maria" }).Should().BeTrue();
        predicate(new ExampleEntity { Total = 150, Name = "John" }).Should().BeFalse();
        predicate(new ExampleEntity { Total = 50, Name = "Maria" }).Should().BeFalse();
    }

    [Test]
    public void SortingCollection_parses_typed_sorts()
    {
        var parsed = SortingCollection<ExampleEntity>.TryParse("total:desc,name", null, out var collection);

        parsed.Should().BeTrue();
        collection.Should().HaveCount(2);
    }
}
