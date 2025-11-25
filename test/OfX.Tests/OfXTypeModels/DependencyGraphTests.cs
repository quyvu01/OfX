using System.Reflection;
using OfX.Accessors;
using OfX.ObjectContexts;
using OfX.Tests.Attributes;
using Shouldly;
using Xunit;

namespace OfX.Tests.OfXTypeModels;

public class DependencyGraphTests
{
    private readonly PropertyInfo _userId;
    private readonly PropertyInfo _userName;
    private readonly PropertyInfo _userEmail;
    private readonly PropertyInfo _userCustomExpression;
    private readonly PropertyInfo _provinceId;
    private readonly PropertyInfo _provinceName;

    public DependencyGraphTests()
    {
        var t = typeof(MemberResponse);

        _userId = t.GetProperty(nameof(MemberResponse.UserId));
        _userName = t.GetProperty(nameof(MemberResponse.UserName));
        _userEmail = t.GetProperty(nameof(MemberResponse.UserEmail));
        _userCustomExpression = t.GetProperty(nameof(MemberResponse.UserCustomExpression));
        _provinceId = t.GetProperty(nameof(MemberResponse.ProvinceId));
        _provinceName = t.GetProperty(nameof(MemberResponse.ProvinceName));
    }

    [Fact]
    public void DependencyGraph_Should_Contain_All_Dependencies()
    {
        var model = new OfXTypeModel(typeof(MemberResponse));
        var graph = model.DependencyGraph;

        graph.Count.ShouldBe(5);
        // 5 properties have dependencies: UserName, UserEmail, UserCustomExpression, ProvinceId, ProvinceName

        graph.Keys.ShouldContain(_userName);
        graph.Keys.ShouldContain(_userEmail);
        graph.Keys.ShouldContain(_userCustomExpression);
        graph.Keys.ShouldContain(_provinceId);
        graph.Keys.ShouldContain(_provinceName);
    }

    [Fact]
    public void UserName_Should_Depends_On_UserId()
    {
        var ctx = GetContext(nameof(MemberResponse.UserName));

        ctx.RequiredPropertyInfo.ShouldBe(_userId);
        ctx.TargetPropertyInfo.ShouldBe(_userName);
        ctx.SelectorPropertyName.ShouldBe(nameof(MemberResponse.UserId));
        ctx.Expression.ShouldBeNull();
        ctx.RuntimeAttributeType.ShouldBe(typeof(UserOfAttribute));
    }

    [Fact]
    public void UserEmail_Should_Depends_On_UserId()
    {
        var ctx = GetContext(nameof(MemberResponse.UserEmail));

        ctx.RequiredPropertyInfo.ShouldBe(_userId);
        ctx.TargetPropertyInfo.ShouldBe(_userEmail);
        ctx.SelectorPropertyName.ShouldBe(nameof(MemberResponse.UserId));
        ctx.Expression.ShouldBe("${UserAlias|Email}");
        ctx.RuntimeAttributeType.ShouldBe(typeof(UserOfAttribute));
    }

    [Fact]
    public void UserCustomExpression_Should_Depends_On_UserId()
    {
        var ctx = GetContext(nameof(MemberResponse.UserCustomExpression));

        ctx.RequiredPropertyInfo.ShouldBe(_userId);
        ctx.TargetPropertyInfo.ShouldBe(_userCustomExpression);
        ctx.SelectorPropertyName.ShouldBe(nameof(MemberResponse.UserId));
        ctx.Expression.ShouldBe("CustomExpression");
        ctx.RuntimeAttributeType.ShouldBe(typeof(UserOfAttribute));
    }

    [Fact]
    public void ProvinceId_Should_Depends_On_UserId()
    {
        var ctx = GetContext(nameof(MemberResponse.ProvinceId));

        ctx.RequiredPropertyInfo.ShouldBe(_userId);
        ctx.TargetPropertyInfo.ShouldBe(_provinceId);
        ctx.SelectorPropertyName.ShouldBe(nameof(MemberResponse.UserId));
        ctx.Expression.ShouldBe("ProvinceId");
        ctx.RuntimeAttributeType.ShouldBe(typeof(UserOfAttribute));
    }

    [Fact]
    public void ProvinceName_Should_Depends_On_ProvinceId_And_UserId()
    {
        var model = new OfXTypeModel(typeof(MemberResponse));
        var graph = model.DependencyGraph;

        var prop = typeof(MemberResponse).GetProperty(nameof(MemberResponse.ProvinceName));

        graph.ContainsKey(prop).ShouldBeTrue();

        var ctxs = graph[prop];

        ctxs.Length.ShouldBe(2); // ProvinceId + UserId

        var requiredProps = ctxs.Select(c => c.RequiredPropertyInfo).ToList();

        requiredProps.ShouldContain(
            typeof(MemberResponse).GetProperty(nameof(MemberResponse.ProvinceId))
        );

        requiredProps.ShouldContain(
            typeof(MemberResponse).GetProperty(nameof(MemberResponse.UserId))
        );

        // Check direct dependency (ProvinceName -> ProvinceId)
        var direct = ctxs.First(c => c.RequiredPropertyInfo.Name == nameof(MemberResponse.ProvinceId));
        direct.TargetPropertyInfo.ShouldBe(prop);
        direct.RuntimeAttributeType.ShouldBe(typeof(ProvinceOfAttribute));
        direct.Expression.ShouldBeNull();
        direct.SelectorPropertyName.ShouldBe(nameof(MemberResponse.ProvinceId));

        // Check indirect dependency (ProvinceName -> UserId)
        var indirect = ctxs.First(c => c.RequiredPropertyInfo.Name == nameof(MemberResponse.UserId));
        indirect.TargetPropertyInfo.ShouldBe(direct.RequiredPropertyInfo);
        indirect.RuntimeAttributeType.ShouldBe(typeof(UserOfAttribute)); // inherited through chain
        indirect.Expression.ShouldBe("ProvinceId"); // from ProvinceId attribute
        indirect.SelectorPropertyName.ShouldBe(nameof(MemberResponse.UserId));
    }


    private PropertyContext GetContext(string propertyName)
    {
        var model = new OfXTypeModel(typeof(MemberResponse));
        var graph = model.DependencyGraph;

        var prop = typeof(MemberResponse).GetProperty(propertyName);
        graph.ContainsKey(prop).ShouldBeTrue();

        var ctx = graph[prop];
        ctx.Length.ShouldBe(1); // mỗi property chỉ có 1 dependency

        return ctx[0];
    }
}