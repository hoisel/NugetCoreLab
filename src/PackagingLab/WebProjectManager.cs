
using NuGet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace PkgLab
{
    internal class WebProjectManager
    {
        // Fields
        private readonly IProjectManager _projectManager;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteSource"></param>
        /// <param name="siteRoot"></param>
        // Methods
        public WebProjectManager(string remoteSource, string siteRoot)
        {

            var sourceRepository = PackageRepositoryFactory.Default.CreateRepository(remoteSource);

            string webRepositoryDirectory = GetWebRepositoryDirectory(siteRoot);
            var pathResolver = new DefaultPackagePathResolver(webRepositoryDirectory);
            var localRepository = PackageRepositoryFactory.Default.CreateRepository(webRepositoryDirectory);
            IProjectSystem project = new WebProjectSystem(siteRoot);
            _projectManager = new ProjectManager(sourceRepository, pathResolver, project, localRepository);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        public IQueryable<IPackage> GetInstalledPackages(string searchTerms)
        {
            return GetPackages(LocalRepository, searchTerms);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <param name="localRepository"></param>
        /// <param name="sourceRepository"></param>
        /// <returns></returns>
        private static IEnumerable<IPackage> GetPackageDependencies(IPackage package, IPackageRepository localRepository, IPackageRepository sourceRepository)
        {
            var walker = new InstallWalker(localRepository,
                                           sourceRepository,
                                           VersionUtility.DefaultTargetFramework,
                                           //new FrameworkName(".NET Framework, Version=4.0"),
                                           NullLogger.Instance,
                                           ignoreDependencies: false,
                                           allowPrereleaseVersions: true,
                                           dependencyVersion: DependencyVersion.Highest);


            return (from operation in walker.ResolveOperations(package)
                    where operation.Action == PackageAction.Install
                    select operation.Package);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        internal static IQueryable<IPackage> GetPackages(IPackageRepository repository, string searchTerm)
        {
            return GetPackages(repository.GetPackages(), searchTerm);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="packages"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        internal static IQueryable<IPackage> GetPackages(IQueryable<IPackage> packages, string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                packages = packages.Find(searchTerm);
            }
            return packages;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        internal IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackage package)
        {
            IPackageRepository localRepository = LocalRepository;
            IPackageRepository sourceRepository = SourceRepository;

            return GetPackagesRequiringLicenseAcceptance(package, localRepository, sourceRepository);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <param name="localRepository"></param>
        /// <param name="sourceRepository"></param>
        /// <returns></returns>
        internal static IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackage package, IPackageRepository localRepository, IPackageRepository sourceRepository)
        {
            return (from p in GetPackageDependencies(package, localRepository, sourceRepository)
                    where p.RequireLicenseAcceptance
                    select p);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        public IQueryable<IPackage> GetPackagesWithUpdates(string searchTerms)
        {
            return GetPackages(LocalRepository.GetUpdates(SourceRepository.GetPackages(),
                                                          includePrerelease: true,
                                                          includeAllVersions: true).AsQueryable(), searchTerms);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        public IQueryable<IPackage> GetRemotePackages(string searchTerms)
        {
            return GetPackages(SourceRepository, searchTerms);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public IPackage GetUpdate(IPackage package)
        {
            var update = SourceRepository.GetUpdates(LocalRepository.GetPackages(),
                                               includePrerelease: true,
                                               includeAllVersions: true)
                                         .OrderByDescending(p => p.Version.Version)  // todo: eu que coloquei pra pegar ultima versao. Manter?
                                         .FirstOrDefault(p => (package.Id == p.Id));

            return update;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteRoot"></param>
        /// <returns></returns>
        internal static string GetWebRepositoryDirectory(string siteRoot)
        {
            return Path.Combine(siteRoot, "App_Data", "packages");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public IEnumerable<string> InstallPackage(IPackage package)
        {
            return PerformLoggedAction(
                () => _projectManager.AddPackageReference(package,
                                                            ignoreDependencies: false,
                                                            allowPrereleaseVersions: true));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public bool IsPackageInstalled(IPackage package)
        {
            return LocalRepository.Exists(package);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private IEnumerable<string> PerformLoggedAction(Action action)
        {
            var logger = new ErrorLogger();
            _projectManager.Logger = logger;
            try
            {
                action();
            }
            finally
            {
                _projectManager.Logger = null;
            }
            return logger.Errors;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <param name="removeDependencies"></param>
        /// <returns></returns>
        public IEnumerable<string> UninstallPackage(IPackage package, bool removeDependencies)
        {
            return PerformLoggedAction(
                () =>
                    _projectManager.RemovePackageReference(package.Id,
                        forceRemove: false,
                        removeDependencies: removeDependencies));
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public IEnumerable<string> UpdatePackage(IPackage package)
        {
            return PerformLoggedAction(
                () =>
                    _projectManager.UpdatePackageReference(
                        package.Id,
                        package.Version,
                        updateDependencies: false,
                        allowPrereleaseVersions: false));
        }


        /// <summary>
        /// 
        /// </summary>
        // Properties
        public IPackageRepository LocalRepository
        {
            get
            {
                return _projectManager.LocalRepository;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public IPackageRepository SourceRepository
        {
            get
            {
                return _projectManager.SourceRepository;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        // Nested Types
        private class ErrorLogger : ILogger
        {
            // Fields
            private readonly IList<string> _errors = new List<string>();

            // Methods
            public void Log(MessageLevel level, string message, params object[] args)
            {
                if (level == MessageLevel.Warning)
                {
                    _errors.Add(string.Format(CultureInfo.CurrentCulture, message, args));
                }
            }

            // Properties
            public IEnumerable<string> Errors
            {
                get
                {
                    return _errors;
                }
            }

            public FileConflictResolution ResolveFileConflict(string message)
            {
                // TODO: Whatever I'm supposed to do here.
                throw new NotImplementedException();
            }
        }
    }
}