using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace R2D20B;


/// <summary>
/// Stores and and retrieves the sounds that are available to be played.
/// </summary>
/// <param name="options">The SoundOptions object containing the Sounds folder root path.</param>
internal sealed class SoundCatalog
{
  /// <summary>
  /// The set of all file extensions that are allowed for sound file names.
  /// </summary>
  private static readonly HashSet<string> s_AllowedExtensions =
    new(StringComparer.OrdinalIgnoreCase) { ".wav", ".ogg", ".mp3", };

  /// <summary>
  /// The path to the Sounds folder.
  /// </summary>
  private string RootPath { get; init; }

  /// <summary>
  /// All the sounds stored in the catalog, with extensions intact (base names only, not full
  /// paths). Multiple sounds with the same name but different extensions are permitted.
  /// </summary>
  private HashSet<string> Sounds { get; } = new(StringComparer.OrdinalIgnoreCase);

  private ILogger<SoundCatalog> Logger { get; init; }


  public SoundCatalog(
    IOptions<SoundOptions> options,
    ILogger<SoundCatalog> logger)
  {
    Logger = logger;
    var rootPath = options.Value.RootPath;

    if (!Directory.Exists(rootPath))
      throw new InvalidOperationException(
        $"Invalid sound root path. Value received: {rootPath}");

    RootPath = rootPath;

    PopulateCatalog();
  }


  /// <summary>
  /// Populates the catalog with all the valid sounds found in the Sounds folder.
  /// </summary>
  private void PopulateCatalog()
  {
    var rootPathGroups = Directory.EnumerateFiles(RootPath)
      .Where(HasAllowedExtension)
      .Select(f => (With: Path.GetFileName(f), Without: Path.GetFileNameWithoutExtension(f)))
      .GroupBy(p => p.Without, StringComparer.OrdinalIgnoreCase);
    var sb = new StringBuilder();
    sb.AppendLine($"Adding sounds from {RootPath}:");

    var count = 0;

    foreach (var group in rootPathGroups)
    {
      var groupContents = group.ToList();

      if (groupContents.Count > 1)
      {
        foreach (var (With, _) in groupContents)
        {
          sb.AppendLine($"  - Adding {With}...");
          Sounds.Add(With);
          ++count;
        }
      }
      else
      {
        var without = groupContents[0].Without;
        sb.AppendLine($"  - Adding {without}...");
        Sounds.Add(without);
        ++count;
      }
    }

    if (sb.Length > 0)
      Logger.LogInformation("{LoadedSounds}",
        sb.AppendLine($"- Finished adding {count} sound(s).").ToString());
    else
      Logger.LogInformation($"  - No valid sound files found.");
  }


  /// <summary>
  /// Checks whether the given file name has a valid sound file extension.
  /// </summary>
  /// <param name="fileName">The file name to check.</param>
  /// <returns>True if it has an allowed extension, false otherwise.</returns>
  private static bool HasAllowedExtension(string fileName)
    => s_AllowedExtensions.Contains(Path.GetExtension(fileName));


  /// <summary>
  /// Returns the file Uri pointing to the given sound file name.
  /// </summary>
  /// <param name="name">The sound name to retrieve.</param>
  /// <returns>The Uri to the sound file, or null if it can't be found.</returns>
  public Uri? TryGetSoundUri(string name)
  {
    var path = TryGetSoundPathByName(name);
    if (path is null) return null;

    return new Uri(path, UriKind.Absolute);
  }

  
  /// <summary>
  /// Returns the absolute path to the sound requested by name. Searches by exact string first, and
  /// then if that fails, searches by stripping any extensions on both the input and each entry in
  /// the catalog, and then selects an entry from the matches. Currently it just takes the first one
  /// in the matches container, but I might change that later.
  /// </summary>
  /// <param name="name">The sound name to search for.</param>
  /// <returns>The requested sound path, or null if it can't be found.</returns>
  public string? TryGetSoundPathByName(string name)
  {
    if (string.IsNullOrWhiteSpace(name)) return null;
    if (!IsValidFileName(name)) return null;

    name = name.Trim();

    var soundName = string.Empty;

    if (!Sounds.TryGetValue(name, out soundName))
    {
      name = Path.ChangeExtension(name, null);

      var candidates = Sounds.Where(entry => {
        var key = Path.ChangeExtension(entry, null);
        return string.Equals(name, key, StringComparison.OrdinalIgnoreCase);
      }).ToList();

      if (candidates.Count == 0) return null;
      
      soundName = PickCandidate(candidates, name);
    }

    return Path.Combine(RootPath, soundName);
  }


  /// <summary>
  /// Validates the given string on whether it's a valid file name.
  /// </summary>
  /// <param name="s">The string to validate.</param>
  /// <returns>True if the given string is a valid file name, false otherwise.</returns>
  private static bool IsValidFileName(string s)
    => s.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !(s == "." || s == "..");
  // TODO: Apparently Path.GetInvalidFileNameChars won't solve the whole problem on Linux. Fix this
  // before I migrate.


  /// <summary>
  /// Picks the candidate for the retrieval function to return, from a list of candidates. Currently
  /// just returns the first item in the list.
  /// </summary>
  /// <param name="candidates">The candidate entries that were found by query.</param>
  /// <param name="_">The name of the sound that was searched for. Discarded for now.</param>
  /// <returns>The selected candidate.</returns>
  private static string PickCandidate(IEnumerable<string> candidates, string _)
    => candidates.First();
  // TODO: Apparently HashSets aren't stable, i.e. this won't guarantee the same "first" entry each
  // time we call it with the same inputs. Fix this when I actually want to use multiple sounds
  // that would be passed into this function.


  public IEnumerable<string> GetSortedList()
    => Sounds.ToImmutableSortedSet();
}
