namespace R2D20B;


internal sealed class EmojiCatalog
{
  public static string Clean { get; } = "<a:clean:1455055703038234768>";
  public static string Cleanish { get; } = "<a:cleanish:1455055704606900356>";
  public static string NotClean { get; } = "<:notclean:1455055705995350038>";

  public static string Dance0 { get; } = "<a:0dance:1455032151539060791>";
  public static string Dance1 { get; } = "<a:1dance:1455032152688427150>";
  public static string Dance2 { get; } = "<a:2dance:1455032153778819113>";
  public static string Dance3 { get; } = "<a:3dance:1455032154999226498>";
  public static string Dance4 { get; } = "<a:4dance:1455032156857434162>";
  public static string Dance5 { get; } = "<a:5dance:1455032157968793812>";
  public static string Dance6 { get; } = "<a:6dance:1455032158866641015>";
  public static string Dance7 { get; } = "<a:7dance:1455032160070140041>";
  public static string Dance8 { get; } = "<a:8dance:1455032160967852160>";
  public static string Dance9 { get; } = "<a:9dance:1455032162666680373>";

  public static string DanceAnd { get; } = "<a:ANDdance:1455032167259312321>";
  public static string DanceAt { get; } = "<a:ATdance:1455032168261619819>";
  public static string DanceDollar { get; } = "<a:DOLdance:1455032172938531029>";
  public static string DanceExclamation { get; } = "<a:EXCLdance:1455032175299661950>";
  public static string DanceQuestion { get; } = "<a:QUESdance:1455032191724683374>";

  public static string DanceA { get; } = "<a:Adance:1455032165749358612>";
  public static string DanceB { get; } = "<a:Bdance:1455032169612181619>";
  public static string DanceC { get; } = "<a:Cdance:1455032170845573171>";
  public static string DanceD { get; } = "<a:Ddance:1455032171898081424>";
  public static string DanceE { get; } = "<a:Edance:1455032173789712454>";
  public static string DanceF { get; } = "<a:Fdance:1455032177346609330>";
  public static string DanceG { get; } = "<a:Gdance:1455032178751705311>";
  public static string DanceH { get; } = "<a:Hdance:1455032179535904962>";
  public static string DanceI { get; } = "<a:Idance:1455032180492210198>";
  public static string DanceJ { get; } = "<a:Jdance:1455032181662421149>";
  public static string DanceK { get; } = "<a:Kdance:1455032182618853469>";
  public static string DanceL { get; } = "<a:Ldance:1455032184199970897>";
  public static string DanceM { get; } = "<a:Mdance:1455032185273843815>";
  public static string DanceN { get; } = "<a:Ndance:1455032186855096619>";
  public static string DanceO { get; } = "<a:Odance:1455032188297810021>";
  public static string DanceP { get; } = "<a:Pdance:1455032189703032978>";
  public static string DanceQ { get; } = "<a:Qdance:1455032190814392482>";
  public static string DanceR { get; } = "<a:Rdance:1455032192999620628>";
  public static string DanceS { get; } = "<a:Sdance:1455032194144669790>";
  public static string DanceT { get; } = "<a:Tdance:1455032195063484507>";
  public static string DanceU { get; } = "<a:Udance:1455032196195946604>";
  public static string DanceV { get; } = "<a:Vdance:1455032196950917255>";
  public static string DanceW { get; } = "<a:Wdance:1455032198225989786>";
  public static string DanceX { get; } = "<a:Xdance:1455032200641908950>";
  public static string DanceY { get; } = "<a:Ydance:1455032201635823808>";
  public static string DanceZ { get; } = "<a:Zdance:1455032202533540108>";

  public static Dictionary<char, string> DanceEmoji { get; } = new()
  {
    { ' ', "    " },

    { '0', Dance0 },
    { '1', Dance1 },
    { '2', Dance2 },
    { '3', Dance3 },
    { '4', Dance4 },
    { '5', Dance5 },
    { '6', Dance6 },
    { '7', Dance7 },
    { '8', Dance8 },
    { '9', Dance9 },

    { '&', DanceAnd },
    { '@', DanceAt },
    { '$', DanceDollar },
    { '!', DanceExclamation },
    { '?', DanceQuestion },

    { 'A', DanceA },
    { 'B', DanceB },
    { 'C', DanceC },
    { 'D', DanceD },
    { 'E', DanceE },
    { 'F', DanceF },
    { 'G', DanceG },
    { 'H', DanceH },
    { 'I', DanceI },
    { 'J', DanceJ },
    { 'K', DanceK },
    { 'L', DanceL },
    { 'M', DanceM },
    { 'N', DanceN },
    { 'O', DanceO },
    { 'P', DanceP },
    { 'Q', DanceQ },
    { 'R', DanceR },
    { 'S', DanceS },
    { 'T', DanceT },
    { 'U', DanceU },
    { 'V', DanceV },
    { 'W', DanceW },
    { 'X', DanceX },
    { 'Y', DanceY },
    { 'Z', DanceZ },
  };
}
