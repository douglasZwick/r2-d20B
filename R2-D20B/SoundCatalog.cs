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
  private static readonly HashSet<string> s_HiddenPrefixes = 
    new(StringComparer.OrdinalIgnoreCase) { "secret.", "music.", "r2.", };

  /// <summary>
  /// The path to the Sounds folder.
  /// </summary>
  private string RootPath { get; init; }

  /// <summary>
  /// All the sounds stored in the catalog, with extensions intact (base names only, not full
  /// paths). Multiple sounds with the same name but different extensions are permitted.
  /// </summary>
  private HashSet<string> Sounds { get; } = 
    new(StringComparer.OrdinalIgnoreCase);
  private Dictionary<string, List<string>> SoundsByBaseName { get; } =
    new(StringComparer.OrdinalIgnoreCase);

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
    var rootPaths = Directory.EnumerateFiles(RootPath)
      .Where(HasAllowedExtension)
      .Select(f => (BaseName: Path.GetFileNameWithoutExtension(f), FullName: Path.GetFileName(f)));
    
    var sb = new StringBuilder();
    sb.AppendLine($"Adding sounds from {RootPath}:");

    var count = 0;

    foreach (var (BaseName, FullName) in rootPaths)
    {
      sb.AppendLine($"  - Adding {FullName}...");
      Sounds.Add(FullName);
      
      if (SoundsByBaseName.TryGetValue(BaseName, out var list))
        list.Add(FullName);
      else
        SoundsByBaseName.Add(BaseName, [FullName]);

      ++count;
    }

    if (count > 0)
      Logger.LogInformation("{LoadedSounds}",
        sb.AppendLine($"- Finished adding {count} sound(s).").ToString());
    else
      Logger.LogInformation($"  - No valid sound files found.");
  }


  /// <summary>
  /// Checks whether the given path has a valid sound file extension.
  /// </summary>
  /// <param name="path">The path to check.</param>
  /// <returns>True if it has an allowed extension, false otherwise.</returns>
  private static bool HasAllowedExtension(string path)
    => s_AllowedExtensions.Contains(Path.GetExtension(path));


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
  /// Returns the absolute path to the sound requested by name.
  /// </summary>
  /// <param name="name">The sound name to search for.</param>
  /// <returns>The requested sound path, or null if it can't be found.</returns>
  public string? TryGetSoundPathByName(string name)
  {
    name = name.Trim();

    if (name.Length <= 0) return null;
    if (!IsValidFileName(name)) return null;

    var fileName = TryGetSoundFileByName(name);
    if (fileName is null) return null;

    return Path.Combine(RootPath, fileName);
  }


  /// <summary>
  /// Returns the file name for the sound requested by name.
  /// </summary>
  /// <param name="name">The sound name to search for.</param>
  /// <returns>The requested file name, or null if it can't be found.</returns>
  private string? TryGetSoundFileByName(string name)
  {
    if (Sounds.Contains(name)) return name;
    if (Path.HasExtension(name)) return null;

    if (SoundsByBaseName.TryGetValue(name, out var variants))
      return PickCandidate([.. variants.OrderBy(v => v, StringComparer.OrdinalIgnoreCase)], name);

    return null;
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
  private static string PickCandidate(List<string> candidates, string _)
    => candidates[0];


  /// <summary>
  /// Gets a sorted list of names of the sounds in the catalog, filtered to exclude any secrets.
  /// </summary>
  /// <returns>The sorted list.</returns>
  public List<string> GetSortedList()
  {
    var output = new List<string>();

    var sortedDictionary = SoundsByBaseName
      .Where(kvp => !ShouldBeHidden(kvp.Key))
      .ToImmutableSortedDictionary(StringComparer.OrdinalIgnoreCase);
    
    foreach (var kvp in sortedDictionary)
    {
      var variants = kvp.Value;

      if (variants.Count <= 1)
      {
        output.Add(kvp.Key);
        continue;
      }

      var sortedVariants = variants.OrderBy(v => v, StringComparer.OrdinalIgnoreCase);

      foreach (var variant in sortedVariants)
        output.Add(variant);
    }

    return output;
  }


  /// <summary>
  /// Returns whether the input string starts with a hidden prefix.
  /// </summary>
  /// <param name="input">The string to check</param>
  /// <returns>True if it do, false if it don't.</returns>
  private static bool ShouldBeHidden(string input)
    => s_HiddenPrefixes.Any(p => input.StartsWith(p, StringComparison.OrdinalIgnoreCase));
}
