using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CubaRest.Model;
using CubaRest.Model.Reflection;
using CubaRest.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;

namespace CubaRest.Tests
{
    public class CubaTypeMappingTestsEngine : CubaRestTestsBase
    {
        public void CheckEntityClusterMapping(Type clusterClass)
        {
            var types = clusterClass.GetNestedTypes().Where(t => t.GetCustomAttributes<CubaNameAttribute>(false).Any());

            Assert.IsTrue(types != null && types.Count() > 0,
                $"В классе {clusterClass.Name} ни одного вложенного класса с атрибутом CubaName не найдено");

            foreach (var type in types)
                CheckEntityMapping(type);
        }

        public void CheckEntityMapping(Type entityType, bool strictMode = false)
        {
            Assert.IsTrue(entityType.IsSubclassOf(typeof(Entity)), $"Тип {entityType.Name} должен наследоваться от Entity");

            string cubaType = CubaRestApi.GetCubaNameForType(entityType);
            CubaRestApi.ValidateMetaclassNameFormat(cubaType); // Проверяем, что прописанное в атрибуте название метакласса валидно

            EntityType cubaTypeMetadata = GetTypeMetadata(cubaType);

            Assert.IsFalse(string.IsNullOrEmpty(cubaTypeMetadata.EntityName),
                "Ошибка в структуре данных Кубы: название поля не может быть пустым");

            // Проверяем, что из рефлексии пришёл нужный тип
            Assert.AreEqual(cubaTypeMetadata.EntityName, cubaType, 
                $"В рефлексии Кубы название типа сущности {cubaTypeMetadata.EntityName} отличается от запрошенного типа {cubaType}");

            // Проверяем наличие соответствий по каждому из полей
            foreach (var cubaProperty in cubaTypeMetadata.Properties)
                CheckEntityPropertyMapping(entityType, cubaType, cubaProperty, strictMode);

            // Проверяем отсутствие в классе посторонних полей (спорно, но пусть пока будет)
            var cubaPropertyNames = cubaTypeMetadata.Properties.Select(p => p.Name.ToPascalCase());
            foreach (var property in entityType.GetProperties(BindingFlags.DeclaredOnly))
                Assert.IsTrue(cubaPropertyNames.Contains(property.Name), $"Свойство {entityType.GetNameWithDeclaring()}.{property.Name} отсутствует в схеме данных Кубы");
        }

        protected static void CheckEntityPropertyMapping(Type entityType, string cubaType, EntityField cubaProperty, bool strictMode)
        {
            Assert.IsTrue(entityType.IsSubclassOf(typeof(Entity)), $"Тип {entityType.Name} должен наследоваться от Entity");

            string entityFullName = entityType.GetNameWithDeclaring(); // Полное имя типа сущности вроде Std.Produce

            var validPropertyName = cubaProperty.Name.ToPascalCase(); // brief -> Brief
            var propertyInfo = entityType.GetProperty(validPropertyName); // свойство TEntity, которое мы сопоставляем с типом Кубы

            Assert.IsFalse(cubaProperty.Persistent && cubaProperty.Transient,
                "Ошибка в структуре данных Кубы: поле не может быть одновременно persistent и transient");

            // В строгом режиме (strictMode=true) проверяем наличие в классе всех свойств, в нестрогом - только Mandatory
            if (strictMode || cubaProperty.Mandatory)
                Assert.IsNotNull(propertyInfo,
                $"Для поля {cubaType}.{cubaProperty.Name} не найдено соответствия в виде свойства {entityFullName}.{validPropertyName}");

            // В нестрогом режиме если поле не обязательное и отсутствует в классе, дальнейшие проверки пропускаем
            if (propertyInfo == null)
                return;

            // Проверяем наличие атрибутов ограничений у свойства
            foreach (var attribute in CubaPropertyRestrictionBase.ListPropertyRestrictionAttributes())
            {
                var attributeName = attribute.Name.EndsWith("Attribute") ? attribute.Name.Remove(attribute.Name.Length - "Attribute".Length) : attribute.Name;

                var cubaPropertyInfo = typeof(EntityField).GetProperty(attributeName);
                Assert.IsNotNull(cubaPropertyInfo, $"В рефлексии Кубы не найдено поля { attributeName }");

                var cubaPropertyValue = typeof(EntityField).GetProperty(attributeName).GetValue(cubaProperty);
                Assert.IsTrue(cubaPropertyValue is bool, $"Ошибка рефлексии Кубы: поле { attributeName } должно иметь тип boolean");

                var attributePresense = (bool)cubaPropertyValue;
                Assert.AreEqual(
                    attributePresense,
                    propertyInfo.GetCustomAttributes(attribute, false).Any(),
                    $"Свойство {entityFullName}.{propertyInfo.Name} {(attributePresense ? "должно" : "не должно")} быть помечено атрибутом {attributeName}");
            }

            // Проверка атрибута Description
            {
                var description = propertyInfo.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>(false)?.Description;
                Assert.IsNotNull(description,
                    $"У {entityFullName}.{propertyInfo.Name} не найден атрибут Description");

                Assert.AreEqual(
                    cubaProperty.Description,
                    description,
                    $"Описание свойства {entityFullName}.{propertyInfo.Name} \"{description}\" не совпадает с описанием поля {cubaType}.{cubaProperty.Name} \"{ cubaProperty.Description }\"");
            }


            // Проверка множественности
            bool isMultipleCardinality = cubaProperty.Cardinality == Cardinality.ONE_TO_MANY || cubaProperty.Cardinality == Cardinality.MANY_TO_MANY;
            Assert.AreEqual(
                    typeof(IList).IsAssignableFrom(propertyInfo.PropertyType),
                    isMultipleCardinality,
                    $"Тип множественности поля {cubaType}.{cubaProperty.Name}.{cubaProperty.Cardinality} обязывает свойство {entityFullName}.{propertyInfo.Name} быть списком");

            var underlyingType = isMultipleCardinality ? propertyInfo.PropertyType.GenericTypeArguments[0] : propertyInfo.PropertyType;

            // Проверка соответствия типа
            switch (cubaProperty.AttributeType)
            {
                case AttributeType.DATATYPE:
                    if (isMultipleCardinality)
                        throw new NotImplementedException("Проверка соответствия типа для массивов встроенных типов пока не реализована");

                    Assert.IsTrue(EmbeddedTypes.Types.ContainsKey(cubaProperty.Type),
                        $"Встроенный тип поля {cubaType}.{cubaProperty.Name} {cubaProperty.Type} не поддерживается");

                    Assert.AreEqual(
                        EmbeddedTypes.Types[cubaProperty.Type],
                        underlyingType,
                        $"Тип поля {cubaType}.{cubaProperty.Name} {cubaProperty.Type} не совпадает с типом свойства {entityFullName} {propertyInfo.PropertyType.Name}");
                    break;

                case AttributeType.ASSOCIATION:
                case AttributeType.COMPOSITION:
                case AttributeType.ENUM:
                    Assert.AreEqual(
                        cubaProperty.Type, 
                        CubaRestApi.GetCubaNameForType(underlyingType),
                        $"Тип поля {cubaType}.{cubaProperty.Name} {cubaProperty.Type} не совпадает с типом свойства {entityFullName} {propertyInfo.PropertyType.Name}");
                    break;
            }
        }

        public void CheckEnumMapping(Type enumType)
        {
            Assert.IsTrue(enumType.IsEnum, $"Тип {enumType.Name} должен быть перечислением");

            string cubaType = CubaRestApi.GetCubaNameForType(enumType);
            CubaRestApi.ValidateEnumNameFormat(cubaType); // Проверяем, что прописанное в атрибуте название перечисления валидно

            EnumType cubaEnumMetadata = GetEnumMetadata(cubaType);

            Assert.IsFalse(string.IsNullOrEmpty(cubaEnumMetadata.Name),
                "Ошибка в структуре данных Кубы: название перечисления не может быть пустым");

            // Проверяем, что из рефлексии пришёл нужный тип
            Assert.AreEqual(cubaEnumMetadata.Name, cubaType,
                $"В рефлексии Кубы название типа сущности {cubaEnumMetadata.Name} отличается от запрошенного типа {cubaType}");

            // Проверяем наличие соответствий по каждому из полей
            foreach (var cubaValue in cubaEnumMetadata.Values)
                CheckEnumValueMapping(enumType, cubaEnumMetadata.Name, cubaValue);

            // Проверяем отсутствие в перечислении посторонних значений (спорно, но пусть пока будет)
            var cubaNames = cubaEnumMetadata.Values.Select(p => p.Name);

            foreach (var name in Enum.GetNames(enumType))
                Assert.IsTrue(cubaNames.Contains(name), $"Значение {enumType.GetNameWithDeclaring()}.{name} отсутствует в схеме данных Кубы");
        }

        protected static void CheckEnumValueMapping(Type enumType, string cubaEnumName, EnumField cubaValue)
        {
            Assert.IsTrue(enumType.IsEnum, $"Тип {enumType.Name} должен быть перечислением");

            // Для значения cubaValue проверяем наличие соответствующего значения в TEnum
            Assert.IsTrue(
                Enum.GetNames(enumType).Any(x => x == cubaValue.Name),
                $"Значение { cubaValue.Name } присутствует в перечислении Кубы { cubaEnumName }, но отсутствует в перечислении { enumType.Name }");

            // Проверяем совпадение поля Caption в Кубе и значения атрибута у значения перечисления TEnum
            var fieldInfo = enumType.GetField(cubaValue.Name);
            var attribute = fieldInfo?.GetCustomAttribute(typeof(System.ComponentModel.DescriptionAttribute)) as System.ComponentModel.DescriptionAttribute;

            Assert.AreEqual(
                attribute?.Description ?? "",
                cubaValue.Caption,
                $"Описание для значения перечисления { enumType.Name }.{ cubaValue.Name } \"{ attribute?.Description }\" отличается от описания значения перечисления Кубы  {cubaEnumName}.{cubaValue.Name} \"{ cubaValue.Caption }\"");

            // Проверяем, что число, соответствующее значению перечисления совпадает со значением поля id в перечислении Кубы
            if (int.TryParse(cubaValue.Id, out int cubaEnumNumber)) // проверяем только в тех случаях, когда cubaValue.Id - число
            {
                var enumNumberValue = (int)Enum.Parse(enumType, cubaValue.Name, false);

                Assert.AreEqual(
                    enumNumberValue,
                    cubaEnumNumber,
                    $"Численное представление значения перечисления { enumType.Name }.{ cubaValue.Name } { enumNumberValue } отличается от поля id значения перечисления Кубы {cubaEnumName}.{cubaValue.Name} { cubaValue.Id }");
            }
        }


        private List<EntityType> entityTypesCache = null;
        /// <summary>
        /// Вспомогательный метод для получения метаданных типа Кубы. Сразу запрашивает информацию по всем типам и сохраняет в кеш.
        /// </summary>
        /// <exception cref="CubaMetaclassNotFoundException"></exception>
        /// <exception cref="CubaException"></exception>
        private EntityType GetTypeMetadata(string cubaType)
        {
            if (entityTypesCache == null)
                entityTypesCache = api.ListTypes();

            // Сначала берём из кеша. Если не нашли, обращаемся к серверу.
            return entityTypesCache.FirstOrDefault(x => x.EntityName == cubaType) ?? api.GetTypeMetadata(cubaType);
        }

        private List<EnumType> enumTypesCache = null;
        /// <summary>
        /// Вспомогательный метод для получения метаданных перечисления Кубы. Сразу запрашивает информацию по всем перечислениям и сохраняет в кеш.
        /// </summary>
        /// <exception cref="CubaMetaclassNotFoundException"></exception>
        /// <exception cref="CubaException"></exception>
        private EnumType GetEnumMetadata(string cubaEnumType)
        {
            if (enumTypesCache == null)
                enumTypesCache = api.ListEnums();

            // Сначала берём из кеша. Если не нашли, обращаемся к серверу.
            return enumTypesCache.FirstOrDefault(x => x.Name == cubaEnumType) ?? api.GetEnumMetadata(cubaEnumType);
        }
    }
}
