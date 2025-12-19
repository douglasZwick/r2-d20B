using System.Text;

namespace R2D20B;


internal class UptimeService
{
  public DateTimeOffset StartTime { get; init; }
  public TimeSpan Uptime =>
    DateTimeOffset.UtcNow - StartTime;
  public string UptimeFormatted
  {
    get
    {
      var uptime = Uptime;
      var days = uptime.Days;
      var hours = uptime.Hours;
      var minutes = uptime.Minutes;
      var seconds = uptime.Seconds;
      
      var outputSb = new StringBuilder();
      var commaNeeded = false;
      var allZero = true;

      if (days > 0)
      {
        var label = days == 1 ? "day" : "days";
        outputSb.Append($"{days} {label}");
        commaNeeded = true;
        allZero = false;
      }
      if (hours > 0)
      {
        if (commaNeeded) outputSb.Append(", ");

        var label = hours == 1 ? "hour" : "hours";
        outputSb.Append($"{hours} {label}");
        commaNeeded = true;
        allZero = false;
      }
      if (minutes > 0)
      {
        if (commaNeeded) outputSb.Append(", ");
        
        var label = minutes == 1 ? "minute" : "minutes";
        outputSb.Append($"{minutes} {label}");
        commaNeeded = true;
        allZero = false;
      }
      if (seconds > 0)
      {
        if (commaNeeded) outputSb.Append(", ");

        var label = seconds == 1 ? "second" : "seconds";
        outputSb.Append($"{seconds} {label}");
        allZero = false;
      }
      
      if (allZero)
      {
        outputSb.Append("less than one second");
      }

      return outputSb.ToString();
    }
  }


  public UptimeService()
  {
    StartTime = DateTimeOffset.UtcNow;
  }
}
