﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using MrCMS.Helpers;
using MrCMS.Indexing.Management;

namespace MrCMS.Indexing.Utils
{
    /// <summary>
    /// Extension methods for creating Lucene documents
    /// </summary>
    public static class DocumentExtensions
    {
        public static Document SetFields<T>(this Document document, IEnumerable<FieldDefinition<T>> definitions, T obj)
        {
            definitions.ForEach(definition => document.AddField(definition, obj));
            return document;
        }

        /// <summary>
        /// Allows method chaining of add field
        /// </summary>
        /// <param name="document">Document to add field to</param>
        /// <param name="field">Field to add</param>
        /// <returns>Document</returns>
        public static Document AddField(this Document document, IIndexableField field)
        {
            if (document != null)
                document.Add(field);
            return document;
        }

        public static async Task<Document> AddField<T>(this Document document, FieldDefinition<T> definition, T obj)
        {
            if (document != null)
            {
                var fields = await definition.GetFields(obj);
                fields.ForEach(document.Add);
            }
            return document;
        }

        public static string GetValue(this Document document, string field)
        {
            return GetValue<string>(document, field);
        }

        public static T GetValue<T>(this Document document, string field)
        {
            var result = default(T);

            var values = document.GetValues(field);
            if (values.Any())
                result = values[0].To<T>();

            return result;
        }

        public static bool Exists(this Document document, string field, string value)
        {
            return document.GetValues(field).Any(v => v.Equals(value));
        }
    }
}