using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using CubaRest.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using CubaRest.Model;

namespace CubaRest.Tests
{
    using SampleEntityType = SampleTypes.Config; // Менять синхронно с sampleCubaType!

    [TestClass]
    public class CubaRestApiTests : CubaRestTestsBase
    {
        const string sampleCubaType = "sys$Config"; // Менять синхронно с SampleEntityType!

        #region Token
        /// <summary>
        /// Успешное получение access-токена. Полученный токен должен быть непустым и соответствовать формату UUID.
        /// В тестовом окружении ответ должен быть получен не более, чем за 5 секунд.
        /// </summary>
        [TestMethod]
        [Timeout(5000)]
        public void RequestAccessToken_Succeeds()
        {
            var typedResult = (string)privateApi.Invoke(
                    "RequestAccessToken",
                    new Type[] { typeof(string) },
                    new object[] { api.RefreshToken });
        }
        
        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "Запрос access токена на основе недействительного refresh токена должно вызывать исключение")]
        public void RequestAccessTokenWithIncorrectRefreshToken_Fails()
        {
            try
            {
                var typedResult = (string)privateApi.Invoke(
                        "RequestAccessToken",
                        new Type[] { typeof(string) },
                        new object[] { "incorrect-refresh-token" });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
        #endregion

        #region Endpoint
        [TestMethod]
        [ExpectedException(typeof(CubaInvalidConnectionParametersException), "При указании пустого endpoint должно выбрасываться исключение")]
        public void RequestWithEmptyEndpoint_Fails()
        {
            var notWorkingApi = new CubaRestApi("", basicUsername, basicPassword, username, password);
        }

        [TestMethod]
        [ExpectedException(typeof(CubaInvalidConnectionParametersException), "При указании endpoint, не являющимся корректным http-адресом, должно выбрасываться исключение")]
        public void RequestWithIncorrectEndpointFormat_Fails()
        {
            var notWorkingApi = new CubaRestApi("This is not an URL", basicUsername, basicPassword, username, password);
        }

        [TestMethod]
        [ExpectedException(typeof(CubaNotFoundException), "При указании недействительного endpoint должно выбрасываться исключение")]
        public void RequestWithIncorrectEndpoint_Fails()
        {
            var notWorkingApi = new CubaRestApi("http://google.com", basicUsername, basicPassword, username, password);
        }
        #endregion

        #region Basic auth
        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "При указании пустого basicUsername должно выбрасываться исключение")]
        public void RequestWithEmptyBasicUsername_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, "", basicPassword, username, password);
        }

        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "При указании некорректного basicUsername должно выбрасываться исключение")]
        public void RequestWithIncorrectBasicUsername_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, "incorrect-basic-username", basicPassword, username, password);
        }

        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "При указании пустого basicPassword должно выбрасываться исключение")]
        public void RequestWithEmptyBasicPassword_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, basicUsername, "", username, password);
        }

        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "При указании некорректного basicPassword должно выбрасываться исключение")]
        public void RequestWithIncorrectBasicPassword_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, basicUsername, "incorrect-basic-password", username, password);
        }
        #endregion

        #region Main auth
        [TestMethod]
        [ExpectedException(typeof(CubaInvalidConnectionParametersException), "При указании пустого username должно выбрасываться исключение")]
        public void RequestWithEmptyMainUsername_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, basicUsername, basicPassword, "", password);
        }


        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "При указании некорректного basicUsername должно выбрасываться исключение")]
        public void RequestWithIncorrectMainUsername_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, basicUsername, basicPassword, "incorrect-username", password);
        }

        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "При указании пустого password должно должно выбрасываться исключение")]
        public void RequestWithEmptyMainPassword_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, basicUsername, basicPassword, username, "");
        }

        [TestMethod]
        [ExpectedException(typeof(CubaAccessException), "При указании некорректного basicPassword должно выбрасываться исключение")]
        public void RequestWithIncorrectMainPassword_Fails()
        {
            var notWorkingApi = new CubaRestApi(endpoint, basicUsername, basicPassword, username, "incorrect-password");
        }
        #endregion

        #region Получение списка сущностей
        /// <summary>
        /// Получение списка сущностей с преобразованием в массив целевого типа
        /// </summary>
        [TestMethod]
        public void ListEntitiesTyped_Succeeds()
        {
            var result = api.ListEntities<SampleEntityType>();
        }

        // DEPRECATED
        ///// <summary>
        ///// Получение списка сущностей с преобразованием в List<Dictionary<string, string>>
        ///// </summary>
        //[TestMethod]
        //public void ListEntitiesUntyped_Succeeds()
        //{
        //    var result = api.ListEntities(sampleCubaType);
        //}

        /// <summary>
        /// Получение списка сущностей несуществующего на сервере типа
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CubaEntityMappingSchemeException), "При запросе несуществующего типа должно выбрасываться исключение")]
        public void ListEntitiesOfNotExistingType_Fails()
        {
            var result = api.ListEntities<NotExistingRestApiType>();
        }
        private class NotExistingRestApiType : Model.Entity { }

        // DEPRECATED
        ///// <summary>
        ///// Получение списка сущностей с преобразованием в List<Dictionary<string, string>> с указанием некорректного названия метакласса 
        ///// </summary>
        //[TestMethod]
        //[ExpectedException(typeof(CubaMetaclassNotFoundException), "При запросе несуществующего типа должно выбрасываться исключение")]
        //public void ListEntitiesIncorrectMetaclass_Fails()
        //{
        //    var result = api.ListEntities("incorrect$MetaclassName");
        //}

        /// <summary>
        /// Попытка задать некорректные дополнительные параметры при выборе списка сущностей
        /// </summary>
        [TestMethod]
        public void ListEntitiesWithIncorrectParameters()
        {
            // Несуществующий view
            Assert.ThrowsException<CubaViewNotFoundException>(() => 
                        { api.ListEntities<SampleEntityType>(new EntityListAttributes() { View = "incorrect_view" }); });

            // Отрицательный limit
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => 
                        { api.ListEntities<SampleEntityType>(new EntityListAttributes() { Limit = -1 }); });

            // Отрицательный offset
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            { api.ListEntities<SampleEntityType>(new EntityListAttributes() { Offset = -1 }); });

            // Несуществующее поле для сортировки
            /// У сервера нет специфического сообщения об ошибке сортировки, выдаётся 500 ошибка и 
            /// {
            ///    "error": "Server error",
            ///    "details": ""
            /// }
            /// Поэтому с нашей стороны адекватная реакция - CubaNotImplementedException
            Assert.ThrowsException<CubaNotImplementedException>(() =>
            { api.ListEntities<SampleEntityType>(new EntityListAttributes() { Sort = "incorrect_sort" }); });
        }        
        #endregion

        #region Получение сущности по id
        /// <summary>
        /// Получение сущности по id
        /// </summary>
        [TestMethod]
        public void GetEntityById_Succeeds()
        {
            // нам нужен id
            var entities = api.ListEntities<SampleEntityType>();

            Assert.IsTrue(entities != null && entities.Any(),
                $"Ни одной сущности типа {typeof(SampleEntityType)} не найдено, поэтому невозможно запросить такую сущность по id");

            var sampleEntityId = entities.First().Id;
            var resultTyped = api.GetEntity<SampleEntityType>(sampleEntityId);
            var resultUntyped = api.GetEntity(sampleCubaType, sampleEntityId);
        }

        /// <summary>
        /// Получение сущности по id с неправильными параметрами
        /// </summary>
        [TestMethod]
        public void GetEntityByIdWithIncorrectParameters_Succeeds()
        {
            Assert.ThrowsException<CubaInvalidFormatException>(() => 
                { api.GetEntity<SampleEntityType>("incorrect-format-entity-id"); }, 
                "При запросе сущности по id, не соответствующему формату UUID, должно выбрасываться исключение");

            Assert.ThrowsException<CubaEntityNotFoundException>(() => 
                { api.GetEntity<SampleEntityType>("66666666-4f98-2ab9-0618-5d75c410f7fa"); },
                "При запросе сущности по несуществующему id должно выбрасываться исключение");
        }        
        #endregion

        // TEST: тесты для перечислений

        #region Вспомогательные методы
        [TestMethod]
        public void GetRestApiNameForEntity_Succeeds()
        {
            Assert.AreEqual(CubaRestApi.GetCubaNameForType<SampleEntityType>(), sampleCubaType);
        }

        /// <summary>
        /// Проверка названия Metaclass на соответствие формату xxx$XxxxXxxxXxxx
        /// </summary>
        [TestMethod]
        public void ValidateMetaclassName_Succeeds()
        {
            CubaRestApi.ValidateMetaclassNameFormat("std$ProduceStandard123");
        }

        [TestMethod]
        [ExpectedException(typeof(CubaInvalidFormatException), "Валидация названия метакласса без $ должна выбрасывать исключение")]
        public void ValidateMetaclassWithoutDollarSign_Fails()
        {
            CubaRestApi.ValidateMetaclassNameFormat("stdProduceStandard");
        }

        [TestMethod]
        [ExpectedException(typeof(CubaInvalidFormatException), "Валидация названия метакласса с маленькими буквами должна выбрасывать исключение")]
        public void ValidateMetaclassSmallLetters_Fails()
        {
            CubaRestApi.ValidateMetaclassNameFormat("std$producestandard");
        }

        [TestMethod]
        [ExpectedException(typeof(CubaInvalidFormatException), "Валидация названия метакласса с посторонним символом в префиксе должна выбрасывать исключение")]
        public void ValidateMetaclassDigitInPrefix_Fails()
        {
            CubaRestApi.ValidateMetaclassNameFormat("std2$ProduceStandard");
        }
        #endregion
    }
}