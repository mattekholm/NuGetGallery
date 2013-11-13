﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NuGetGallery.Auditing
{
    public abstract class AuditingService
    {
        public static readonly AuditingService None = new NullAuditingService();

        private static readonly JsonSerializerSettings _auditRecordSerializerSettings;

        static AuditingService()
        {
            var settings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.Indented,
                MaxDepth = 10,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                TypeNameHandling = TypeNameHandling.None
            };
            settings.Converters.Add(new StringEnumConverter());
            _auditRecordSerializerSettings = settings;
        }

        public virtual Task<Uri> SaveAuditRecord(AuditRecord record)
        {
            // Build an audit entry
            var entry = new AuditEntry(record, GetCurrentAuditEnvironment());

            // Serialize to json
            string rendered = RenderAuditEntry(entry);

            // Save the record
            return SaveAuditRecord(rendered, record.GetResourceType(), record.GetPath());
        }

        public virtual string RenderAuditEntry(AuditEntry entry)
        {
            return JsonConvert.SerializeObject(entry, _auditRecordSerializerSettings);
        }

        /// <summary>
        /// Performs the actual saving of audit data to an audit store
        /// </summary>
        /// <param name="auditData">The data to store in the audit record</param>
        /// <param name="resourceType">The type of resource affected by the audit (usually used as the first-level folder)</param>
        /// <param name="filePath">The file-system path to use to identify the audit record</param>
        /// <returns></returns>
        protected abstract Task<Uri> SaveAuditRecord(string auditData, string resourceType, string filePath);

        protected virtual AuditEnvironment GetCurrentAuditEnvironment()
        {
            return AuditEnvironment.GetCurrent();
        }

        private class NullAuditingService : AuditingService
        {
            protected override Task<Uri> SaveAuditRecord(string auditData, string resourceType, string filePath)
            {
                return Task.FromResult<Uri>(new Uri("http://auditing.local/" + resourceType + "/" + filePath));
            }
        }
    }
}
