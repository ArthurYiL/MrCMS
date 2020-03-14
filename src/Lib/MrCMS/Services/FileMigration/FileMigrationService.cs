using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MrCMS.Batching;
using MrCMS.Batching.Services;
using MrCMS.Data;
using MrCMS.Entities.Documents.Media;
using MrCMS.Helpers;
using MrCMS.Settings;
using Newtonsoft.Json;

namespace MrCMS.Services.FileMigration
{
    public class FileMigrationService : IFileMigrationService
    {
        private readonly Dictionary<string, IFileSystem> _allFileSystems;
        private readonly ICreateBatch _createBatch;
        private readonly IRepository<MediaFile> _mediaFileRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IUrlHelper _urlHelper;

        public FileMigrationService(IServiceProvider serviceProvider, IConfigurationProvider configurationProvider, IRepository<MediaFile> mediaFileRepository,
            ICreateBatch createBatch, IUrlHelper urlHelper)
        {
            IEnumerable<IFileSystem> fileSystems = TypeHelper.GetAllConcreteTypesAssignableFrom<IFileSystem>()
                .Select(type => serviceProvider.GetService(type) as IFileSystem);
            _allFileSystems =
                fileSystems
                    .ToDictionary(system => system.GetType().FullName);
            _mediaFileRepository = mediaFileRepository;
            _createBatch = createBatch;
            _serviceProvider = serviceProvider;
            _configurationProvider = configurationProvider;
            _urlHelper = urlHelper;
        }

        public async Task<IFileSystem> GetCurrentFileSystem()
        {
            var _fileSystemSettings = await _configurationProvider.GetSiteSettings<FileSystemSettings>();
            string storageType = _fileSystemSettings.StorageType;
            return _allFileSystems[storageType];
        }

        public async Task<FileMigrationResult> MigrateFiles()
        {
            IList<MediaFile> mediaFiles = await _mediaFileRepository.Readonly().ToListAsync();
            var currentFileSystem = await GetCurrentFileSystem();
            List<Guid> guids =
                mediaFiles.Where(
                    mediaFile => MediaFileExtensions.GetFileSystem(mediaFile, _allFileSystems.Values) !=
                                 currentFileSystem)
                    .Select(file => file.Guid).ToList();

            if (!guids.Any())
            {
                return new FileMigrationResult
                {
                    MigrationRequired = false,
                    Message = "Migration not required"
                };
            }

            BatchCreationResult result = await _createBatch.Create(guids.Chunk(10)
                .Select(set => new MigrateFilesBatchJob
                {
                    Data = JsonConvert.SerializeObject(set.ToHashSet()),
                }));

            return new FileMigrationResult
            {
                MigrationRequired = true,
                Message = string.Format(
                    "Batch created. Click <a target=\"_blank\" href=\"{0}\">here</a> to view and start.".AsResource(
                        _serviceProvider),
                    _urlHelper.Action("Show", "BatchRun", new { id = result.InitialBatchRun.Id }))
            };
        }
    }

    public class FileMigrationResult
    {
        public bool MigrationRequired { get; set; }
        public string Message { get; set; }
    }
}