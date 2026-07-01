using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Serilog;

sealed partial class Build : NukeBuild
{
    static readonly Regex versionRegex = CreateVersionRegex();
    static readonly AbsolutePath mainProjectFile = RootDirectory / "src" / "WTorricos.Either" / "WTorricos.Either.csproj";
    static readonly AbsolutePath changelogFile = RootDirectory / "CHANGELOG.md";
    static readonly string[] meaningfulChangePaths =
    [
        "src/WTorricos.Either",
        "Directory.Build.props",
        "Directory.Packages.props",
        "global.json"
    ];
    const string ChangelogSectionPattern = "## [";

    [Parameter(description: "Emit preview package version (x.y.z-preview.1) instead of stable (x.y.z).")]
    readonly bool preview;

    [Parameter(description: "Date for CHANGELOG entry in yyyy-MM-dd format. Defaults to today.")]
    readonly string changelogDate;

    // dotnet nuke BumpReleaseVersion --preview
    Target BumpReleaseVersion => _ => _
        .Executes(() =>
        {
            EnsureMeaningfulMainProjectChangesExist();

            string csprojContent = File.ReadAllText(mainProjectFile);
            string changelogContent = File.ReadAllText(changelogFile);

            VersionInfo currentVersion = ReadProjectVersion(csprojContent);
            VersionInfo targetVersion = currentVersion.BumpPatch(preview);

            string releaseNotes = $"Release {targetVersion}. See CHANGELOG.md for details.";
            string updatedCsprojContent = UpdateProjectMetadata(
                csprojContent,
                targetVersion.ToString(),
                releaseNotes);
            string updatedChangelogContent = InsertChangelogSection(
                changelogContent,
                targetVersion.ToString(),
                ResolveChangelogDate(changelogDate));

            WriteFilesAtomically(
                (mainProjectFile, updatedCsprojContent),
                (changelogFile, updatedChangelogContent));

            Log.Information($"Version bumped from {currentVersion} to {targetVersion}.");
        });

    [GeneratedRegex(
        @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-preview\.(?<preview>\d+))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex CreateVersionRegex();

    static VersionInfo ReadProjectVersion(string csprojContent)
    {
        XDocument document = XDocument.Parse(csprojContent, LoadOptions.PreserveWhitespace);
        XElement versionElement = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Version")
            ?? throw new InvalidOperationException($"Missing <Version> element in {mainProjectFile}.");

        string rawVersion = versionElement.Value.Trim();
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            throw new InvalidOperationException($"<Version> in {mainProjectFile} cannot be empty.");
        }

        Match match = versionRegex.Match(rawVersion);
        _ = match.Success
            ? true
            : throw new InvalidOperationException(
                $"Invalid version '{rawVersion}'. Expected x.y.z or x.y.z-preview.n.");

        return new VersionInfo(
            int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture));
    }

    static string UpdateProjectMetadata(string csprojContent, string targetVersion, string packageReleaseNotes)
    {
        XDocument document = XDocument.Parse(csprojContent, LoadOptions.PreserveWhitespace);
        XElement versionElement = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Version")
            ?? throw new InvalidOperationException($"Missing <Version> element in {mainProjectFile}.");
        XElement releaseNotesElement = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "PackageReleaseNotes")
            ?? throw new InvalidOperationException($"Missing <PackageReleaseNotes> element in {mainProjectFile}.");

        versionElement.Value = targetVersion;
        releaseNotesElement.Value = packageReleaseNotes;

        return document.ToString(SaveOptions.DisableFormatting);
    }

    static string InsertChangelogSection(string changelogContent, string targetVersion, string changelogDate)
    {
        int insertionIndex = changelogContent.IndexOf(ChangelogSectionPattern, StringComparison.Ordinal);
        if (insertionIndex < 0)
        {
            throw new InvalidOperationException(
                $"Missing changelog insertion point '{ChangelogSectionPattern}' in {changelogFile}.");
        }

        string newSection =
            $"## [{targetVersion}] - {changelogDate}{Environment.NewLine}" +
            $"### Added{Environment.NewLine}" +
            $"- TODO: describe changes.{Environment.NewLine}{Environment.NewLine}";

        return changelogContent.Insert(insertionIndex, newSection);
    }

    static string ResolveChangelogDate(string inputDate)
    {
        if (string.IsNullOrWhiteSpace(inputDate))
        {
            return DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        bool isValid = DateTime.TryParseExact(
            inputDate.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate);

        _ = isValid
            ? true
            : throw new InvalidOperationException(
                $"Invalid ChangelogDate '{inputDate}'. Expected format yyyy-MM-dd.");

        return parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    static void EnsureMeaningfulMainProjectChangesExist()
    {
        string pathspecArgs = string.Join(" ", meaningfulChangePaths);
        IReadOnlyCollection<Output> output = ProcessTasks
            .StartProcess(
                toolPath: "git",
                arguments: $"--no-pager status --porcelain -- {pathspecArgs}",
                workingDirectory: RootDirectory)
            .AssertZeroExitCode()
            .Output;

        bool hasMeaningfulChanges = output.Any(line => !string.IsNullOrWhiteSpace(line.Text));

        if (!hasMeaningfulChanges)
        {
            throw new InvalidOperationException(
                "Version bump blocked: no meaningful changes detected in src/WTorricos.Either/**, Directory.Build.props, Directory.Packages.props, or global.json.");
        }
    }

    static void WriteFilesAtomically(params (AbsolutePath Path, string Content)[] files)
    {
        List<PendingFileWrite> pendingWrites =
        [
            .. files.Select(
                file => new PendingFileWrite(
                    file.Path,
                    file.Content,
                    file.Path + ".tmp",
                    file.Path + ".bak"))
        ];

        foreach (PendingFileWrite pending in pendingWrites)
        {
            File.WriteAllText(pending.TempPath, pending.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        List<PendingFileWrite> replacedFiles = [];
        try
        {
            foreach (PendingFileWrite pending in pendingWrites)
            {
                File.Replace(pending.TempPath, pending.Path, pending.BackupPath, ignoreMetadataErrors: true);
                replacedFiles.Add(pending);
            }
        }
        catch (IOException ex)
        {
            RollbackReplacedFiles(replacedFiles);
            throw new InvalidOperationException("Failed to update release files atomically.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            RollbackReplacedFiles(replacedFiles);
            throw new InvalidOperationException("Failed to update release files atomically.", ex);
        }
        finally
        {
            foreach (PendingFileWrite pending in pendingWrites)
            {
                if (File.Exists(pending.TempPath))
                {
                    File.Delete(pending.TempPath);
                }
            }
        }

        foreach (PendingFileWrite pending in pendingWrites)
        {
            if (File.Exists(pending.BackupPath))
            {
                File.Delete(pending.BackupPath);
            }
        }
    }

    static void RollbackReplacedFiles(IEnumerable<PendingFileWrite> replacedFiles)
    {
        foreach (PendingFileWrite pending in replacedFiles.Reverse())
        {
            if (File.Exists(pending.BackupPath))
            {
                File.Replace(pending.BackupPath, pending.Path, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
        }
    }

    sealed record VersionInfo(int Major, int Minor, int Patch)
    {
        public VersionInfo BumpPatch(bool preview) =>
            preview
                ? new VersionInfo(Major, Minor, Patch + 1) { IsPreview = true }
                : new VersionInfo(Major, Minor, Patch + 1) { IsPreview = false };

        bool IsPreview { get; init; }

        public override string ToString() =>
            IsPreview
                ? $"{Major}.{Minor}.{Patch}-preview.1"
                : $"{Major}.{Minor}.{Patch}";
    }

    sealed record PendingFileWrite(AbsolutePath Path, string Content, string TempPath, string BackupPath);
}
