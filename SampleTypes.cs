using CubaRest.Model;
using System;
using System.ComponentModel;

namespace CubaRest.Tests.SampleTypes
{
    [CubaName("sys$Config")]
    public class Config : Entity, IUuidEntity
    {
        /// <summary>Id</summary>
        [Description("Id")]
        public string Id { get; set; }

        /// <summary>Config.createdBy</summary>
        [Description("Config.createdBy")]
        public string CreatedBy { get; set; }

        /// <summary>Config.createTs</summary>
        [Description("Config.createTs")]
        public DateTime CreateTs { get; set; }

        /// <summary>Название</summary>
        [Description("Название")]
        public string Name { get; set; }

        /// <summary>Config.updatedBy</summary>
        [Description("Config.updatedBy")]
        public string UpdatedBy { get; set; }

        /// <summary>Config.updateTs</summary>
        [Description("Config.updateTs")]
        public DateTime UpdateTs { get; set; }

        /// <summary>Значение</summary>
        [Description("Значение")]
        public string Value { get; set; }

        /// <summary>Config.version</summary>
        [Description("Config.version")]
        public int Version { get; set; }
    }
}
