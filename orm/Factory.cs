using System;
using System.IO;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public enum SystemType : int // TODO: binary = byte[] !
        {
            Unknown  = -1, // unknown type exception
            Null     =  0, // 0 byte
            Boolean  =  1, // true|false (1 byte)
            Char     =  2, // Unicode (16-bit) character
            Byte     =  3, // 8-bit unsigned integer
            SByte    =  4, // 8-bit signed integer
            Int16    =  5, // 16-bit signed integer
            UInt16   =  6, // 16-bit unsigned integer
            Int32    =  7, // 32-bit signed integer
            UInt32   =  8, // 32-bit unsigned integer
            Int64    =  9, // 64-bit signed integer
            UInt64   = 10, // 64-bit unsigned integer
            Single   = 11, // single-precision (32-bit) floating-point number
            Double   = 12, // double-precision (64-bit) floating-point number
            Decimal  = 13, // decimal (128-bit) value
            String   = 14, // An immutable, fixed-length string of Unicode characters
            DateTime = 15, // 64-bit binary value (Int64)
            Guid     = 16, // 128-bit integer (16 bytes)
            Type     = 17, // discriminator (int = 4 bytes) ... Type.GetType("System.RuntimeType") !?
            List     = 18, // one-dimensional array { type + count + elements }
            UserType = 39  // indicates number of discriminator values reserved by type system (starting number for user types)
        }

        public sealed class Factory // TODO: all serialization & deserialization export to -> IBinaryFormatter !
        {
            # region " string constants "

            internal const string ISerializable = "zhichkin.orm.ISerializable";
            internal const string IPersistent   = "zhichkin.orm.IPersistent`1";
            
            # endregion

            private UserType.Registry registry = null;

            private IdentityMap identity_map = new IdentityMap();

            internal void Initialize(Assembly domain_model)
            {
                object[] attributes = domain_model.GetCustomAttributes(typeof(DomainModelAttribute), false);

                if (attributes != null && attributes.Length == 1) // сборка пользовательских типов данных
                {
                    DomainModelAttribute dma = (DomainModelAttribute)attributes[0];

                    if (!string.IsNullOrWhiteSpace(dma.Name)) // имя предметной области (домена) не должно быть пустым
                    {
                        if (registry == null) registry = new UserType.Registry(dma.Name);

                        foreach (Type type in domain_model.GetTypes()) // adding custom types
                        {
                            attributes = type.GetCustomAttributes(typeof(DiscriminatorAttribute), false);

                            if (attributes != null && attributes.Length == 1)
                            {
                                DiscriminatorAttribute attribute = (DiscriminatorAttribute)attributes[0];

                                registry.Add(type, attribute.Discriminator);
                            }
                        }
                    }
                }
            }

            public string DomainName { get { return (registry == null) ? null : registry.DomainName; } }

            // function tests if custom type is well-formed and, if so,
            // returns it's domain name and discriminator values
            public static bool IsWellformed(Type type, ref string domain_name, ref int discriminator)
            {
                if (type == null) throw new ArgumentNullException("type");

                bool result = false;

                object[] attributes = type.Assembly.GetCustomAttributes(typeof(DomainModelAttribute), false);

                if (attributes != null && attributes.Length == 1) // сборка пользовательских типов данных
                {
                    DomainModelAttribute dma = (DomainModelAttribute)attributes[0];

                    if (!string.IsNullOrWhiteSpace(dma.Name)) // имя предметной области (домена) не должно быть пустым
                    {
                        attributes = type.GetCustomAttributes(typeof(DiscriminatorAttribute), false);

                        if (attributes != null && attributes.Length == 1) // пользовательский тип данных
                        {
                            DiscriminatorAttribute da = (DiscriminatorAttribute)attributes[0];

                            if (da.Discriminator > (int)SystemType.UserType) // значение дискриминатора должно быть больше максимального зарезервированного системой
                            {
                                // пользовательский тип должен наследовать интерфейс ISerializable

                                if (type.GetInterface(Factory.ISerializable) != null)
                                {
                                    // specification for a default constructor is not defined yet ...

                                    domain_name = dma.Name; discriminator = da.Discriminator; result = true;
                                }
                            }
                        }
                    }
                }

                return result;
            }

            # region " Type to discriminator and vice versa converter "

            public string GetDomainName(Type type)
            {
                string domain_name = string.Empty;

                int discriminator = (int)SystemType.Unknown;

                if (Factory.IsWellformed(type, ref domain_name, ref discriminator))
                {
                    if (this.GetType(discriminator) == null)
                    {
                        domain_name = string.Empty;
                    }
                }

                return domain_name;
            }

            public int GetDiscriminator(Type type)
            {
                string domain_name = string.Empty;

                int discriminator = (int)SystemType.Unknown;

                if (type == null) discriminator = (int)SystemType.Null;
                else if (type == typeof(bool)) discriminator = (int)SystemType.Boolean;
                else if (type == typeof(char)) discriminator = (int)SystemType.Char;
                else if (type == typeof(byte)) discriminator = (int)SystemType.Byte;
                else if (type == typeof(sbyte)) discriminator = (int)SystemType.SByte;
                else if (type == typeof(short)) discriminator = (int)SystemType.Int16;
                else if (type == typeof(ushort)) discriminator = (int)SystemType.UInt16;
                else if (type == typeof(int)) discriminator = (int)SystemType.Int32;
                else if (type == typeof(uint)) discriminator = (int)SystemType.UInt32;
                else if (type == typeof(long)) discriminator = (int)SystemType.Int64;
                else if (type == typeof(ulong)) discriminator = (int)SystemType.UInt64;
                else if (type == typeof(float)) discriminator = (int)SystemType.Single;
                else if (type == typeof(double)) discriminator = (int)SystemType.Double;
                else if (type == typeof(decimal)) discriminator = (int)SystemType.Decimal;
                else if (type == typeof(string)) discriminator = (int)SystemType.String;
                else if (type == typeof(DateTime)) discriminator = (int)SystemType.DateTime;
                else if (type == typeof(Guid)) discriminator = (int)SystemType.Guid;

                else if (Factory.IsWellformed(type, ref domain_name, ref discriminator))
                {
                    if (this.GetType(discriminator) == null)
                    {
                        discriminator = (int)SystemType.Unknown;
                    }
                }

                else if (type.GetInterface("System.Collections.Generic.IList`1") != null)
                {
                    Type argument = type.GetGenericArguments()[0];

                    if (GetDiscriminator(argument) > (int)SystemType.Null)
                    {
                        discriminator = (int)SystemType.List;
                    }
                }

                else if (type == typeof(Type) || type == Type.GetType("System.RuntimeType")) discriminator = (int)SystemType.Type;

                return discriminator;
            }

            public Type GetType(int discriminator)
            {
                Type type = null;

                if (discriminator < (int)SystemType.UserType)
                {
                    switch (discriminator)
                    {
                        case (int)SystemType.Boolean: type = typeof(bool); break;
                        case (int)SystemType.Char: type = typeof(char); break;
                        case (int)SystemType.Byte: type = typeof(byte); break;
                        case (int)SystemType.SByte: type = typeof(sbyte); break;
                        case (int)SystemType.Int16: type = typeof(short); break;
                        case (int)SystemType.UInt16: type = typeof(ushort); break;
                        case (int)SystemType.Int32: type = typeof(int); break;
                        case (int)SystemType.UInt32: type = typeof(uint); break;
                        case (int)SystemType.Int64: type = typeof(long); break;
                        case (int)SystemType.UInt64: type = typeof(ulong); break;
                        case (int)SystemType.Single: type = typeof(float); break;
                        case (int)SystemType.Double: type = typeof(double); break;
                        case (int)SystemType.Decimal: type = typeof(decimal); break;
                        case (int)SystemType.String: type = typeof(string); break;
                        case (int)SystemType.DateTime: type = typeof(DateTime); break;
                        case (int)SystemType.Guid: type = typeof(Guid); break;
                        case (int)SystemType.Type: type = typeof(Type); break;
                        case (int)SystemType.List: type = typeof(IList<>); break;
                    }
                }
                else
                {
                    UserType info = registry.GetTypeInfo(discriminator);

                    if (info != null)
                    {
                        type = info.Type;
                    }
                }

                return type;
            }

            # endregion

            # region " Factory methods "

            public object New(Type type)
            {
                if (type == null) throw new ArgumentNullException("type");

                UserType info = registry.GetTypeInfo(type);

                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.DefaultConstructor();

                Entity entity = item as Entity;
                if (entity != null)
                {
                    identity_map.Add(entity);
                    return entity;
                }

                return item;
            }

            public object New(Type type, ISerializable key)
            {
                if (key == null) throw new ArgumentNullException("key");

                if (type == null) throw new ArgumentNullException("type");

                UserType info = registry.GetTypeInfo(type);

                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.VirtualConstructor(key);

                Entity entity = item as Entity;
                if (entity != null)
                {
                    bool exists = identity_map.Find(type, key, ref entity);
                    if (!exists)
                    {
                        identity_map.Add(entity);
                    }
                    return entity;
                }

                return item;
            }

            # endregion

            # region " Serialization "

            // IBinaryFormatter performs shallow copying of objects (objects are not copied deeply, but only references)
            // для копирования объектов тех классов, которые реализуют IPersistent, используется метод поверхностного (shallow) копирования

            public void Serialize(BinaryWriter stream, bool value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, char value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, byte value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, sbyte value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, short value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, ushort value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, int value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, uint value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, long value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, ulong value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, float value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, double value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, decimal value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, string value) { stream.Write(value); }
            public void Serialize(BinaryWriter stream, DateTime value) { stream.Write(value.ToBinary()); }
            public void Serialize(BinaryWriter stream, Guid value) { stream.Write(value.ToByteArray()); }
            public void Serialize(BinaryWriter stream, Type value) { stream.Write(this.GetDiscriminator(value)); }

            public void Serialize(BinaryWriter stream, ISerializable value)
            {
                if (value == null)
                {
                    stream.Write((int)SystemType.Null);
                }
                else
                {
                    Type type = value.GetType();

                    UserType info = registry.GetTypeInfo(type);

                    if (info == null) throw new UnknownTypeException(type.FullName);

                    value.Serialize(stream);
                }
            }

            // used by Parameter class
            internal void Serialize(BinaryWriter stream, Type type, object value)
            {
                if (type == null) throw new ArgumentNullException("type");
                if (value == null) throw new ArgumentNullException("value");
                if (type != value.GetType()) throw new ArgumentOutOfRangeException("type != value.GetType()");

                int discriminator = this.GetDiscriminator(type);

                if (discriminator == (int)SystemType.Unknown)
                {
                    throw new UnknownTypeException(type.FullName);
                }
                else if (discriminator == (int)SystemType.List)
                {
                    if (type.GetInterface("System.Collections.Generic.IList`1") != null)
                    {
                        Type argument = type.GetGenericArguments()[0];

                        discriminator = this.GetDiscriminator(argument);

                        IList list = (IList)value;

                        int count = list.Count;

                        this.Serialize(stream, (int)SystemType.List); // List<> type
                        this.Serialize(stream, discriminator);        // item type
                        this.Serialize(stream, count);                // count of items

                        for (int i = 0; i < count; i++)
                        {
                            this.Serialize(stream, discriminator, list[i]);
                        }
                    }
                }
                else
                {
                    this.Serialize(stream, discriminator);        // value type
                    this.Serialize(stream, discriminator, value); // value itself
                }
            }

            private void Serialize(BinaryWriter stream, int discriminator, object value)
            {
                if (discriminator < (int)SystemType.UserType)
                {
                    switch (discriminator)
                    {
                        case (int)SystemType.Boolean: this.Serialize(stream, (bool)value); break;
                        case (int)SystemType.Char: this.Serialize(stream, (char)value); break;
                        case (int)SystemType.Byte: this.Serialize(stream, (byte)value); break;
                        case (int)SystemType.SByte: this.Serialize(stream, (sbyte)value); break;
                        case (int)SystemType.Int16: this.Serialize(stream, (short)value); break;
                        case (int)SystemType.UInt16: this.Serialize(stream, (ushort)value); break;
                        case (int)SystemType.Int32: this.Serialize(stream, (int)value); break;
                        case (int)SystemType.UInt32: this.Serialize(stream, (uint)value); break;
                        case (int)SystemType.Int64: this.Serialize(stream, (long)value); break;
                        case (int)SystemType.UInt64: this.Serialize(stream, (ulong)value); break;
                        case (int)SystemType.Single: this.Serialize(stream, (float)value); break;
                        case (int)SystemType.Double: this.Serialize(stream, (double)value); break;
                        case (int)SystemType.Decimal: this.Serialize(stream, (decimal)value); break;
                        case (int)SystemType.String: this.Serialize(stream, (string)value); break;
                        case (int)SystemType.DateTime: this.Serialize(stream, (DateTime)value); break;
                        case (int)SystemType.Guid: this.Serialize(stream, (Guid)value); break;
                        case (int)SystemType.Type: this.Serialize(stream, (Type)value); break;
                    }
                }
                else
                {
                    this.Serialize(stream, (ISerializable)value);
                }
            }

            # endregion

            # region " Deserialization "

            public void Deserialize(BinaryReader stream, ref bool value) { value = stream.ReadBoolean(); }
            public void Deserialize(BinaryReader stream, ref char value) { value = stream.ReadChar(); }
            public void Deserialize(BinaryReader stream, ref byte value) { value = stream.ReadByte(); }
            public void Deserialize(BinaryReader stream, ref sbyte value) { value = stream.ReadSByte(); }
            public void Deserialize(BinaryReader stream, ref short value) { value = stream.ReadInt16(); }
            public void Deserialize(BinaryReader stream, ref ushort value) { value = stream.ReadUInt16(); }
            public void Deserialize(BinaryReader stream, ref int value) { value = stream.ReadInt32(); }
            public void Deserialize(BinaryReader stream, ref uint value) { value = stream.ReadUInt32(); }
            public void Deserialize(BinaryReader stream, ref long value) { value = stream.ReadInt64(); }
            public void Deserialize(BinaryReader stream, ref ulong value) { value = stream.ReadUInt64(); }
            public void Deserialize(BinaryReader stream, ref float value) { value = stream.ReadSingle(); }
            public void Deserialize(BinaryReader stream, ref double value) { value = stream.ReadDouble(); }
            public void Deserialize(BinaryReader stream, ref decimal value) { value = stream.ReadDecimal(); }
            public void Deserialize(BinaryReader stream, ref string value) { value = stream.ReadString(); }
            public void Deserialize(BinaryReader stream, ref DateTime value) { value = DateTime.FromBinary(stream.ReadInt64()); }
            public void Deserialize(BinaryReader stream, ref Guid value) { value = new Guid(stream.ReadBytes(16)); }
            public void Deserialize(BinaryReader stream, ref Type value) { value = this.GetType(stream.ReadInt32()); }

            public object Deserialize(BinaryReader stream)
            {
                long position = stream.BaseStream.Position;

                Type type = null;

                this.Deserialize(stream, ref type);

                if (type == null) { return null; }

                UserType info = registry.GetTypeInfo(type);

                if (info == null) throw new UnknownTypeException(type.FullName);

                byte _state = byte.MaxValue;

                this.Deserialize(stream, ref _state);

                PersistenceState state = (PersistenceState)_state;

                ISerializable value = null;

                if (state == PersistenceState.Virtual) // reference object
                {
                    ISerializable key = info.KeyConstructor();

                    key.Deserialize(stream);

                    value = (ISerializable)this.New(type, key); // if(info.VirtualConstructor == null) framework error !!!
                }
                else // value object or embedded value
                {
                    value = (ISerializable)this.New(type); // if(info.DefaultConstructor == null) framework error !!!

                    stream.BaseStream.Position = position;

                    value.Deserialize(stream);
                }

                return value;
            }

            // used by Parameter class
            internal object Deserialize(BinaryReader stream, int discriminator)
            {
                object value = null;

                if (discriminator == (int)SystemType.List)
                {
                    int item_discriminator = (int)SystemType.Unknown;

                    this.Deserialize(stream, ref item_discriminator);

                    Type item_type = this.GetType(item_discriminator);

                    if (item_type == null) throw new UnknownTypeException("discriminator(" + item_discriminator.ToString() + ")");

                    value = Activator.CreateInstance(typeof(List<>).MakeGenericType(item_type));

                    int count = 0;

                    this.Deserialize(stream, ref count);

                    if (count > 0)
                    {
                        IList list = (IList)value;

                        for (int i = 0; i < count; i++)
                        {
                            list.Add(this.Deserialize(stream, item_discriminator));
                        }
                    }
                }
                else if (discriminator < (int)SystemType.UserType)
                {
                    bool bool_value = default(bool);
                    char char_value = default(char);
                    byte byte_value = default(byte);
                    sbyte sbyte_value = default(sbyte);
                    short short_value = default(short);
                    ushort ushort_value = default(ushort);
                    int int_value = default(int);
                    uint uint_value = default(uint);
                    long long_value = default(long);
                    ulong ulong_value = default(ulong);
                    float float_value = default(float);
                    double double_value = default(double);
                    decimal decimal_value = default(decimal);
                    string string_value = string.Empty;
                    DateTime DateTime_value = default(DateTime);
                    Guid Guid_value = Guid.Empty;
                    Type Type_value = null;

                    switch (discriminator)
                    {
                        case (int)SystemType.Boolean: { this.Deserialize(stream, ref bool_value); value = bool_value; break; }
                        case (int)SystemType.Char: { this.Deserialize(stream, ref char_value); value = char_value; break; }
                        case (int)SystemType.Byte: { this.Deserialize(stream, ref byte_value); value = byte_value; break; }
                        case (int)SystemType.SByte: { this.Deserialize(stream, ref sbyte_value); value = sbyte_value; break; }
                        case (int)SystemType.Int16: { this.Deserialize(stream, ref short_value); value = short_value; break; }
                        case (int)SystemType.UInt16: { this.Deserialize(stream, ref ushort_value); value = ushort_value; break; }
                        case (int)SystemType.Int32: { this.Deserialize(stream, ref int_value); value = int_value; break; }
                        case (int)SystemType.UInt32: { this.Deserialize(stream, ref uint_value); value = uint_value; break; }
                        case (int)SystemType.Int64: { this.Deserialize(stream, ref long_value); value = long_value; break; }
                        case (int)SystemType.UInt64: { this.Deserialize(stream, ref ulong_value); value = ulong_value; break; }
                        case (int)SystemType.Single: { this.Deserialize(stream, ref float_value); value = float_value; break; }
                        case (int)SystemType.Double: { this.Deserialize(stream, ref double_value); value = double_value; break; }
                        case (int)SystemType.Decimal: { this.Deserialize(stream, ref decimal_value); value = decimal_value; break; }
                        case (int)SystemType.String: { this.Deserialize(stream, ref string_value); value = string_value; break; }
                        case (int)SystemType.DateTime: { this.Deserialize(stream, ref DateTime_value); value = DateTime_value; break; }
                        case (int)SystemType.Guid: { this.Deserialize(stream, ref Guid_value); value = Guid_value; break; }
                        case (int)SystemType.Type: { this.Deserialize(stream, ref Type_value); value = Type_value; break; }
                        default: { throw new UnknownTypeException("discriminator(" + discriminator.ToString() + ")"); }
                    }
                }
                else
                {
                    value = this.Deserialize(stream);
                }

                return value;
            }

            # endregion
        }
    }
}
// tip: if (type.IsPublic && type.IsAbstract && type.IsSealed) /* that means static class */