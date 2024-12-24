
# OfX

OfX is an open-source, which focus on Attribute-based Data Mapping, simplifying data handling across services and enhancing maintainability.


## Project Highlights
Attribute-based Data Mapping in OfX is a feature that lets developers annotate properties in their data models with custom attributes. These attributes define how and from where data should be fetched, eliminating repetitive code and automating data retrieval.
For example, imagine a scenario where Service A needs a user’s name stored in Service B. With Attribute-based Data Mapping, Service A can define a UserName property annotated with [UserAttribute(nameof(UserId))]. This tells the system to automatically retrieve the UserName based on UserId, without writing custom code each time.

Example:

```C#
public sealed class MemberResponse : ModelResponse
{
    public string UserId { get; set; }

    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; }

    [UserOf(nameof(UserId))] public string UserName { get; set; }
    public DateTime CreatedTime { get; set; }
    public List<MemberMapRoleGroupResponse> MemberMapRoleGroups { get; set; }
    public bool IsActivated { get; set; }
    public bool IsRemoved { get; set; }
}
```
The [UserOfAttribute] annotation acts as a directive to automatically retrieve UserName based on UserId,you can also fetch custom fields as Email on the User Table using Expression like [UserOf(nameof(UserId), Expression="Email")]. This eliminates the need for manual mapping logic, freeing developers to focus on core functionality rather than data plumbing.
