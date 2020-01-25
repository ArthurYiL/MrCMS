﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.EntityFrameworkCore;
using MrCMS.Data;
using MrCMS.Entities;
using MrCMS.Entities.Multisite;
using MrCMS.Helpers;
using MrCMS.Search;
using MrCMS.Website;

namespace MrCMS.Indexing.Management
{
    public class IndexManager<TEntity, TDefinition> : IIndexManager<TEntity, TDefinition>
        where TEntity : SystemEntity
        where TDefinition : IndexDefinition<TEntity>
    {
        private readonly IGetLuceneIndexWriter _getLuceneIndexWriter;
        private readonly IGetLuceneIndexSearcher _getLuceneIndexSearcher;
        private readonly IGetSiteId _getSiteId;
        private readonly TDefinition _definition;
        private readonly IRepositoryResolver _repositoryResolver;
        private readonly IGetLuceneDirectory _getLuceneDirectory;
        private readonly IGetIndexResult _getIndexResult;

        public IndexManager(IGetLuceneIndexWriter getLuceneIndexWriter, IGetLuceneIndexSearcher getLuceneIndexSearcher,
            IGetSiteId getSiteId, TDefinition definition, IRepositoryResolver repositoryResolver, IGetLuceneDirectory getLuceneDirectory,
            IGetIndexResult getIndexResult)
        {
            _getLuceneIndexWriter = getLuceneIndexWriter;
            _getLuceneIndexSearcher = getLuceneIndexSearcher;
            _getSiteId = getSiteId;
            _definition = definition;
            _repositoryResolver = repositoryResolver;
            _getLuceneDirectory = getLuceneDirectory;
            _getIndexResult = getIndexResult;
        }

        public string IndexFolderName
        {
            get { return Definition.IndexFolderName; }
        }

        public TDefinition Definition
        {
            get { return _definition; }
        }

        public bool IndexExists
        {
            get { return DirectoryReader.IndexExists(GetDirectory(_getSiteId.GetId())); }
        }

        public int? NumberOfDocs
        {
            get
            {
                if (!IndexExists)
                    return null;

                using (var indexReader = DirectoryReader.Open(GetDirectory(_getSiteId.GetId())))
                {
                    return indexReader.NumDocs;
                }
            }
        }

        public string IndexName
        {
            get { return Definition.IndexName; }
        }

        IndexDefinition IIndexManagerBase.Definition => Definition;

        public IndexCreationResult CreateIndex()
        {
            Directory fsDirectory = GetDirectory(_getSiteId.GetId());
            bool indexExists = DirectoryReader.IndexExists(fsDirectory);
            if (indexExists)
                return IndexCreationResult.AlreadyExists;
            try
            {
                Write(writer => { }, true);
                return IndexCreationResult.Success;
            }
            catch
            {
                return IndexCreationResult.Failure;
            }
        }

        public Type GetIndexDefinitionType()
        {
            return typeof(TDefinition);
        }

        public Type GetEntityType()
        {
            return typeof(TEntity);
        }

        public void Write(Action<IndexWriter> action)
        {
            Write(action, false);
        }

        public IndexResult Insert(IEnumerable<TEntity> entities)
        {
            return _getIndexResult.GetResult(() => Write(writer =>
            {
                foreach (TEntity entity in entities)
                    writer.AddDocument(Definition.Convert(entity));
            }));
        }

        public IndexResult Insert(TEntity entity)
        {
            return _getIndexResult.GetResult(() => Write(writer => writer.AddDocument(Definition.Convert(entity))));
        }

        public IndexResult Insert(object entity)
        {
            if (entity is TEntity)
                return Insert(entity as TEntity);

            return _getIndexResult.GetResult(() =>
            {
                throw new Exception(
                    string.Format(
                        "object {0} is not of correct type for the index {1}",
                        entity,
                        GetType().Name));
            });
        }

        public IndexResult Delete(object entity)
        {
            if (entity is TEntity)
                return Delete(entity as TEntity);

            return _getIndexResult.GetResult(() =>
            {
                throw new Exception(
                    string.Format(
                        "object {0} is not of correct type for the index {1}",
                        entity,
                        GetType().Name));
            });
        }

        public void ResetSearcher()
        {
            _getLuceneIndexSearcher.Reset(Definition);
        }

        public IndexResult Update(IEnumerable<TEntity> entities)
        {
            return _getIndexResult.GetResult(() => Write(writer =>
            {
                foreach (TEntity entity in entities)
                    writer.UpdateDocument(Definition.GetIndex(entity),
                        Definition.Convert(entity));
            }));
        }

        public IndexResult Update(TEntity entity)
        {
            return _getIndexResult.GetResult(() => Write(writer =>
            {
                var indexSearcher = _getLuceneIndexSearcher.Get(Definition);

                TopDocs topDocs = indexSearcher.Search(new TermQuery(Definition.GetIndex(entity)), int.MaxValue);
                if (!topDocs.ScoreDocs.Any())
                    return;

                writer.UpdateDocument(Definition.GetIndex(entity),
                    Definition.Convert(entity));
            }));
        }

        public IndexResult Update(object entity)
        {
            if (entity is TEntity)
                return Update(entity as TEntity);

            return _getIndexResult.GetResult(() => throw new Exception(
                string.Format(
                    "object {0} is not of correct type for the index {1}", entity, GetType().Name)));
        }

        public IndexResult Delete(IEnumerable<TEntity> entities)
        {
            return _getIndexResult.GetResult(() => Write(writer =>
            {
                foreach (TEntity entity in entities)
                    writer.DeleteDocuments(Definition.GetIndex(entity));
            }));
        }

        public IndexResult Delete(TEntity entity)
        {
            return _getIndexResult.GetResult(() => Write(writer => writer.DeleteDocuments(Definition.GetIndex(entity))));
        }

        public async Task<IndexResult> ReIndex()
        {
            var thisType = typeof(IndexManager<,>);
            var entities = new List<TEntity>();
            if (typeof(SiteEntity).IsAssignableFrom(typeof(TEntity)))
            {
                entities.AddRange(await LoadAllOfType());
            }
            else
            {
                entities.AddRange(await GlobalLoadAllOfType());
            }

            return _getIndexResult.GetResult(() => Write(writer =>
            {
                foreach (Document document in Definition.ConvertAll(entities))
                    writer.AddDocument(document);
            }, true));
        }

        public Task<List<TEntity>> LoadAllOfType()
        {
            var siteId = _getSiteId.GetId();
            return _repositoryResolver.GetGlobalRepository<TEntity>().Readonly()
                .Where(x => EF.Property<int>(x, nameof(IHaveSite.SiteId)) == siteId)
                .ToListAsync();
        }
        public Task<List<TEntity>> GlobalLoadAllOfType()
        {
            return _repositoryResolver.GetGlobalRepository<TEntity>().Readonly().ToListAsync();
        }

        public Document GetDocument(object entity)
        {
            return Definition.Convert(entity as TEntity);
        }

        private Directory GetDirectory(int siteId)
        {
            return _getLuceneDirectory.Get(siteId, IndexFolderName);
        }

        private void Write(Action<IndexWriter> writeFunc, bool recreateIndex)
        {
            if (recreateIndex)
                _getLuceneIndexWriter.RecreateIndex(Definition);
            using (var indexWriter = _getLuceneIndexWriter.Get(Definition))
            {
                writeFunc(indexWriter);
                indexWriter.Commit();
            }
            _getLuceneIndexSearcher.Reset(Definition);
        }
    }
}