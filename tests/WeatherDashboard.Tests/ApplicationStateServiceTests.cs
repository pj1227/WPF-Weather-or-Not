using System.Collections.Generic;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services;
using Xunit;

namespace WeatherDashboard.Tests
{
    public class ApplicationStateServiceTests
    {
        // ── Defaults ─────────────────────────────────────────────────────────

        [Fact]
        public void UseCelsius_DefaultsToTrue()
        {
            var svc = new ApplicationStateService();
            Assert.True(svc.UseCelsius);
        }

        [Fact]
        public void SelectedLocation_DefaultsToNull()
        {
            var svc = new ApplicationStateService();
            Assert.Null(svc.SelectedLocation);
        }

        // ── UseCelsius PropertyChanged ────────────────────────────────────────

        [Fact]
        public void SetUseCelsius_DifferentValue_FiresPropertyChanged()
        {
            var svc     = new ApplicationStateService();
            var changed = new List<string?>();
            svc.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            svc.UseCelsius = false;

            Assert.Contains(nameof(svc.UseCelsius), changed);
        }

        [Fact]
        public void SetUseCelsius_SameValue_DoesNotFirePropertyChanged()
        {
            var svc   = new ApplicationStateService();  // defaults to true
            int count = 0;
            svc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(svc.UseCelsius)) count++;
            };

            svc.UseCelsius = true;  // same value — SetProperty should not fire

            Assert.Equal(0, count);
        }

        [Fact]
        public void SetUseCelsius_ToFalse_ThenTrue_FiresTwice()
        {
            var svc   = new ApplicationStateService();
            int count = 0;
            svc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(svc.UseCelsius)) count++;
            };

            svc.UseCelsius = false;
            svc.UseCelsius = true;

            Assert.Equal(2, count);
        }

        // ── SelectedLocation PropertyChanged ──────────────────────────────────

        [Fact]
        public void SetSelectedLocation_DifferentValue_FiresPropertyChanged()
        {
            var svc     = new ApplicationStateService();
            var changed = new List<string?>();
            svc.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            svc.SelectedLocation = new SavedLocation { Id = 1, Name = "London" };

            Assert.Contains(nameof(svc.SelectedLocation), changed);
        }

        [Fact]
        public void SetSelectedLocation_SameReference_DoesNotFirePropertyChanged()
        {
            var svc      = new ApplicationStateService();
            var location = new SavedLocation { Id = 1, Name = "London" };
            svc.SelectedLocation = location;

            int count = 0;
            svc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(svc.SelectedLocation)) count++;
            };

            svc.SelectedLocation = location;  // same reference

            Assert.Equal(0, count);
        }
    }
}
