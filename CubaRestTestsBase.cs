using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CubaRest.Tests
{
    /// <summary>
    /// Точку доступа и логины/пароли для подключения к Кубе во всех тестах указывать здесь.
    /// </summary>
    /// <exception cref="CubaInvalidConnectionParametersException"></exception>
    public abstract class CubaRestTestsBase
    {
        protected readonly string endpoint, basicUsername, basicPassword, username, password;

        protected CubaRestApi api;
        protected PrivateObject privateApi; // специальная обёртка над api для тестирования protected/private методов CubaRestApi

        public CubaRestTestsBase()
        {
            var restApiConfigurationFile = "RestApiConnection.json";
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"CubaRest.Tests.{restApiConfigurationFile}")
                    ?? throw new CubaInvalidConnectionParametersException($"{restApiConfigurationFile} not found in project folder");            

            try
            {
                var reader = new StreamReader(stream);
                var json = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());

                endpoint = json["endpoint"];
                basicUsername = json["basicUsername"];
                basicPassword = json["basicPassword"];
                username = json["username"];
                password = json["password"];
            }
            catch (Exception ex)
            {
                throw new CubaInvalidConnectionParametersException($"Failed to get valid connection parameters from {restApiConfigurationFile}", ex);
            }

            /// Если возникают ошибки при создании объекта API, все тесты автоматически падают.
            api = new CubaRestApi(endpoint, basicUsername, basicPassword, username, password);
            privateApi = new PrivateObject(Activator.CreateInstance(typeof(CubaRestApi), endpoint, basicUsername, basicPassword, api.RefreshToken));
        }
    }
}
