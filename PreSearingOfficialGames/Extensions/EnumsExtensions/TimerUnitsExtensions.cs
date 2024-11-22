using PreSearingOfficialGames.Helpers;

namespace PreSearingOfficialGames.Extensions.EnumsExtensions
{
    public static class TimerUnitsExtensions
    {
        public static string NameToString(this TimerUnits timerUnits)
        {
            return timerUnits switch
            {
                TimerUnits.m 
                    or TimerUnits.min 
                    or TimerUnits.mins 
                    or TimerUnits.minute 
                    or TimerUnits.minutes 
                        => "mins",

                TimerUnits.none 
                    or TimerUnits.h 
                    or TimerUnits.hr 
                    or TimerUnits.hrs 
                    or TimerUnits.hour 
                    or TimerUnits.hours 
                        => "hrs",

                _ => throw new InvalidOperationException($"No existing unit to convert to string"),
            };
        }
    }
}
