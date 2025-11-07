using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using System.Net;

namespace Voia.Api.Services
{
    /// <summary>
    /// Servicio de geolocalización usando MaxMind GeoLite2
    /// </summary>
    public interface IGeoLocationService
    {
        /// <summary>
        /// Obtiene información de país y ciudad a partir de una dirección IP
        /// </summary>
        /// <param name="ipAddress">Dirección IP a geolocalizar</param>
        /// <returns>Tupla (país, ciudad) o ("Unknown", "Unknown") si no se puede determinar</returns>
        Task<(string Country, string City)> GetLocationAsync(string ipAddress);
    }

    public class GeoLocationService : IGeoLocationService, IDisposable
    {
        private readonly ILogger<GeoLocationService> _logger;
        private readonly string _geoLite2Path;
        private DatabaseReader _reader;

        public GeoLocationService(ILogger<GeoLocationService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            // Ruta a la base de datos GeoLite2 City (deberás descargarla)
            _geoLite2Path = Path.Combine(env.ContentRootPath, "wwwroot", "GeoLite2-City.mmdb");

            try
            {
                if (File.Exists(_geoLite2Path))
                {
                    _reader = new DatabaseReader(_geoLite2Path);
                    _logger.LogInformation("GeoLocation Service initialized with GeoLite2 database");
                }
                else
                {
                    _logger.LogWarning($"GeoLite2 database not found at {_geoLite2Path}. Geolocation will return 'Unknown'");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing GeoLocation Service");
            }
        }

        public async Task<(string Country, string City)> GetLocationAsync(string ipAddress)
        {
            return await Task.Run(() => GetLocation(ipAddress));
        }

        private (string Country, string City) GetLocation(string ipAddress)
        {
            try
            {
                // Validar IP
                if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out var ip))
                {
                    _logger.LogWarning($"Invalid IP address: {ipAddress}");
                    return ("Unknown", "Unknown");
                }

                // Si no hay lector, retornar Unknown
                if (_reader == null)
                {
                    return ("Unknown", "Unknown");
                }

                // Saltar IPs locales
                if (IPAddress.IsLoopback(ip) || ip.ToString() == "127.0.0.1" || ip.ToString() == "::1")
                {
                    return ("Local", "Local");
                }

                // Obtener respuesta de la base de datos
                var response = _reader.City(ip);

                var countryCode = response.Country?.IsoCode ?? "Unknown";
                var countryName = GetCountryNameFromIsoCode(countryCode);
                var city = response.City?.Name ?? "Unknown";

                _logger.LogInformation($"Geolocation for IP {ipAddress}: Country={countryName} ({countryCode}), City={city}");

                return (countryName, city);
            }
            catch (GeoIP2Exception ex)
            {
                _logger.LogWarning($"GeoIP2 Exception for IP {ipAddress}: {ex.Message}");
                return ("Unknown", "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting geolocation for IP {ipAddress}");
                return ("Unknown", "Unknown");
            }
        }

        /// <summary>
        /// Convierte códigos ISO de país a nombres legibles
        /// </summary>
        private string GetCountryNameFromIsoCode(string isoCode)
        {
            return isoCode?.ToUpper() switch
            {
                "ES" => "Spain",
                "CO" => "Colombia",
                "US" => "United States",
                "GB" => "United Kingdom",
                "FR" => "France",
                "DE" => "Germany",
                "IT" => "Italy",
                "JP" => "Japan",
                "CN" => "China",
                "IN" => "India",
                "BR" => "Brazil",
                "MX" => "Mexico",
                "AR" => "Argentina",
                "AU" => "Australia",
                "CA" => "Canada",
                "IL" => "Israel",
                "RU" => "Russia",
                "KR" => "South Korea",
                _ => isoCode ?? "Unknown"
            };
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
