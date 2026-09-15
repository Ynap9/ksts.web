using kssm.be.external.KySo.Fonts.Interfaces;
using kssm.be.shared.Constants.Signing;
using PdfSharp.Fonts;
using System.Collections.Concurrent;

namespace kssm.be.external.KySo.Fonts.Implements
{
    public class AppearanceFontResolver : IAppearanceFontResolver
    {
        private static readonly EnumerationOptions FileSearchOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        private readonly ConcurrentDictionary<string, string?> _pathByStyle = new();
        private readonly ConcurrentDictionary<string, byte[]> _bytesByPath = new();

        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
        {
            var path = FindFontFile(bold, italic);
            if (path != null)
            {
                return new FontResolverInfo(path);
            }

            var regular = FindFontFile(false, false);
            return regular == null ? null : new FontResolverInfo(regular, bold, italic);
        }

        public byte[]? GetFont(string faceName) => _bytesByPath.GetOrAdd(faceName, File.ReadAllBytes);

        public string? FindFontFile(bool bold, bool italic) =>
            _pathByStyle.GetOrAdd($"{bold}|{italic}", _ => Locate(GetCandidateFiles(bold, italic)));

        public IReadOnlyList<string> GetCandidateFiles(bool bold, bool italic)
        {
            if (bold && italic)
            {
                return AppearanceFontConstants.BoldItalicFiles;
            }

            if (bold)
            {
                return AppearanceFontConstants.BoldFiles;
            }

            return italic ? AppearanceFontConstants.ItalicFiles : AppearanceFontConstants.RegularFiles;
        }

        public string? Locate(IReadOnlyList<string> fileNames)
        {
            foreach (var directory in GetSearchDirectories())
            {
                foreach (var fileName in fileNames)
                {
                    var found = Directory
                        .EnumerateFiles(directory, fileName, FileSearchOptions)
                        .FirstOrDefault();

                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        public IEnumerable<string> GetSearchDirectories()
        {
            var directories = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.Fonts) };
            directories.AddRange(AppearanceFontConstants.SearchDirectories);

            return directories.Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path));
        }
    }
}
