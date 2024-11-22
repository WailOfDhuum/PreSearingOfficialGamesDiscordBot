using PreSearingOfficialGames.Helpers;

namespace PreSearingOfficialGames.Games
{
    public class GameTimer
    {
        private const int _maxHoursExt = 48;

        public TimeSpan Value { get; }
        public int RawValue { get; }
        public TimerUnits Units { get; }


        private GameTimer(TimeSpan value, int rawValue, TimerUnits units)
        {
            Value = value;
            RawValue = rawValue;
            Units = units;
        }

        public static GameTimer GetDefaultGameTimer()
            => new(TimeSpan.FromHours(24), 24, TimerUnits.h);

        public static ValidationResult<GameTimer?> GetGameTimerIfValid(int rawValue, TimerUnits units)
        {
            if (rawValue == 0)
                return ValidationResult<GameTimer?>.Success(new GameTimer(TimeSpan.Zero, 0, TimerUnits.none));

            var validation = IsTimerValueValid(rawValue, units);
            if (validation.IsError)
                return ValidationResult<GameTimer?>.Failure(validation.ErrorMessage);


            var value = GetNewTimeSpan(rawValue, units);

            return ValidationResult<GameTimer?>.Success(new GameTimer(value, rawValue, units));
        }

        private static ValidationResult IsTimerValueValid(int timerValue, TimerUnits timerUnits)
        {
            if (timerValue < 0)
                return ValidationResult.Failure("Fool, use positive numeric values for the new value of timer!");

            var minutes = new[]
            {
                TimerUnits.m,
                TimerUnits.min,
                TimerUnits.mins,
                TimerUnits.minute,
                TimerUnits.minutes
            };

            var hours = new[]
{
                TimerUnits.none,
                TimerUnits.h,
                TimerUnits.hr,
                TimerUnits.hrs,
                TimerUnits.hour,
                TimerUnits.hours
            };

            //48h
            if (timerValue > _maxHoursExt * 60 && minutes.Contains(timerUnits)
                || timerValue > _maxHoursExt && hours.Contains(timerUnits))
            {
                return ValidationResult.Failure("No one will want to wait that long for the game to end, " +
                    "I won't allow it!");
            }

            return ValidationResult.Success();
        }

        private static TimeSpan GetNewTimeSpan(int newTimerValue, TimerUnits newTimerUnits)
        {
            return newTimerUnits switch
            {
                TimerUnits.m
                    or TimerUnits.min
                    or TimerUnits.mins
                    or TimerUnits.minute
                    or TimerUnits.minutes
                        => TimeSpan.FromMinutes(newTimerValue),

                TimerUnits.none
                    or TimerUnits.h
                    or TimerUnits.hr
                    or TimerUnits.hrs
                    or TimerUnits.hour
                    or TimerUnits.hours
                        => TimeSpan.FromHours(newTimerValue),

                _ => throw new InvalidOperationException($"No existing unit for {newTimerUnits}"),
            };
        }
    }
}