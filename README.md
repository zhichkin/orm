# Self-tracking entities ORM solution

The solution is based on [entity state machine](https://github.com/zhichkin/orm/blob/master/docs/Persistent%20Object%20State%20Machine.png). Creation of entity instances is provided by Factory, which keeps track on created entities with the help of IdentityMap. This aims to have exactly one reference within Factory class instance. The Factory is used by Context singleton class, which is in it's own turn is the entry point for the ORM usage. It is very simular to DbContext class in .NET Entity Framework.

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
  int RecordingType { get; set; } // income or expense
}
```
To support such kind of properties like Document in OrderPaymentsRegister class the ORM has concept of discriminator. This an integer value corresponding some class. Simular to TypeCode enum in .NET. A special lookup is used to keep bindings of Type to int. See Register class nested into UserType class. The lookup is build by Register class loading assembly containing entities merked by attributes in Metadata file.
