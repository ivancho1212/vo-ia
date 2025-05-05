Voia API
Descripción
Voia API es un sistema de backend basado en una arquitectura RESTful que permite gestionar diversos aspectos de un sistema, como usuarios, roles, suscripciones, sesiones de entrenamiento, respuestas de soporte y más. La API está construida utilizando ASP.NET Core 6 y proporciona una interfaz para interactuar con la base de datos, realizar operaciones CRUD (Crear, Leer, Actualizar, Eliminar) y autenticar a los usuarios mediante roles y permisos.

Tecnologías Utilizadas
ASP.NET Core 6: Framework principal para construir la API.

Entity Framework Core: Para la gestión de la base de datos.

SQL Server: Base de datos utilizada para almacenar los datos de la aplicación.

Swagger: Para la documentación automática de la API y pruebas interactivas.

JWT (JSON Web Tokens): Para la autenticación y autorización.

AutoMapper: Para mapear entre entidades y DTOs.

LINQ: Para consultas complejas a la base de datos.

Middleware personalizado: Para la validación de permisos y roles.

Requisitos
Antes de comenzar, asegúrate de tener instalados los siguientes programas:

.NET 6 SDK (o superior) - Puedes descargarlo desde aquí.

SQL Server (o SQL Server Express) o cualquier base de datos compatible con Entity Framework Core.

Postman o cURL - Herramientas para realizar peticiones HTTP.

Instalación
Sigue estos pasos para instalar y ejecutar la API en tu máquina local:

Clona el repositorio:

bash
Copy
Edit
git clone https://github.com/ivancho1212/vo-ia
cd voia-api
Restaura las dependencias:

bash
Copy
Edit
dotnet restore
Configura la base de datos:

La API usa Entity Framework Core para interactuar con la base de datos. Debes configurar la conexión a la base de datos en el archivo appsettings.json:

json
Copy
Edit
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=VoiaDb;User Id=sa;Password=your_password;"
}
Ejecuta las migraciones para crear la base de datos:

bash
Copy
Edit
dotnet ef database update
Inicia la API:

bash
Copy
Edit
dotnet run
La API se iniciará en https://localhost:5006 (por defecto).

Uso de la API
Autenticación
La API utiliza JWT para la autenticación. Para obtener un token JWT, primero debes autenticarte mediante el endpoint de login:

Endpoint de Login

URL: POST /api/auth/login

Cuerpo:

json
Copy
Edit
{
  "username": "usuario",
  "password": "contraseña"
}
Respuesta:

json
Copy
Edit
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
Usa el token JWT obtenido para acceder a los endpoints de la API. Agrega el token en el encabezado de la siguiente manera:

http
Copy
Edit
Authorization: Bearer <tu_token_jwt>
Endpoints Principales
Usuarios

Obtener todos los usuarios: GET /api/users

Crear un nuevo usuario: POST /api/users

Actualizar usuario: PUT /api/users/{id}

Eliminar usuario: DELETE /api/users/{id}

Roles

Obtener todos los roles: GET /api/roles

Crear un nuevo rol: POST /api/roles

Actualizar rol: PUT /api/roles/{id}

Eliminar rol: DELETE /api/roles/{id}

Prompts

Obtener todos los prompts: GET /api/prompts

Crear un nuevo prompt: POST /api/prompts

Actualizar un prompt: PUT /api/prompts/{id}

Eliminar un prompt: DELETE /api/prompts/{id}

Suscripciones

Obtener todas las suscripciones: GET /api/subscriptions

Crear una nueva suscripción: POST /api/subscriptions

Actualizar suscripción: PUT /api/subscriptions/{id}

Eliminar suscripción: DELETE /api/subscriptions/{id}

Swagger
Una vez que la API esté en funcionamiento, puedes acceder a la documentación de Swagger para explorar los endpoints, ver ejemplos de respuestas y probar la API en tiempo real.

URL de Swagger: https://localhost:5006/swagger

Seguridad y Autorización
La API implementa control de acceso mediante roles y permisos. Los usuarios deben estar autenticados y tener los permisos necesarios para realizar ciertas operaciones. Puedes asignar roles y permisos a los usuarios desde la base de datos.

Los roles y permisos se definen y gestionan en la base de datos y se utilizan en el código mediante el middleware y los atributos de autorización.

Contribución
Si deseas contribuir a este proyecto, por favor sigue estos pasos:

Haz un fork del repositorio.

Crea una rama para tus cambios (git checkout -b feature/nueva-caracteristica).

Realiza tus cambios y haz un commit (git commit -am 'Agregué nueva característica').

Haz push a tu rama (git push origin feature/nueva-caracteristica).

Crea un pull request describiendo los cambios realizados.

Licencia
Este proyecto está licenciado bajo la Licencia MIT - mira el archivo LICENSE para más detalles.

