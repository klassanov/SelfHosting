using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SelfHosting.Tests
{
    public class WeatherForecastEndpointTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory = new();
        private HttpClient _client = new();

        public async ValueTask InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task GetWeatherForecast_ReturnsOkStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetWeatherForecast_ReturnsJsonContent()
        {
            // Act
            var response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetWeatherForecast_ReturnsFiveDayForecast()
        {
            // Act
            var response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // Assert
            Assert.NotNull(forecasts);
            Assert.Equal(5, forecasts.Length);
        }

        [Fact]
        public async Task GetWeatherForecast_EachForecastHasValidProperties()
        {
            // Act
            var response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // Assert
            Assert.NotNull(forecasts);
            foreach (var forecast in forecasts)
            {
                Assert.NotNull(forecast.Summary);
                Assert.NotEmpty(forecast.Summary);
                Assert.InRange(forecast.TemperatureC, -20, 55);
                Assert.True(forecast.Date >= DateOnly.FromDateTime(DateTime.Now.AddDays(1)));
            }
        }

        [Fact]
        public async Task GetWeatherForecast_TemperatureFahrenheitCalculationIsCorrect()
        {
            // Act
            var response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // Assert
            Assert.NotNull(forecasts);
            foreach (var forecast in forecasts)
            {
                // TemperatureF = 32 + (TemperatureC / 0.5556)
                var expectedF = 32 + (int)(forecast.TemperatureC / 0.5556);
                Assert.Equal(expectedF, forecast.TemperatureF);
            }
        }

        [Fact]
        public async Task GetWeatherForecast_AllDatesAreConsecutiveDays()
        {
            // Act
            var response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // Assert
            Assert.NotNull(forecasts);
            var today = DateOnly.FromDateTime(DateTime.Now);
            for (int i = 0; i < forecasts.Length; i++)
            {
                var expectedDate = today.AddDays(i + 1);
                Assert.Equal(expectedDate, forecasts[i].Date);
            }
        }
    }

    // Mirror of the WeatherForecast record from Program.cs for deserialization
    public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
