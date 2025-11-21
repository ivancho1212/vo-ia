using System;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Voia.Api.Services.Security
{
    /// <summary>
    /// Servicio para proteger contra XXE (XML External Entity) attacks.
    /// 
    /// XXE Attack Example:
    /// <?xml version="1.0"?>
    /// <!DOCTYPE foo [
    ///   <!ENTITY xxe SYSTEM "file:///etc/passwd">
    /// ]>
    /// <foo>&xxe;</foo>
    /// 
    /// Solución: Deshabilitar entity expansion y external DTDs.
    /// </summary>
    public interface IXxeProtectionService
    {
        /// <summary>
        /// Cargar XML de forma segura contra XXE attacks.
        /// </summary>
        XDocument LoadXmlSafe(string xmlContent);

        /// <summary>
        /// Cargar XML desde archivo de forma segura.
        /// </summary>
        XDocument LoadXmlFileSafe(string filePath);

        /// <summary>
        /// Obtener XmlReader seguro (para StreamReader, etc).
        /// </summary>
        XmlReader CreateSafeXmlReader(System.IO.Stream stream);
    }

    public class XxeProtectionService : IXxeProtectionService
    {
        private readonly ILogger<XxeProtectionService> _logger;

        public XxeProtectionService(ILogger<XxeProtectionService> logger)
        {
            _logger = logger;
        }

        public XDocument LoadXmlSafe(string xmlContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    throw new ArgumentException("XML content cannot be null or empty", nameof(xmlContent));
                }

                // Crear XmlReaderSettings seguro
                var settings = new XmlReaderSettings
                {
                    // ✅ CRÍTICO: Deshabilitar entity expansion
                    DtdProcessing = DtdProcessing.Prohibit,

                    // ✅ CRÍTICO: No permitir external DTDs
                    XmlResolver = null,

                    // ✅ Deshabilitar features peligrosas
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    IgnoreWhitespace = true,

                    // ✅ Limitar tamaño de entrada
                    MaxCharactersInDocument = 1_000_000, // 1 MB máximo
                    MaxCharactersFromEntities = 100_000,

                    // ✅ Validación
                    ConformanceLevel = ConformanceLevel.Document,
                };

                using (var reader = XmlReader.Create(
                    new System.IO.StringReader(xmlContent),
                    settings))
                {
                    var doc = XDocument.Load(reader, LoadOptions.None);
                    _logger.LogInformation("✅ [XXE] XML loaded safely from content");
                    return doc;
                }
            }
            catch (XmlException ex)
            {
                _logger.LogWarning($"⚠️ [XXE] XML parsing failed (possible XXE attempt?): {ex.Message}");
                throw new InvalidOperationException("Invalid XML format. Check for XXE attacks.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [XXE] Unexpected error loading XML: {ex.Message}");
                throw;
            }
        }

        public XDocument LoadXmlFileSafe(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!System.IO.File.Exists(filePath))
                {
                    throw new System.IO.FileNotFoundException($"File not found: {filePath}");
                }

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    IgnoreWhitespace = true,
                    MaxCharactersInDocument = 1_000_000,
                    MaxCharactersFromEntities = 100_000,
                    ConformanceLevel = ConformanceLevel.Document,
                };

                using (var reader = XmlReader.Create(filePath, settings))
                {
                    var doc = XDocument.Load(reader, LoadOptions.None);
                    _logger.LogInformation($"✅ [XXE] XML loaded safely from file: {filePath}");
                    return doc;
                }
            }
            catch (XmlException ex)
            {
                _logger.LogWarning($"⚠️ [XXE] XML file parsing failed: {ex.Message}");
                throw new InvalidOperationException("Invalid XML file format. Check for XXE attacks.", ex);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                _logger.LogError($"❌ [XXE] File not found: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [XXE] Error loading XML file: {ex.Message}");
                throw;
            }
        }

        public XmlReader CreateSafeXmlReader(System.IO.Stream stream)
        {
            try
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    IgnoreWhitespace = true,
                    MaxCharactersInDocument = 1_000_000,
                    MaxCharactersFromEntities = 100_000,
                    ConformanceLevel = ConformanceLevel.Document,
                };

                var reader = XmlReader.Create(stream, settings);
                _logger.LogInformation("✅ [XXE] Safe XmlReader created for stream");
                return reader;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [XXE] Error creating safe XML reader: {ex.Message}");
                throw;
            }
        }
    }
}
