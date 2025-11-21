namespace Voia.Api.Services.Caching
{
    /// <summary>
    /// Constantes para Cache Keys y TTLs
    /// Centraliza las claves y tiempos de expiración
    /// </summary>
    public static class CacheConstants
    {
        // ✅ BOT CACHE - 1 hora TTL
        public const string BOT_KEY_PREFIX = "bot";
        public static readonly TimeSpan BOT_TTL = TimeSpan.FromHours(1);
        public static string GetBotKey(int botId) => $"{BOT_KEY_PREFIX}:{botId}";
        public static string GetBotsByUserKey(int userId) => $"bots:user:{userId}";
        public static string GetBotsKey() => "bots:all"; // Cache global sin filtros

        // ✅ TEMPLATE CACHE - 24 horas TTL
        public const string TEMPLATE_KEY_PREFIX = "template";
        public static readonly TimeSpan TEMPLATE_TTL = TimeSpan.FromHours(24);
        public static string GetTemplateKey(int templateId) => $"{TEMPLATE_KEY_PREFIX}:{templateId}";
        public static string GetTemplatesByBotKey(int botId) => $"templates:bot:{botId}";
        public static string GetAllTemplatesKey() => "templates:all"; // Cache para GetAll()

        // ✅ USER CACHE - 1 hora TTL
        public const string USER_KEY_PREFIX = "user";
        public static readonly TimeSpan USER_TTL = TimeSpan.FromHours(1);
        public static string GetUserKey(int userId) => $"{USER_KEY_PREFIX}:{userId}";
        public static string GetAllUsersKey(int page, int pageSize) => $"users:all:{page}:{pageSize}"; // Cache paginado

        // ✅ STYLE CACHE - 24 horas TTL (cambia raramente)
        public const string STYLE_KEY_PREFIX = "style";
        public static readonly TimeSpan STYLE_TTL = TimeSpan.FromHours(24);
        public static string GetStyleKey(int botId) => $"{STYLE_KEY_PREFIX}:bot:{botId}";
        public static string GetAllStylesKey() => "styles:all"; // Cache para GetAllStyles()

        // ✅ WELCOME MESSAGE CACHE - 24 horas TTL
        public const string WELCOME_KEY_PREFIX = "welcome";
        public static readonly TimeSpan WELCOME_TTL = TimeSpan.FromHours(24);
        public static string GetWelcomeKey(int botId) => $"{WELCOME_KEY_PREFIX}:bot:{botId}";
    }
}
