# Self-tracking entities ORM solution

The solution is based on [entity state machine](https://github.com/zhichkin/orm/blob/master/docs/Persistent%20Object%20State%20Machine.png). Creation of entity instances is provided by Factory, which keeps track on created entities with the help of IdentityMap. This aims to have exactly one reference for the entity instance within Context's Factory class instance. See the method "New" of the class "UserTypeFactory" for details. The Factory is used by Context singleton class, which is on it's own turn is the entry point for the ORM usage. It is very similar to the DbContext class in .NET Entity Framework.

The ORM provides lazy-loading by meaning of Virtual state of an entity and supports multiple valued properties. That will say that it is possible to have the following classes system:
```C#
public class DocumentBase { }
public class OrderDocument : DocumentBase { }
public class PaymentDocument : DocumentBase { }
public class OrderPaymentsRegister
{
  DateTime DateOfEvent { get; set; }
  DocumentBase Document { get; set; }
  double Sum { get; set; }
  int RecordType { get; set; } // income or expense
}
```
To support such kind of properties like Document in OrderPaymentsRegister class the ORM has concept of discriminator. This is an integer value corresponding some class. Similar to TypeCode enum in .NET. A special lookup is used to bind entity types to int. See Register class nested into UserType class. The lookup is built by Register class loading assembly containing entities marked by attributes in the Metadata.cs file. This concept is expressed in the following class: public abstract class Persistent<TKey> (see Persistent.cs file).

The ORM supports also concept of reference and value objects according to the corresponding patterns: [value object](https://martinfowler.com/bliki/ValueObject.html) and [here](https://martinfowler.com/bliki/EvansClassification.html)

Example of reference entity
```C#
public sealed partial class InfoBase : EntityBase
{
    private static readonly IDataMapper _mapper = MetadataPersistentContext.Current.GetDataMapper(typeof(InfoBase));
    
    public InfoBase() : base(_mapper) { }
    public InfoBase(Guid identity) : base(_mapper, identity) { }
    public InfoBase(Guid identity, PersistentState state) : base(_mapper, identity, state) { }

    private string server = string.Empty;
    private string database = string.Empty;
    private string username = string.Empty;
    private string password = string.Empty;

    public string Server { set { Set<string>(value, ref server); } get { return Get<string>(ref server); } }
    public string Database { set { Set<string>(value, ref database); } get { return Get<string>(ref database); } }
    public string UserName { set { Set<string>(value, ref username); } get { return Get<string>(ref username); } }
    public string Password { set { Set<string>(value, ref password); } get { return Get<string>(ref password); } }
}
```
More implementation examples can be found here: https://github.com/zhichkin/one-c-sharp/tree/master/src/Metadata.Model
